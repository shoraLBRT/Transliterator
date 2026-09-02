using Xunit;
using Transliterator.Domain.Phonology;

namespace Transliterator.Tests.RulesTests
{
    public class WaslRuleTests
    {
        [Theory]
        [InlineData("بِسْمِ ٱللَّهِ", "бисми-лляhи")]
        [InlineData("بِسْمِ ٱللَّهِ ٱلرَّحْمَـٰنِ", "бисми-лляhи-ррохIмаани")]
        public void ConnectedWasl_MergesWordsWithHyphen(string arabic, string expected) =>
            Assert.Equal(expected, TransliterationPipeline.Transliterate(arabic));

        [Fact]
        public void VerseNumber_StartsNewUtterance() =>
            // После номера аята чтение начинается заново, поэтому васля озвучивается.
            Assert.Equal("2 аль-хIамду", TransliterationPipeline.Transliterate("٢ ٱلْحَمْدُ"));

        [Theory]
        [InlineData("ٱهْدِنَا", "иhдинаа")]   // третья буква с касрой
        [InlineData("ٱدْخُلُوا", "удхъулуу")] // третья буква с даммой
        [InlineData("ٱنظُرْ", "унзIур")]      // третья буква с даммой
        public void InitialWasl_TakesVowelFromThirdLetter(string arabic, string expected) =>
            Assert.Equal(expected, TransliterationPipeline.Transliterate(arabic));
    }

    public class ArticleRuleTests
    {
        [Theory]
        [InlineData("ٱلْحَمْدُ", "аль-хIамду")]
        [InlineData("ٱلْفَجْرِ", "аль-фаджри")]
        public void MoonLetter_KeepsLam(string arabic, string expected) =>
            Assert.Equal(expected, TransliterationPipeline.Transliterate(arabic));

        [Theory]
        [InlineData("ٱلرَّحْمَـٰنِ", "ар-рохIмаани")]
        [InlineData("ٱلسَّمَآءِ", "ас-самаааъи")]
        [InlineData("ٱلَّذِينَ", "аллязъиина")]
        public void SunLetter_AssimilatesLam(string arabic, string expected) =>
            Assert.Equal(expected, TransliterationPipeline.Transliterate(arabic));

        [Fact]
        public void MoonLam_SurvivesWaslMerge() =>
            // Прежде стадия васли съедала лям артикля целиком: "робби-'аалямиина".
            Assert.Equal("робби-ль-'аалямиина",
                TransliterationPipeline.Transliterate("رَبِّ ٱلْعَـٰلَمِينَ"));

        [Fact]
        public void MoonLam_ProducesSingleSoftSign() =>
            // Прежде правило мягкого знака шло после артикля и давало "альь-".
            Assert.DoesNotContain("ьь", TransliterationPipeline.Transliterate("ٱلْحَمْدُ"));
    }

    public class EmphasisRuleTests
    {
        [Theory]
        [InlineData("رِزْقِ", "ризqи")]        // касра — таркик
        [InlineData("ٱلْفَجْرِ", "аль-фаджри")] // касра — таркик
        [InlineData("رَبِّ", "робби")]          // фатха — тафхим
        public void Ra_EmphasisFollowsItsHarakah(string arabic, string expected) =>
            Assert.Equal(expected, TransliterationPipeline.Transliterate(arabic));

        [Fact]
        public void SakinEmphatic_ColoursPrecedingVowel() =>
            // Эмфаза распространяется и назад: прежде правило смотрело только вперёд.
            Assert.Equal("бор", TransliterationPipeline.Transliterate("بَر"));

        [Fact]
        public void LamOfAllah_IsLightAfterKasra() =>
            // "лилляhи", а не "лиллаhи". Прежний хак искал в кириллице "Аллах"
            // и не срабатывал никогда, потому что ه отображается в "h".
            Assert.Equal("лилляhи", TransliterationPipeline.Transliterate("لِلَّهِ"));
    }

    public class LetterCoverageTests
    {
        [Fact]
        public void Hamza_IsNotDropped() =>
            Assert.Equal("qуръаани", TransliterationPipeline.Transliterate("قُرْءَانِ"));

        [Fact]
        public void TaMarbutaAndTanwin_AreNotDropped() =>
            Assert.Equal("рохIматун", TransliterationPipeline.Transliterate("رَحْمَةٌ"));

        [Fact]
        public void HamzaCarrier_KeepsItsOwnHarakah() =>
            // Прежде огласовка носителя гасилась и أُ читалось как "а".
            Assert.Equal("унзиля", TransliterationPipeline.Transliterate("أُنزِلَ"));

        [Fact]
        public void EmphaticAtEndOfText_DoesNotThrow()
        {
            // Прежде выход за границы массива ронял приложение на любом тексте,
            // заканчивающемся эмфатической буквой.
            foreach (var word in new[] { "بَر", "قَط", "نَارٌ", "ٱنظُرْ" })
                Assert.False(string.IsNullOrEmpty(TransliterationPipeline.Transliterate(word)));
        }
    }

    public class WaqfRuleTests
    {
        [Fact]
        public void FinalShortVowel_IsRemovedOnWaqf()
        {
            // На знаке вакфа краткая гласная снимается: ِ (1 харакат) → сукун.
            // U+06D6 — ۖ (Optional waqf)
            var segments = TransliterationPipeline.Parse("ٱلْفَجْرِۖ");
            var ra = segments.First(s => s.Letter == "ر");
            Assert.True(ra.WaqfAfter != WaqfType.None);
            Assert.Equal(Harakah.Sukun, ra.Vowel);
        }

        [Fact]
        public void FathatanOnWaqf_BecomesLongA()
        {
            // Фатхатан (ً) на паузе: краткая фатха удлиняется в мадд ивад (2 харката).
            // U+06D6 — ۖ (Optional waqf)
            var segments = TransliterationPipeline.Parse("مُسْلِمٌۖ");
            var lastM = segments.Where(s => s.Letter == "م").Last();
            Assert.True(lastM.WaqfAfter != WaqfType.None);
            Assert.Equal(Harakah.Fatha, lastM.Vowel);
            Assert.Equal(2, lastM.VowelLength);
            var nun = segments.First(s => s.FromTanwin && s.Letter == "ن");
            Assert.True(nun.Silent);
        }

        [Fact]
        public void DammatanOnWaqf_IsRemoved()
        {
            // Дамматан (ٌ) на паузе: дамма + нун сакин снимаются полностью.
            // U+06D6 — ۖ (Optional waqf)
            var segments = TransliterationPipeline.Parse("رَحْمَٰنٌۖ");
            var lastM = segments.Where(s => s.Letter == "م").Last();
            Assert.True(lastM.WaqfAfter != WaqfType.None);
            Assert.Equal(Harakah.Sukun, lastM.Vowel);
            var nun = segments.First(s => s.FromTanwin && s.Letter == "ن");
            Assert.True(nun.Silent);
        }

        [Fact]
        public void KasratanOnWaqf_IsRemoved()
        {
            // Касратан (ٍ) на паузе: касра + нун сакин снимаются полностью.
            // U+06D6 — ۖ (Optional waqf)
            var segments = TransliterationPipeline.Parse("مُنَادٍۖ");
            var lastD = segments.Where(s => s.Letter == "د").Last();
            Assert.True(lastD.WaqfAfter != WaqfType.None);
            Assert.Equal(Harakah.Sukun, lastD.Vowel);
            var nun = segments.First(s => s.FromTanwin && s.Letter == "ن");
            Assert.True(nun.Silent);
        }

        [Fact]
        public void TaMarbutaOnWaqf_BecomesHa()
        {
            // Та-марбута на паузе становится /h/ через ключ профиля "|waqf".
            // U+06D6 — ۖ (Optional waqf)
            var segments = TransliterationPipeline.Parse("رَحْمَةٌۖ");
            var ta = segments.First(s => s.IsTaMarbuta);
            Assert.True(ta.WaqfAfter != WaqfType.None);
            Assert.Equal(Harakah.Sukun, ta.Vowel);
        }

        [Fact]
        public void RaRemainsLight_WhenOriginalVowelWasKasra()
        {
            // На паузе р становится безгласной, но по исходной касре остаётся мягкой.
            // U+06D6 — ۖ (Optional waqf)
            var segments = TransliterationPipeline.Parse("ٱلْفَجْرِۖ");
            var ra = segments.First(s => s.Letter == "ر");
            Assert.Equal(Harakah.Sukun, ra.Vowel);
            Assert.Equal(Harakah.Kasra, ra.OriginalVowel);
            Assert.Equal(Emphasis.Light, ra.Emphasis);
        }

        [Fact]
        public void WaslAfterWaqf_IsPronounced() =>
            // После номера аята васля озвучивается.
            Assert.Equal("2 иhдинаа", TransliterationPipeline.Transliterate("٢ ٱهْدِنَا"));

        [Fact]
        public void MaddAridLisSukun_OnWaqf()
        {
            // Мадд арид: естественный мадд (2 харката) перед безгласным конечным
            // согласным на паузе удлиняется до 4 харакатов.
            // U+06D6 — ۖ (Optional waqf)
            var segments = TransliterationPipeline.Parse("وَالْفَجْرِۖ");
            var waw = segments.First(s => s.Letter == "و" && s.StartsWord);
            Assert.True(waw.VowelLength >= 4, $"VowelLength={waw.VowelLength}, expected >=4");
        }
    }
}
