using Xunit;

namespace Transliterator.Tests.RulesTests
{
    /// <summary>
    /// Мягкость ляма на письме (B12). Стадия 8 помечает гласную мягкого ляма
    /// смягчённой перед любой гласной, а записывает её профиль. В Standard
    /// мягкость видна везде, где кириллица её различает: «ль», «ля», «лю».
    /// «ли» мягкое и без варианта.
    /// </summary>
    public class SoftLamTests
    {
        [Theory]
        [InlineData("حَبْلٌۭ مِّن مَّسَدٍۢ", "хIаблюм-мим-масад")] // дамма — «лю» (111:5)
        [InlineData("مَالُهُۥ وَمَا", "маалюhуу уамаа")]        // дамма — «лю» (111:2)
        [InlineData("ٱدْخُلُوا", "удхъулюю")]                   // долгая «у» — «люю», как «ляя»
        [InlineData("لِلَّهِ رَبِّ", "лилляяhи робб")]           // фатха — «ля», касра — «ли»
        [InlineData("قُلْ هُوَ ٱللَّهُ أَحَدٌ", "qуль hууа-ллааhу ахIад")] // безгласный — «ль»
        public void SoftLam_IsWrittenSoftBeforeEveryVowel(string arabic, string expected) =>
            Assert.Equal(expected, TransliterationPipeline.Transliterate(arabic));

        [Theory]
        [InlineData("هُوَ ٱللَّهُ أَحَدٌ", "hууа-ллааhу ахIад")] // лям имени Аллаха после фатхи — твёрдый
        [InlineData("رَسُولُ ٱللَّهِ", "росуулю-ллаааh")]        // после даммы — тоже
        public void LamOfAllah_AfterFathaOrDamma_StaysHard(string arabic, string expected) =>
            // Его гласная не смягчается ни в «я», ни в «ю»: B12 касается только
            // мягкого ляма. Во втором случае лямов два: у самого رَسُولُ лям
            // обычный и потому мягкий — «лю», а следом твёрдое «ллааа» имени
            // Аллаха. Долгота и «h» там — от паузы в конце.
            Assert.Equal(expected, TransliterationPipeline.Transliterate(arabic));
    }
}
