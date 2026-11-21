using Transliterator.Core.Services;
using Transliterator.Core.Services.Phonology;
using Transliterator.Core.Services.Rules;
using Transliterator.Domain.Entities;
using Xunit;

namespace Transliterator.Tests.ServiceTests
{
    /// <summary>
    /// Подсказки букв (D41): отрисовщик отмечает, из какой арабской буквы получился
    /// каждый кусок вывода, а страница по этим кускам подсвечивает буквы, звука
    /// которых нет в русском. Буква берётся из происхождения, не из поиска графемы.
    /// </summary>
    public class OutputLettersTests
    {
        private static (string Text, List<LetterSpan> Letters) Render(string arabic)
        {
            var letters = new List<LetterSpan>();
            var text = new CyrillicRenderer().Render(TransliterationPipeline.Parse(arabic), TestProfiles.Cyrillic, null, letters);
            return (text, letters);
        }

        private static string Of(string text, LetterSpan span) => text.Substring(span.Start, span.Length);

        [Fact]
        public void EachConsonant_IsMarkedWithItsLetter_WithoutTheVowel()
        {
            var (text, letters) = Render("الْحَمْدُ");

            var ha = Assert.Single(letters, s => s.Letter == "ح");
            Assert.Equal(TestProfiles.Cyrillic.Rules["ح"], Of(text, ha));
            Assert.Equal("хI", Of(text, ha));
        }

        [Fact]
        public void DoubledConsonant_IsOneSpanOverBothHalves()
        {
            var (text, letters) = Render("رَبِّ");

            var ba = Assert.Single(letters, s => s.Letter == "ب");
            Assert.Equal("бб", Of(text, ba));
        }

        [Fact]
        public void Ayn_IsMarkedEvenThoughItIsOnlyAnApostrophe()
        {
            var (text, letters) = Render("الْعَالَمِينَ");

            var ayn = Assert.Single(letters, s => s.Letter == "ع");
            Assert.Equal("'", Of(text, ayn));
        }

        [Fact]
        public void Spans_AreInOrder_DoNotOverlap_AndStayInsideTheText()
        {
            var (text, letters) = Render("بِسْمِ اللَّهِ الرَّحْمَٰنِ الرَّحِيمِ\nالْحَمْدُ لِلَّهِ رَبِّ الْعَالَمِينَ");

            Assert.NotEmpty(letters);
            var end = 0;
            foreach (var span in letters)
            {
                Assert.True(span.Start >= end, $"span {span} overlaps the previous one");
                Assert.True(span.Length > 0);
                Assert.True(span.Start + span.Length <= text.Length);
                Assert.DoesNotContain('\n', Of(text, span));
                end = span.Start + span.Length;
            }
        }

        [Fact]
        public void ServiceResult_CarriesTheSameSpansAsTheRenderer()
        {
            // Профиль передаётся целиком, хранилище сервису не нужно (C2).
            var service = new TransliterationService(
                null!, new ArabicNormalizer(), new ArabicParser(),
                new RulesService(new WaqfRule(), new WaslRule(), new ArticleRule(),
                                 new AssimilationRule(), new NasalRule(),
                                 new EmphasisRule(), new MaddRule(), new QalqalahRule()),
                new CyrillicRenderer());

            var result = service.Transliterate("الْحَمْدُ لِلَّهِ", TestProfiles.Cyrillic);
            var (text, letters) = Render("الْحَمْدُ لِلَّهِ");

            Assert.Equal(text, result.TransliteratedText);
            Assert.Equal(letters, result.Letters);
        }

        [Fact]
        public void Build_KeepsOnlyHintedLetters_InLinePositions()
        {
            const string text = "аб\nвгд";
            var letters = new[]
            {
                new LetterSpan(0, 1, "x"), new LetterSpan(1, 1, "y"),
                new LetterSpan(4, 2, "x"),
            };

            var hints = OutputLetters.Build(text, letters, letter => letter == "x");

            Assert.Equal(new[] { new LetterSpan(0, 1, "x") }, hints.Line(0));
            Assert.Equal(new[] { new LetterSpan(1, 2, "x") }, hints.Line(1));
            Assert.Empty(hints.Line(2));
        }

        [Fact]
        public void Split_CutsChangedPartsByLetters_AndGluesBackToTheSameText()
        {
            const string text = "ab cdef";
            var hints = OutputLetters.Build(text, new[] { new LetterSpan(1, 1, "x"), new LetterSpan(4, 2, "x") }, _ => true);

            // Кусок строки с позиции 1 — «b cdef», «cdef» изменилось (D19).
            var parts = new[] { new DiffSegment("b ", false), new DiffSegment("cdef", true) };
            var pieces = hints.Split(0, 1, parts);

            Assert.Equal("b cdef", string.Concat(pieces.Select(p => p.Text)));
            Assert.Equal(
                new[]
                {
                    new OutputPiece("b", false, "x"), new OutputPiece(" ", false, null),
                    new OutputPiece("c", true, null), new OutputPiece("de", true, "x"), new OutputPiece("f", true, null),
                },
                pieces);
        }

        [Fact]
        public void None_HasNoLetters()
        {
            var hints = OutputLetters.None("ab\ncd");

            Assert.Empty(hints.Line(0));
            Assert.Empty(hints.Line(1));
            Assert.Equal(new[] { new OutputPiece("ab", false, null) }, hints.Split(0, 0, new[] { new DiffSegment("ab", false) }));
        }
    }
}
