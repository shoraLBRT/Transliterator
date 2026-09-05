using System.Globalization;
using Xunit;

namespace Transliterator.Tests.ProfileTests
{
    /// <summary>
    /// Второй профиль в ресурсах. Его задача — не «ещё одна раскладка», а проверка
    /// главного инварианта проекта: правила решают, что за звук, профиль решает,
    /// как его записать. Ни одно правило про латиницу не знает, и весь конвейер
    /// отрабатывает здесь ровно тот же, что для Standard.
    /// </summary>
    public class LatinProfileTests
    {
        [Theory]
        [InlineData("بِسْمِ ٱللَّهِ ٱلرَّحْمَـٰنِ ٱلرَّحِيمِ", "bismi-llaahi-rroḥmaani-rroḥiiim")]
        [InlineData("ٱلْحَمْدُ لِلَّهِ رَبِّ ٱلْعَـٰلَمِينَ", "al-ḥamdu lillaahi robbi-l-ʿaalamiiin")]
        [InlineData("مَـٰلِكِ يَوْمِ ٱلدِّينِ", "maaliki yawmi-ddiiin")]
        [InlineData("قُلْ هُوَ ٱللَّهُ أَحَدٌ", "qul huwa-llaahu aḥad")]
        public void Pipeline_WritesTheSameReadingInLatin(string arabic, string expected) =>
            Assert.Equal(expected, TransliterationPipeline.Transliterate(arabic, TestProfiles.Latin));

        [Theory]
        [InlineData("أُنزِلَ", "унзиль", "unzil")]        // Standard: "ل|sukun" = "ль"
        [InlineData("ٱلَّذِينَ", "аллязъииин", "allaḏiiin")] // Standard: "َ|soft" = "я"
        public void OmittedVariant_FallsBackToTheBaseKey(string arabic, string cyrillic, string latin)
        {
            // Мягкость ляма кириллице приходится дописывать, латинице — нет.
            // Профиль вправе не задавать вариант вовсе: рендерер возьмёт базовый ключ.
            Assert.Equal(cyrillic, TransliterationPipeline.Transliterate(arabic));
            Assert.Equal(latin, TransliterationPipeline.Transliterate(arabic, TestProfiles.Latin));
        }

        [Fact]
        public void Hamza_IsNotConfusableWithAnyOtherLetter()
        {
            // В Standard хамза — "ъ", и "з + хамза" неотличимо от диграфа "зъ" (ذ):
            // это открытое решение проекта. Латиница ту же задачу решает диакритикой
            // вместо диграфов, поэтому ʾ рядом с любой буквой читается однозначно.
            Assert.Equal("qurʾaaan", TransliterationPipeline.Transliterate("قُرْءَانِ", TestProfiles.Latin));

            Assert.All(TestProfiles.Latin.Rules.Values.Where(v => v.Length > 0),
                grapheme => Assert.Equal(1, new StringInfo(grapheme).LengthInTextElements));
        }

        [Theory]
        [InlineData("رَبِّ", "robb")]  // фатха при твёрдой ر
        [InlineData("رِزْقِ", "rizq")] // касра держит ر мягкой
        public void Emphasis_ColorsTheVowelHere_Too(string arabic, string expected) =>
            // Решение об эмфазе принимает стадия 7, а не профиль: латиница просто
            // получает уже окрашенную гласную и пишет её своей графемой.
            Assert.Equal(expected, TransliterationPipeline.Transliterate(arabic, TestProfiles.Latin));

        [Theory]
        [InlineData("رَحْمَةٌ", "roḥmah")]   // та-марбута на паузе — вариант "ة|waqf"
        [InlineData("لَهُ مَا", "lahuu maa")] // мадд силя сугра — два хараката
        [InlineData("خَوْفٌ", "ḫowwwf")]     // мадд лин на паузе — четыре, и лежат на глайде
        public void MaddAndWaqf_ReachLatinUnchanged(string arabic, string expected) =>
            Assert.Equal(expected, TransliterationPipeline.Transliterate(arabic, TestProfiles.Latin));
    }
}
