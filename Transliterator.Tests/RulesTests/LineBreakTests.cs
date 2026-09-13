using Transliterator.Core.Services.Phonology;
using Xunit;

namespace Transliterator.Tests.RulesTests
{
    /// <summary>
    /// Перевод строки — граница слов для правил и перевод строки для письма (D3).
    /// Правила его от пробела не отличают: перенос строки посреди фразы — вёрстка,
    /// а не остановка чтеца, и паузы он вводить не должен.
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

        [Theory]
        [InlineData("قُلْ هُوَ ٱللَّهُ أَحَدٌ")]
        [InlineData("لَمْ يَلِدْ وَلَمْ يُولَدْ")]
        [InlineData("إِذَا جَآءَ نَصْرُ ٱللَّهِ وَٱلْفَتْحُ")]
        public void LineBreak_ChangesTheLayout_NotTheReading(string ayah)
        {
            // Каждый пробел заменён переводом строки. Правила разницы не видят,
            // и вывод отличается только тем, чем разделены слова.
            var oneLine = TransliterationPipeline.Transliterate(ayah);
            var lines = TransliterationPipeline.Transliterate(ayah.Replace(' ', '\n'));

            Assert.Contains('\n', lines);
            Assert.Equal(oneLine, lines.Replace('\n', ' '));
        }

        [Fact]
        public void WordSeamAcrossALineBreak_IsStillAHyphen()
        {
            // Шов слова съедает границу, и перевод строки уходит вместе с пробелом:
            // слияние — звук, вёрстка его не разрывает.
            var lines = TransliterationPipeline.Transliterate("هُوَ\nٱللَّهُ");

            Assert.DoesNotContain('\n', lines);
            Assert.Equal(TransliterationPipeline.Transliterate("هُوَ ٱللَّهُ"), lines);
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
