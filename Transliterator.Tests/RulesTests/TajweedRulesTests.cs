using Transliterator.Domain.Entities;
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
        [InlineData("شَيْءٍ", "щайййъ")]    // касратан; ي при этом получает мадд лин
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
        // Имя Аллаха записано без надстрочного алифа, но читается с долгой ā и так:
        // её восстанавливает нормализация, а на паузе её тянет ещё и мадд арид.
        [InlineData("بِسْمِ ٱللَّهِ", "бисми-лляяяh")]
        [InlineData("بِسْمِ ٱللَّهِ ٱلرَّحْمَـٰنِ", "бисми-лляяhи-ррохIмааан")]
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

    /// <summary>
    /// Стадия 6: нун сакина, танвин и мим сакина. Танвин отдельных проверок не требует —
    /// парсер развернул его в нун ещё до правил, и это тот же нун сакина.
    /// </summary>
    public class NasalRuleTests
    {
        [Fact]
        public void ThroatLetter_KeepsNunClear()
        {
            // Изхар халькы: гортанной букве носовой призвук передать нечем.
            var nun = Assert.Single(TransliterationPipeline.Consonants("مَنْ عَمِلَ"), s => s.Letter == "ن");

            Assert.False(nun.Ghunna);
            Assert.Equal("ман 'амиль", TransliterationPipeline.Transliterate("مَنْ عَمِلَ"));
        }

        [Fact]
        public void Ihfa_NasalizesNunWithoutChangingIt()
        {
            // Ихфа: нун не сливается и не исчезает. Огласовки на нём в أُنزِلَ
            // не написано вовсе — в мусхафе это и означает безгласность.
            var nun = Assert.Single(TransliterationPipeline.Consonants("أُنزِلَ"), s => s.Letter == "ن");

            Assert.True(nun.Ghunna);
            Assert.False(nun.IsGeminateFirstHalf);
            Assert.Equal("унзиль", TransliterationPipeline.Transliterate("أُنزِلَ"));
        }

        [Theory]
        [InlineData("مِن رَّبِّهِمْ", "мир-роббиhим")]             // без гунны: ر
        [InlineData("مِن نُّطْفَةٍ", "мин-нутIфаh")]               // с гунной: ن
        [InlineData("رَحْمَةً وَحُكْمًا", "рохIматау-уахIукмаа")] // танвин сливается наравне с написанным нуном
        public void Idgham_TurnsNunIntoTheNextLetter(string arabic, string expected) =>
            // Нун не исчезает, а становится следующей буквой, и дефис приходится
            // между двумя её копиями — как у солнечного ляма в "ар-рохIмаан".
            Assert.Equal(expected, TransliterationPipeline.Transliterate(arabic));

        [Fact]
        public void IdghamIntoRa_LeavesNoGhunna()
        {
            var merged = Assert.Single(TransliterationPipeline.Consonants("مِن رَّبِّهِمْ"),
                                       s => s.IsGeminateFirstHalf);

            Assert.Equal("ر", merged.Letter);
            Assert.False(merged.Ghunna);
        }

        [Fact]
        public void Idgham_DecidesEmphasisOfBothHalves()
        {
            // Твёрдость ر определяется только после слияния: до него на этом месте
            // стоит نْ, а у безгласной первой половины своей огласовки нет — решает
            // вторая. Иначе первая половина взяла бы мягкость у касры مِن.
            foreach (var half in TransliterationPipeline.Consonants("مِن رَّبِّهِمْ").Where(s => s.Letter == "ر"))
                Assert.Equal(Emphasis.Heavy, half.Emphasis);
        }

        [Theory]
        [InlineData("مِنۢ بَعْدِ")] // знак икляба стоит вместо сукуна
        [InlineData("مِنْ بَعْدِ")] // сукун написан явно
        public void Iqlab_TurnsNunIntoMeem(string arabic)
        {
            // Знак икляба несёт звук, а не совет чтецу: без него, если бы его
            // выбросила нормализация, нун в مِنۢ был бы неотличим от неогласованного.
            Assert.Equal("мим ба'д", TransliterationPipeline.Transliterate(arabic));
            Assert.DoesNotContain(TransliterationPipeline.Consonants(arabic), s => s.Letter == "ن");
        }

        [Fact]
        public void IdghamLetterInsideOneWord_DoesNotMerge()
        {
            // Изхар мутлак: внутри слова идгама не бывает — иначе دُنْيَا читалось бы
            // с удвоением, и корень стал бы неузнаваем.
            Assert.Equal("дунйаа", TransliterationPipeline.Transliterate("دُنْيَا"));
            Assert.DoesNotContain(TransliterationPipeline.Consonants("دُنْيَا"), s => s.IsGeminateFirstHalf);
        }

        [Fact]
        public void MeemSakina_MergesIntoMeem() =>
            // Идгам мисляйн: два мима сливаются в один долгий носовой.
            Assert.Equal("ляhум-могъфироh", TransliterationPipeline.Transliterate("لَهُم مَّغْفِرَةٌ"));

        [Fact]
        public void MeemSakina_IsNasalizedBeforeBaAndClearElsewhere()
        {
            // Ихфа шафави против изхара шафави. Кириллица этой разницы не пишет,
            // но помета доходит до рендерера — графему выбирает профиль.
            var beforeBa = Assert.Single(TransliterationPipeline.Consonants("وَمَا هُم بِمُؤْمِنِينَ"),
                                         s => s.Letter == "م" && s.Vowel is Harakah.Sukun or Harakah.None);
            var beforeDal = Assert.Single(TransliterationPipeline.Consonants("ٱلْحَمْدُ"), s => s.Letter == "م");

            Assert.True(beforeBa.Ghunna);
            Assert.False(beforeDal.Ghunna);
        }

        [Fact]
        public void Ghunna_TakesItsGraphemeFromTheProfile()
        {
            // Standard пишет гунну обычными н и м — это выбор системы записи,
            // а не решение конвейера: правило только помечает звук.
            var profile = new TransliterationProfile
            {
                Name = "ghunna",
                Rules = new Dictionary<string, string>(TestProfiles.Standard.Rules) { ["ن|ghunna"] = "н̃" }
            };

            Assert.Equal("ун̃зиль", TransliterationPipeline.Transliterate("أُنزِلَ", profile));
        }
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
        public void LamOfAllah_IsHeavyAfterFatha() =>
            // Удвоение здесь разложено на два сегмента: лям артикля слился со вторым
            // лямом. Решает та половина, что несёт огласовку, — иначе безгласная
            // первая половина отдала бы имени мягкий лям: "ал-ляяяh".
            Assert.Equal("qооля-ллаааh", TransliterationPipeline.Transliterate("قَالَ ٱللَّٰهُ"));

        [Theory]
        [InlineData("ٱرْحَمْ")]        // в начале высказывания васля звучит
        [InlineData("رَبِّ ٱرْحَمْ")] // в соединении она нема
        [InlineData("ٱرْجِعِي")]
        public void Ra_AfterWaslKasra_IsHeavy(string arabic)
        {
            // Касра хамзат аль-васль привнесена и р не смягчает: иначе одно и то же
            // слово читалось бы по-разному в начале высказывания и в середине.
            var ra = TransliterationPipeline.Consonants(arabic).First(s => s.Letter == "ر");

            Assert.Equal(Emphasis.Heavy, ra.Emphasis);
        }

        [Fact]
        public void Ra_AfterTrueKasra_StaysLight()
        {
            // Для контраста: в فِرْعَوْنَ касра коренная, и безгласная р остаётся мягкой.
            var ra = TransliterationPipeline.Consonants("فِرْعَوْنَ").First(s => s.Letter == "ر");

            Assert.Equal(Emphasis.Light, ra.Emphasis);
        }

        [Fact]
        public void LamOfAllah_IsLightAfterKasra() =>
            // "лилляяяh", а не "лиллаааh". Прежний хак искал в кириллице "Аллах"
            // и не срабатывал никогда, потому что ه отображается в "h".
            Assert.Equal("лилляяяh", TransliterationPipeline.Transliterate("لِلَّهِ"));
    }

    /// <summary>
    /// Стадия 8: длительность мадда. Долгота живёт в харакатах, поэтому проверять
    /// её честнее по сегментам — в кириллице все длительности выше двух
    /// сливаются в «побольше букв».
    /// </summary>
    public class MaddRuleTests
    {
        [Theory]
        [InlineData("ءَامَنَ")]
        [InlineData("إِيمَان")]
        [InlineData("أُوتِيَ")]
        public void MaddBadal_StaysNatural(string arabic)
        {
            // Долгая гласная после хамзы тянется два хараката, а не четыре:
            // муттасиль и мунфасиль требуют хамзы после гласной, а не перед ней.
            var hamza = TransliterationPipeline.Consonants(arabic).First(s => s.Letter == "ء");

            Assert.Equal(2, hamza.VowelLength);
        }

        [Theory]
        [InlineData("خَوْفٌ", "و")]
        [InlineData("قُرَيْشٍ", "ي")]
        [InlineData("فِرْعَوْنَ", "و")]
        public void MaddLin_LengthensGlideAtPause(string arabic, string glide)
        {
            // Пауза обеззвучивает последний согласный, слог закрывается внезапно —
            // и голос отыгрывается на глайде, а не на фатхе перед ним.
            var segment = TransliterationPipeline.Consonants(arabic).First(s => s.Letter == glide);

            Assert.Equal(4, segment.VowelLength);
        }

        [Fact]
        public void MaddLin_NeedsSukunFromPause()
        {
            // عَلَيْهِمْ: сукун написан, а не наведён паузой. Слог закрыт им и в слитном
            // чтении, поэтому удлинять нечего — это простой дифтонг.
            var ya = TransliterationPipeline.Consonants("عَلَيْهِمْ").First(s => s.Letter == "ي");

            Assert.Equal(1, ya.VowelLength);
        }

        [Fact]
        public void MaddLin_ReachesTheOutput() =>
            // Долгота на глайде выражается повтором его же графемы: тянется و, а не фатха.
            Assert.Equal("хъоуууф", TransliterationPipeline.Transliterate("خَوْفٌ"));

        [Fact]
        public void MaddSilaSughra_LengthensPronounHa() =>
            // Местоименная ه между двумя огласованными буквами тянется два хараката,
            // даже когда мусхаф не разметил её малым вавом.
            Assert.Equal("ляhуу маа", TransliterationPipeline.Transliterate("لَهُ مَا"));

        [Fact]
        public void MaddSilaKubra_IsFourHarakat()
        {
            // Перед хамзой силя удлиняется до четырёх — тем же правилом мунфасиля,
            // что удлиняет всякую долгую гласную перед хамзой соседнего слова.
            var ha = TransliterationPipeline.Consonants("لَهُ أَخْلَدَ").First(s => s.Letter == "ه");

            Assert.Equal(4, ha.VowelLength);
        }

        [Theory]
        [InlineData("مِنْهُ مَا")]      // перед ه сукун
        [InlineData("فِيهِ مَا")]       // перед ه долгая ī
        [InlineData("لَهُ ٱلْمُلْكُ")]   // после ه безгласный лям
        [InlineData("ٱللَّٰهُ أَحَدٌ")]    // ه имени Аллаха — коренная, а не местоимение
        public void MaddSila_NeedsTwoMovementsOfVoice(string arabic)
        {
            var ha = TransliterationPipeline.Consonants(arabic).First(s => s.Letter == "ه");

            Assert.Equal(1, ha.VowelLength);
        }

        [Fact]
        public void MaddSila_HasItsExceptions()
        {
            // يَرْضَهُ لَكُمْ Хафс читает короткой даммой, хотя условия налицо…
            var yardahu = TransliterationPipeline.Consonants("يَرْضَهُ لَكُمْ").First(s => s.Letter == "ه");
            Assert.Equal(1, yardahu.VowelLength);

            // …а فِيهِ مُهَانًا — с силёй, хотя перед ه стоит долгая ī.
            var fihi = TransliterationPipeline.Consonants("فِيهِ مُهَانًا").First(s => s.Letter == "ه");
            Assert.Equal(2, fihi.VowelLength);
        }
    }

    /// <summary>
    /// Стадия 9: кальканя. Отзвук — не буква и не огласовка, поэтому проверяется
    /// по пометке на сегменте; в Standard она до письма не доходит осознанно,
    /// и как выглядит написанный отзвук, показывает отдельный профиль.
    /// </summary>
    public class QalqalahRuleTests
    {
        [Theory]
        [InlineData("يَجْعَلُونَ", "ج")]
        [InlineData("ٱلْفَجْرِ", "ج")]
        [InlineData("أَدْبَرَ", "د")]
        [InlineData("يَطْمَعُ", "ط")]
        [InlineData("يَقْتُلُونَ", "ق")]
        public void SakinLetter_GetsMinorQalqalah(string arabic, string letter)
        {
            // Кальканя сугра: безгласный взрывной посреди слова. Отзвук есть,
            // но следующий слог его тут же гасит.
            var segment = TransliterationPipeline.Consonants(arabic).First(s => s.Letter == letter);

            Assert.Equal(Qalqalah.Minor, segment.Qalqalah);
        }

        [Theory]
        [InlineData("خَلَقَ", "ق")]    // огласовку снял вакф
        [InlineData("أَحَدْ", "د")]    // сукун написан, и снимать вакфу нечего
        [InlineData("ٱلْفَلَقِ", "ق")]
        public void LetterAtPause_GetsMajorQalqalah(string arabic, string letter)
        {
            // Кальканя кубра: за буквой не звучит уже ничего, и гасить отзвук нечем.
            var segment = TransliterationPipeline.Consonants(arabic).Last(s => s.Letter == letter);

            Assert.Equal(Qalqalah.Major, segment.Qalqalah);
        }

        [Fact]
        public void WordFinalLetter_StaysMinorWhenReadingContinues()
        {
            // Та же безгласная د в конце слова: остановки нет, следующее слово звучит —
            // и отзвук остаётся слабым. Степень решает положение, а не конец слова.
            var dal = TransliterationPipeline.Consonants("قَدْ أَفْلَحَ").First(s => s.Letter == "د");

            Assert.Equal(Qalqalah.Minor, dal.Qalqalah);
        }

        [Fact]
        public void LetterBeforeStopMark_IsMajor()
        {
            // Слово следом написано, но чтение до него не доходит: знак ۘ требует
            // остановки, и буква оказывается последней в высказывании.
            var dal = TransliterationPipeline.Consonants("قَدْ ۘ أَفْلَحَ").First(s => s.Letter == "د");

            Assert.Equal(Qalqalah.Major, dal.Qalqalah);
        }

        [Theory]
        [InlineData("قَالَ", "ق")]
        [InlineData("بَقَرَةٌ", "ب")]
        public void VowelledLetter_HasNoQalqalah(string arabic, string letter)
        {
            // Отзвук берётся из размыкания смычки в тишину. Огласованной букве
            // размыкаться есть во что.
            var segment = TransliterationPipeline.Consonants(arabic).First(s => s.Letter == letter);

            Assert.Equal(Qalqalah.None, segment.Qalqalah);
        }

        [Fact]
        public void FirstHalfOfIdgham_HasNoQalqalah() =>
            // ٱلدِّينِ: лям артикля стал первой половиной удвоенной د. Она не размыкается,
            // а переходит во вторую — размыкание одно, и оно принадлежит второй половине.
            Assert.All(TransliterationPipeline.Consonants("ٱلدِّينِ"),
                s => Assert.Equal(Qalqalah.None, s.Qalqalah));

        [Fact]
        public void Grapheme_ComesFromTheProfile()
        {
            // Standard отзвук не пишет; профиль, который пишет, получает его
            // без единой правки в правилах.
            var profile = WithQalqalah("э", strong: "э̄");

            Assert.Equal("qодэ афляхI", TransliterationPipeline.Transliterate("قَدْ أَفْلَحَ", profile));
            Assert.Equal("хъолоqэ̄", TransliterationPipeline.Transliterate("خَلَقَ", profile));
        }

        [Fact]
        public void StrongGrapheme_FallsBackToThePlainOne() =>
            // Различать степени на письме профиль не обязан: кальканя кубра — тот же
            // отзвук, только громче.
            Assert.Equal("хъолоqэ",
                TransliterationPipeline.Transliterate("خَلَقَ", WithQalqalah("э", strong: null)));

        [Fact]
        public void DoubledLetterAtPause_EchoesOnce() =>
            // وَتَبَّ: удвоение звучит одной долгой смычкой и размыкается один раз,
            // поэтому отзвук идёт после обеих графем, а не после каждой.
            Assert.Equal("уатаббэ",
                TransliterationPipeline.Transliterate("وَتَبَّ", WithQalqalah("э", strong: null)));

        /// <summary>Standard с дописанным отзвуком: правила те же, различается только письмо.</summary>
        private static TransliterationProfile WithQalqalah(string echo, string? strong)
        {
            var profile = new TransliterationProfile("Qalqalah", "Standard, пишущий отзвук кальканя")
            {
                Rules = new Dictionary<string, string>(TestProfiles.Standard.Rules)
            };

            foreach (var letter in new[] { "ق", "ط", "ب", "ج", "د" })
            {
                profile.Rules[$"{letter}|qalqalah"] = echo;

                if (strong is not null)
                    profile.Rules[$"{letter}|qalqalah-strong"] = strong;
            }

            return profile;
        }
    }

    /// <summary>
    /// Имя Аллаха в современной орфографии: «ٱللَّهُ» вместо «ٱللَّٰهُ».
    /// Долготу в этом слове даёт только надстрочный алиф, и без него имя рассыпается
    /// сразу по трём стадиям — оттого проверки собраны в один класс, а не разложены
    /// по стадиям.
    /// </summary>
    public class NameOfAllahTests
    {
        [Theory]
        [InlineData("ٱللَّهُ", "ٱللَّٰهُ")]
        [InlineData("قَالَ ٱللَّهُ", "قَالَ ٱللَّٰهُ")]
        [InlineData("بِسْمِ ٱللَّهِ", "بِسْمِ ٱللَّٰهِ")]
        [InlineData("لِلَّهِ", "لِلَّٰهِ")]
        [InlineData("ٱللَّهُمَّ", "ٱللَّٰهُمَّ")]
        public void BothSpellings_ReadAlike(string modern, string uthmani) =>
            // Нормализация сводит два написания к одному чтению. Иначе про орфографию
            // пришлось бы знать каждой стадии, которая опирается на эту долготу.
            Assert.Equal(TransliterationPipeline.Transliterate(uthmani),
                TransliterationPipeline.Transliterate(modern));

        [Fact]
        public void ModernSpelling_KeepsTheLongVowel() =>
            // Прежде выходило «qуль hууа-лляhууу ахIад»: слог لَّ оставался кратким,
            // а конечная ه получала мадд силя, которого у коренной буквы не бывает.
            Assert.Equal("qуль hууа-ллааhу ахIад",
                TransliterationPipeline.Transliterate("قُلْ هُوَ ٱللَّهُ أَحَدٌ"));

        [Fact]
        public void Ha_OfTheName_IsNotAPronoun()
        {
            // Мадд силя живёт между двумя движениями голоса, а перед этой ه стоит
            // долгая ā — значит, и без надстрочного алифа силе взяться неоткуда.
            var ha = TransliterationPipeline.Consonants("ٱللَّهُ أَحَدٌ").First(s => s.Letter == "ه");

            Assert.Equal(1, ha.VowelLength);
        }

        [Theory]
        [InlineData("قَالَ ٱللَّهُ", Emphasis.Heavy)] // фатха перед лямом
        [InlineData("لِلَّهِ", Emphasis.Light)] // касра перед лямом
        public void Lam_KeepsItsEmphasisWithoutSuperscriptAlef(string arabic, Emphasis expected)
        {
            // Лям имени Аллаха стадия 7 узнаёт по долгой ā при нём. Без алифа это
            // обычный лям, и твёрдым он не станет ни при какой огласовке.
            var lam = TransliterationPipeline.Consonants(arabic).Last(s => s.Letter == "ل");

            Assert.Equal(expected, lam.Emphasis);
        }

        [Fact]
        public void DoubledLamBeforeHa_IsNotAlwaysTheName()
        {
            // «قُل لَّهُ مَا» — это «скажи ему»: то же لَّ, но долготы в нём нет,
            // а ه здесь как раз местоименная и силю получает.
            var segments = TransliterationPipeline.Consonants("قُل لَّهُ مَا");

            Assert.Equal(1, segments.Last(s => s.Letter == "ل").VowelLength);
            Assert.Equal(2, segments.First(s => s.Letter == "ه").VowelLength);
        }
    }

    public class LetterCoverageTests
    {
        [Fact]
        public void Hamza_IsNotDropped() =>
            Assert.Equal("qуръааан", TransliterationPipeline.Transliterate("قُرْءَانِ"));

        [Fact]
        public void TaMarbutaAndTanwin_AreNotDropped() =>
            // В соединении ة звучит как /t/, а танвин — как настоящий нун.
            // На паузе обе буквы читаются иначе, поэтому проверка идёт на слитном стыке,
            // и следующее слово начинается с гортанной: перед ней нун остаётся нуном.
            // В прежнем "رَحْمَةً وَحُكْمًا" он сливается с و — это уже идгам стадии 6.
            Assert.Equal("рохIматин 'аляйhим",
                TransliterationPipeline.Transliterate("رَحْمَةٍ عَلَيْهِمْ"));

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
