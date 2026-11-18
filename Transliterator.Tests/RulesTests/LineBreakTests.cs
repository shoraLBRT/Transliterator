using Transliterator.Core.Services.Phonology;
using Xunit;

namespace Transliterator.Tests.RulesTests
{
    /// <summary>
    /// Перевод строки — граница слов для правил и перевод строки для письма (D3),
    /// и вдобавок пауза (B15): строку чтец кончает так же, как аят, — последнее
    /// слово в форме паузы, следующая строка начинается новым высказыванием.
    /// </summary>
    public class LineBreakTests
    {
        [Theory]
        [InlineData("قُلْ\nهُوَ", "قُلْ\nهُوَ")]
        [InlineData("قُلْ \r\n  هُوَ", "قُلْ\nهُوَ")]
        [InlineData("قُلْ\u2028هُوَ", "قُلْ\nهُوَ")]
        // Пустые строки не сохраняются: промежуток остаётся одной границей.
        [InlineData("قُلْ\n\n\nهُوَ", "قُلْ\nهُوَ")]
        [InlineData("قُلْ   هُوَ", "قُلْ هُوَ")]
        [InlineData("\n قُلْ هُوَ \n", "قُلْ هُوَ")]
        public void Normalizer_KeepsOneLineBreak_AndCollapsesTheRest(string input, string expected)
        {
            Assert.Equal(expected, new ArabicNormalizer().Normalize(input));
        }

        [Fact]
        public void LineBreak_IsAPause_LikeAVerseNumber()
        {
            // Без переноса — васля и слитная дамма: «hууа-ллааhу».
            // С переносом هُوَ стоит в форме паузы, а ٱللَّهُ начинает заново.
            var lines = TransliterationPipeline.Transliterate("قُلْ هُوَ\nٱللَّهُ أَحَدٌ");

            Assert.Equal("qуль hуу\nал-лааhу ахIад", lines);
        }

        [Fact]
        public void LineBreak_TakesTheTanwinToItsPausalForm()
        {
            // Танвин на конце строки снимается так же, как перед номером аята.
            var lines = TransliterationPipeline.Transliterate("أَحَدٌ\nلَمْ يَلِدْ");

            Assert.Equal(TransliterationPipeline.Transliterate("أَحَدٌ ١ لَمْ يَلِدْ").Replace(" 1 ", "\n"), lines);
        }

        [Fact]
        public void Surah110_LastWordOfALine_IsInPausalForm()
        {
            // Пример из отзыва (B15): «уастагъфирhу» в конце третьей строки читалось
            // слитно, хотя строка кончается. Перед переносом ещё и ۚ — пауза и сама.
            var text = TransliterationPipeline.Transliterate(
                "إِذَا جَآءَ نَصْرُ ٱللَّهِ وَٱلْفَتْحُ ١\n"
                + "وَرَأَيْتَ ٱلنَّاسَ يَدْخُلُونَ فِى دِينِ ٱللَّهِ أَفْوَاجًۭا ٢\n"
                + "فَسَبِّحْ بِحَمْدِ رَبِّكَ وَٱسْتَغْفِرْهُ ۚ\n"
                + " إِنَّهُۥ كَانَ تَوَّابًۢا ٣");

            var lines = text.Split('\n');
            Assert.Equal(4, lines.Length);
            Assert.EndsWith("уастагъфирh", lines[2]);
            Assert.StartsWith("иннаhуу ", lines[3]);
        }

        [Fact]
        public void Permissible_Mark_IsAPause_WithoutALineBreak()
        {
            // ۚ — пауза сама по себе (B15): та же строка без переноса даёт ту же форму.
            var text = TransliterationPipeline.Transliterate(
                "فَسَبِّحْ بِحَمْدِ رَبِّكَ وَٱسْتَغْفِرْهُ ۚ إِنَّهُۥ كَانَ تَوَّابًۢا");

            Assert.Contains("уастагъфирh иннаhуу", text);
        }

        [Fact]
        public void Lines_NeitherEndNorStartWithASpace()
        {
            var text = TransliterationPipeline.Transliterate("قُلْ هُوَ ٱللَّهُ أَحَدٌ ١\nٱللَّهُ ٱلصَّمَدُ ٢");

            Assert.Equal(2, text.Split('\n').Length);
            Assert.DoesNotContain(" \n", text);
            Assert.DoesNotContain("\n ", text);
        }
    }
}
