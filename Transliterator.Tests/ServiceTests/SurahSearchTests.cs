using Transliterator.Core.Services;
using Transliterator.Domain.Entities;
using Transliterator.Domain.Localization;
using Xunit;

namespace Transliterator.Tests.ServiceTests
{
    /// <summary>
    /// Поиск в списке сур (D15): номер, имя на любом языке страницы и арабское имя,
    /// без учёта регистра, дефисов и диакритики.
    /// </summary>
    public class SurahSearchTests
    {
        private static CorpusSurah Surah(int number, string ru, string en, string arabic) => new()
        {
            Number = number,
            Name = new LocalizedText(new Dictionary<string, string> { ["ru"] = ru, ["en"] = en }),
            ArabicName = arabic,
        };

        private static readonly CorpusSurah[] Surahs =
        {
            Surah(1, "Аль-Фатиха", "Al-Fatiha", "سُورَةُ ٱلْفَاتِحَةِ"),
            Surah(11, "Худ", "Hud", "سُورَةُ هُودٍ"),
            Surah(110, "Ан-Наср", "An-Nasr", "سُورَةُ ٱلنَّصْرِ"),
            Surah(112, "Аль-Ихляс", "Al-Ikhlas", "سُورَةُ ٱلْإِخْلَاصِ"),
        };

        private static int[] Found(string query) => SurahSearch.Filter(Surahs, query).Select(s => s.Number).ToArray();

        [Theory]
        [InlineData("фат")]
        [InlineData("ФАТ")]
        [InlineData("альфатиха")]
        [InlineData("Al Fatiha")]
        [InlineData("al-fātiḥa")]
        [InlineData("الفاتحة")]
        [InlineData("ٱلْفَاتِحَةِ")]
        public void FindsAlFatiha_ByItsNameInAnyLanguage_AndInArabic(string query) =>
            Assert.Equal(new[] { 1 }, Found(query));

        [Fact]
        public void FindsAName_InTheOtherLanguageOfThePage() =>
            Assert.Equal(new[] { 112 }, Found("ikhlas"));

        [Fact]
        public void FindsTheArabicName_WithHamzaAndWithout() =>
            Assert.Equal(new[] { 112 }, Found("الاخلاص"));

        [Fact]
        public void ANumber_FindsByItsStart_TheExactNumberFirst()
        {
            Assert.Equal(1, Found("1")[0]);
            Assert.Equal(new[] { 11, 110, 112 }, Found("11"));
            Assert.Equal(new[] { 112 }, Found("112"));
        }

        [Theory]
        [InlineData("")]
        [InlineData("  ")]
        [InlineData(null)]
        public void AnEmptyQuery_KeepsTheWholeList(string? query) =>
            Assert.Equal(new[] { 1, 11, 110, 112 }, Found(query!));

        [Fact]
        public void NothingMatches_NothingIsFound() =>
            Assert.Empty(Found("бакара"));
    }
}
