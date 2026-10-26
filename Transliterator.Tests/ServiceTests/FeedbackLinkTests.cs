using System.Web;
using Transliterator.Core.Services;
using Xunit;

namespace Transliterator.Tests.ServiceTests
{
    /// <summary>
    /// Ссылка кнопки обратной связи (H1): форма нового issue на GitHub, заполненная
    /// тем, что написал человек, и — с его согласия — парой «вход и вывод».
    /// </summary>
    public class FeedbackLinkTests
    {
        private const string Arabic = "يَسْـَٔلُونَ";
        private const string Output = "йасалююн";

        [Fact]
        public void Link_OpensTheNewIssueFormOfTheRepository()
        {
            var draft = FeedbackLink.Build("Пропала хамза");

            Assert.StartsWith("https://github.com/shoraLBRT/Transliterator/issues/new?", draft.Url);
            Assert.False(draft.Truncated);
        }

        [Fact]
        public void Message_GoesToTheBody_AndItsFirstLineToTheTitle()
        {
            var (title, body) = Fields(FeedbackLink.Build("Пропала хамза\nв слове «спрашивают»"));

            Assert.Equal("Отзыв со страницы: Пропала хамза", title);
            Assert.StartsWith("Пропала хамза\nв слове «спрашивают»", body);
        }

        [Fact]
        public void LongFirstLine_IsClippedInTheTitle_ButKeptInTheBody()
        {
            var line = new string('а', 100);
            var (title, body) = Fields(FeedbackLink.Build(line));

            Assert.Equal($"Отзыв со страницы: {new string('а', FeedbackMessage.TitleLength)}…", title);
            Assert.StartsWith(line, body);
        }

        [Fact]
        public void WithoutConsent_NothingButTheMessageIsSent()
        {
            var (_, body) = Fields(FeedbackLink.Build("Пропала хамза"));

            Assert.DoesNotContain("```", body);
        }

        [Fact]
        public void AttachedPair_ReachesTheBodyIntact()
        {
            var (_, body) = Fields(FeedbackLink.Build("Пропала хамза", Arabic, Output));

            Assert.Contains($"```text\n{Arabic}\n```", body);
            Assert.Contains($"```text\n{Output}\n```", body);
        }

        [Fact]
        public void PairWithoutAMessage_IsEnoughToReport()
        {
            var (title, body) = Fields(FeedbackLink.Build("", Arabic, Output));

            Assert.Equal(FeedbackMessage.TitlePrefix, title);
            Assert.Contains(Arabic, body);
        }

        [Theory]
        [InlineData("", Output)]
        [InlineData(Arabic, "")]
        public void HalfAPair_IsNotAttached(string arabic, string output)
        {
            var (_, body) = Fields(FeedbackLink.Build("Пропала хамза", arabic, output));

            Assert.DoesNotContain("```", body);
        }

        /// <summary>
        /// Целая сура в поле — тысячи букв, а GitHub не откроет ссылку длиннее
        /// нескольких килобайт. Режется приложение, сообщение остаётся целым.
        /// </summary>
        [Fact]
        public void LongPair_IsClipped_AndTheMessageKept()
        {
            var arabic = string.Join('\n', Enumerable.Repeat("بِسْمِ ٱللَّهِ ٱلرَّحْمَٰنِ ٱلرَّحِيمِ", 200));
            var output = string.Join('\n', Enumerable.Repeat("бисми-лляяhи-ррохIмаани-ррохIиим", 200));

            var draft = FeedbackLink.Build("Пропала хамза", arabic, output);
            var (_, body) = Fields(draft);

            Assert.True(draft.Truncated);
            Assert.InRange(draft.Url.Length, 1, FeedbackLink.MaxUrlLength);
            Assert.StartsWith("Пропала хамза", body);
            Assert.Contains("بِسْمِ", body);
            Assert.Contains("бисми", body);
            Assert.Contains("…\n```", body);
        }

        [Fact]
        public void LongMessage_IsClippedToFit()
        {
            var draft = FeedbackLink.Build(new string('ж', 3000));

            Assert.True(draft.Truncated);
            Assert.InRange(draft.Url.Length, 1, FeedbackLink.MaxUrlLength);
        }

        /// <summary>Половинка суррогатной пары — неверный UTF-8, и кодирование ссылки падает.</summary>
        [Fact]
        public void Clipping_DoesNotSplitASurrogatePair()
        {
            var draft = FeedbackLink.Build(string.Concat(Enumerable.Repeat("👍", 1000)));
            var (_, body) = Fields(draft);

            Assert.True(draft.Truncated);
            Assert.DoesNotContain('�', body);
        }

        private static (string Title, string Body) Fields(FeedbackDraft draft)
        {
            var query = HttpUtility.ParseQueryString(new Uri(draft.Url).Query);
            return (query["title"]!, query["body"]!);
        }
    }
}
