using Transliterator.Domain.Phonology;
using Xunit;

namespace Transliterator.Tests.RulesTests
{
    /// <summary>
    /// Стадия 3: паузальное произношение. Отдельно взятое слово тоже читается
    /// на паузе — текст на нём кончается, и остановиться чтецу больше негде.
    /// </summary>
    public class WaqfRuleTests
    {
        [Theory]
        [InlineData("ٱلرَّحِيمِ", "ар-рохIииим")] // касра
        [InlineData("ٱلْحَمْدُ", "аль-хIамд")]     // дамма
        [InlineData("أُنزِلَ", "унзиль")]          // фатха
        public void FinalShortVowel_IsDropped(string arabic, string expected) =>
            Assert.Equal(expected, TransliterationPipeline.Transliterate(arabic));

        [Fact]
        public void FinalLongVowel_Survives() =>
            // Голос обрывается на согласном, а долгой гласной обрываться не на чем.
            Assert.Equal("маа", TransliterationPipeline.Transliterate("مَا"));

        [Fact]
        public void FinalShadda_Survives() =>
            // Пауза снимает огласовку, а не удвоение.
            Assert.Equal("робб", TransliterationPipeline.Transliterate("رَبِّ"));

        [Fact]
        public void Fathatan_BecomesMaddIwad() =>
            // Мадд ивад: танвин фатхи «возмещается» долгой ā в 2 хараката.
            Assert.Equal("гъофууроо", TransliterationPipeline.Transliterate("غَفُورًا"));

        [Theory]
        [InlineData("رَحْمَةٌ", "рохIмаh")] // дамматан
        [InlineData("شَيْءٍ", "щайъ")]      // касратан
        public void DammatanAndKasratan_AreDropped(string arabic, string expected) =>
            Assert.Equal(expected, TransliterationPipeline.Transliterate(arabic));

        [Fact]
        public void TanwinNun_LeavesTheStream() =>
            // Парсер развернул танвин в «гласная + нун сакин», и снимать надо оба:
            // иначе правилам нун сакины достанется нун, которого никто не произносит.
            Assert.DoesNotContain(TransliterationPipeline.Consonants("رَحْمَةٌ"), s => s.FromTanwin);

        [Fact]
        public void TaMarbuta_BecomesH() =>
            // В соединении та же ة звучит как /t/: "рохIматан уахIукмаа".
            Assert.Equal("рохIмаh", TransliterationPipeline.Transliterate("رَحْمَةٌ"));

        [Fact]
        public void Ra_StaysLightByTheVowelThePauseRemoved()
        {
            // Твёрдая ر окрасила бы предыдущую фатху в "о" — вышло бы "qомор".
            Assert.Equal("qомар", TransliterationPipeline.Transliterate("قَمَرِ"));

            var ra = TransliterationPipeline.Consonants("ٱلْفَجْرِ").Last(s => s.Letter == "ر");

            Assert.Equal(Harakah.Sukun, ra.Vowel);
            Assert.Equal(Harakah.Kasra, ra.OriginalVowel);
            Assert.Equal(Emphasis.Light, ra.Emphasis);
        }

        [Fact]
        public void MaddArid_LengthensNaturalMadd()
        {
            // ī в ٱلْمُسْتَقِيمَ — естественный мадд в 2 хараката. Пауза обеззвучила م,
            // и слог удлиняется до среднего из трёх дозволенных чтений.
            var qaf = TransliterationPipeline.Consonants("ٱلْمُسْتَقِيمَ").First(s => s.Letter == "ق");

            Assert.Equal(4, qaf.VowelLength);
            Assert.Equal("аль-мустаqииим", TransliterationPipeline.Transliterate("ٱلْمُسْتَقِيمَ"));
        }

        [Fact]
        public void MaddArid_ExistsOnlyAtThePause() =>
            // Тот же مَـٰنِ в середине высказывания остаётся естественным маддом:
            // удлиняет его остановка, а не написание.
            Assert.Equal("ар-рохIмаани-ррохIииим",
                TransliterationPipeline.Transliterate("ٱلرَّحْمَـٰنِ ٱلرَّحِيمِ"));

        [Theory]
        [InlineData("ۗ")] // остановка предпочтительнее соединения
        [InlineData("ۘ")] // остановка обязательна
        public void StopMarks_StartANewUtterance(string mark) =>
            // После остановки читают с нуля: конечная дамма снята, а хамзат аль-васль
            // следующего слова снова звучит — "ар-", а не проглоченное "-рр".
            Assert.Equal("аль-хIамд ар-рохIмааан",
                TransliterationPipeline.Transliterate($"ٱلْحَمْدُ {mark} ٱلرَّحْمَـٰنِ"));

        [Theory]
        [InlineData("ۖ")] // соединение предпочтительнее
        [InlineData("ۙ")] // останавливаться нельзя
        [InlineData("ۚ")] // остановка лишь дозволена, ничем не предпочтена
        public void NonStopMarks_KeepReadingConnected(string mark) =>
            // По умолчанию конвейер читает слитно везде, где текст этого не запрещает,
            // и такой знак ничего не меняет: результат тот же, что и без знака.
            Assert.Equal("аль-хIамду-ррохIмааан",
                TransliterationPipeline.Transliterate($"ٱلْحَمْدُ {mark} ٱلرَّحْمَـٰنِ"));
    }

    public class WaslRuleTests
    {
        [Theory]
        [InlineData("بِسْمِ ٱللَّهِ", "бисми-лляh")]
        [InlineData("بِسْمِ ٱللَّهِ ٱلرَّحْمَـٰنِ", "бисми-лляhи-ррохIмааан")]
        public void ConnectedWasl_MergesWordsWithHyphen(string arabic, string expected) =>
            Assert.Equal(expected, TransliterationPipeline.Transliterate(arabic));

        [Fact]
        public void VerseNumber_StartsNewUtterance() =>
            // После номера аята чтение начинается заново, поэтому васля озвучивается.
            Assert.Equal("2 аль-хIамд", TransliterationPipeline.Transliterate("٢ ٱلْحَمْدُ"));

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
        [InlineData("ٱلْحَمْدُ", "аль-хIамд")]
        [InlineData("ٱلْفَجْرِ", "аль-фаджр")]
        public void MoonLetter_KeepsLam(string arabic, string expected) =>
            Assert.Equal(expected, TransliterationPipeline.Transliterate(arabic));

        [Theory]
        [InlineData("ٱلرَّحْمَـٰنِ", "ар-рохIмааан")]
        [InlineData("ٱلسَّمَآءِ", "ас-самаааъ")]
        [InlineData("ٱلَّذِينَ", "аллязъииин")]
        public void SunLetter_AssimilatesLam(string arabic, string expected) =>
            Assert.Equal(expected, TransliterationPipeline.Transliterate(arabic));

        [Fact]
        public void MoonLam_SurvivesWaslMerge() =>
            // Прежде стадия васли съедала лям артикля целиком: "робби-'аалямиина".
            Assert.Equal("робби-ль-'аалямииин",
                TransliterationPipeline.Transliterate("رَبِّ ٱلْعَـٰلَمِينَ"));

        [Fact]
        public void MoonLam_ProducesSingleSoftSign() =>
            // Прежде правило мягкого знака шло после артикля и давало "альь-".
            Assert.DoesNotContain("ьь", TransliterationPipeline.Transliterate("ٱلْحَمْدُ"));
    }

    public class EmphasisRuleTests
    {
        [Theory]
        [InlineData("رِزْقِ", "ризq")]        // касра — таркик
        [InlineData("ٱلْفَجْرِ", "аль-фаджр")] // касра — таркик
        [InlineData("رَبِّ", "робб")]          // фатха — тафхим
        public void Ra_EmphasisFollowsItsHarakah(string arabic, string expected) =>
            Assert.Equal(expected, TransliterationPipeline.Transliterate(arabic));

        [Fact]
        public void SakinEmphatic_ColoursPrecedingVowel() =>
            // Эмфаза распространяется и назад: прежде правило смотрело только вперёд.
            Assert.Equal("бор", TransliterationPipeline.Transliterate("بَر"));

        [Fact]
        public void LamOfAllah_IsLightAfterKasra() =>
            // "лилляh", а не "лиллаhи". Прежний хак искал в кириллице "Аллах"
            // и не срабатывал никогда, потому что ه отображается в "h".
            Assert.Equal("лилляh", TransliterationPipeline.Transliterate("لِلَّهِ"));
    }

    public class LetterCoverageTests
    {
        [Fact]
        public void Hamza_IsNotDropped() =>
            Assert.Equal("qуръааан", TransliterationPipeline.Transliterate("قُرْءَانِ"));

        [Fact]
        public void TaMarbutaAndTanwin_AreNotDropped() =>
            // В соединении ة звучит как /t/, а танвин — как настоящий нун.
            // На паузе обе буквы читаются иначе, поэтому проверка идёт на слитном стыке.
            Assert.Equal("рохIматан уахIукмаа",
                TransliterationPipeline.Transliterate("رَحْمَةً وَحُكْمًا"));

        [Fact]
        public void HamzaCarrier_KeepsItsOwnHarakah() =>
            // Прежде огласовка носителя гасилась и أُ читалось как "а".
            Assert.Equal("унзиль", TransliterationPipeline.Transliterate("أُنزِلَ"));

        [Fact]
        public void EmphaticAtEndOfText_DoesNotThrow()
        {
            // Прежде выход за границы массива ронял приложение на любом тексте,
            // заканчивающемся эмфатической буквой.
            foreach (var word in new[] { "بَر", "قَط", "نَارٌ", "ٱنظُرْ" })
                Assert.False(string.IsNullOrEmpty(TransliterationPipeline.Transliterate(word)));
        }
    }
}
