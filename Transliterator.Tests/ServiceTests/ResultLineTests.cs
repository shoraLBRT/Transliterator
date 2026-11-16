using Microsoft.Extensions.Logging.Abstractions;
using Transliterator.Core.Repositories;
using Transliterator.Core.Services;
using Xunit;

namespace Transliterator.Tests.ServiceTests
{
    /// <summary>
    /// Строки вывода веб-версии (D17): номер аята отделяется знаком, а склеенные
    /// обратно строки — ровно прежний вывод, тот, что копирует кнопка.
    /// </summary>
    public class ResultLineTests
    {
        [Fact]
        public async Task AlFatiha_SevenLines_EachClosedByItsAyahMark_AndJoinsBackToTheSameText()
        {
            var corpus = new EmbeddedCorpusRepository(NullLogger<EmbeddedCorpusRepository>.Instance);
            var surah = (await corpus.GetSurahAsync(1))!;
            var result = TransliterationPipeline.Transliterate(CorpusText.Arabic(surah));

            var lines = ResultLine.Split(result);

            Assert.Equal(7, lines.Count);
            Assert.Equal(new[] { "1", "2", "3", "4", "5", "6", "7" }, lines.Select(l => l.Ayah));
            Assert.All(lines, line => Assert.EndsWith(" ", line.Text));
            Assert.Equal(result, ResultLine.Join(lines));
        }

        [Theory]
        [InlineData("бисмилляхи")]
        [InlineData("")]
        [InlineData("12")]
        [InlineData("сура 1234")]
        [InlineData("текст 12а")]
        public void TextWithoutATrailingNumber_IsOneLineWithoutAMark(string text)
        {
            var line = Assert.Single(ResultLine.Split(text));

            Assert.Null(line.Ayah);
            Assert.Equal(text, line.Text);
        }

        [Theory]
        [InlineData("а 1\n\nб 2")]
        [InlineData("первая строка\nвторая 3 ")]
        [InlineData("  ведущие пробелы 5")]
        public void AnyText_JoinsBackUnchanged(string text)
        {
            Assert.Equal(text, ResultLine.Join(ResultLine.Split(text)));
        }
    }
}
