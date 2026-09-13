using Microsoft.Extensions.Logging.Abstractions;
using Transliterator.Core.Repositories;
using Transliterator.Core.Services;
using Xunit;

namespace Transliterator.Tests.CorpusTests
{
    /// <summary>
    /// Текст суры, который подставляет панель готовых сур (D3). Прогон корпуса
    /// проверяет суру одной строкой; здесь проверяется, что панель подставляет
    /// ровно её же, только по аяту на строке, — и что вывод от этого меняется
    /// одной вёрсткой.
    /// </summary>
    public class CorpusTextTests
    {
        private static EmbeddedCorpusRepository Corpus() =>
            new(NullLogger<EmbeddedCorpusRepository>.Instance);

        public static TheoryData<int> SurahNumbers()
        {
            var data = new TheoryData<int>();
            foreach (var surah in Corpus().GetAllSurahsAsync().GetAwaiter().GetResult())
                data.Add(surah.Number);
            return data;
        }

        [Theory]
        [MemberData(nameof(SurahNumbers))]
        public async Task SurahFromThePanel_IsTheTestedSurah_OneAyahPerLine(int number)
        {
            var surah = (await Corpus().GetSurahAsync(number))!;
            var tested = CorpusCases.Surah($"сура {number}");

            var text = CorpusText.Arabic(surah);

            Assert.Equal(surah.Ayahs.Count, text.Split('\n').Length);
            Assert.Equal(tested.Arabic, text.Replace('\n', ' '));
        }

        [Theory]
        [MemberData(nameof(SurahNumbers))]
        public async Task SurahFromThePanel_ReadsAsTheSurahOnOneLine(int number)
        {
            var surah = (await Corpus().GetSurahAsync(number))!;
            var tested = CorpusCases.Surah($"сура {number}");

            var lines = TransliterationPipeline.Transliterate(CorpusText.Arabic(surah)).Split('\n');

            // Аят на строке и в выводе: номер аята закрывает свою строку.
            Assert.Equal(surah.Ayahs.Count, lines.Length);
            Assert.All(surah.Ayahs, ayah => Assert.EndsWith($" {ayah.Number}", lines[ayah.Number - 1]));
            Assert.Equal(tested.Expected, string.Join(' ', lines));
        }

        [Theory]
        [InlineData(1, "١")]
        [InlineData(7, "٧")]
        [InlineData(110, "١١٠")]
        public void ArabicDigits_WritesTheNumberInArabicIndicDigits(int number, string expected)
        {
            Assert.Equal(expected, CorpusText.ArabicDigits(number));
        }
    }
}
