using Transliterator.Core.Repositories;
using Transliterator.Core.Services.Rules;
using Transliterator.Domain.Phonology;
using Xunit;

namespace Transliterator.Tests.RulesTests
{
    /// <summary>
    /// Ступени чтения (I2): полный таджвид, варианты внутри таджвида и обычное
    /// чтение без таджвида. Раскладка правил по ступеням — в каталоге (I1),
    /// эти тесты проверяют, что конвейер её исполняет.
    /// </summary>
    public class ReadingTiersTests
    {
        private static string Plain(string arabic) =>
            TransliterationPipeline.Transliterate(arabic, reading: ReadingOptions.Plain);

        private static ReadingOptions WithCount(string ruleId, int count) =>
            new() { Counts = new Dictionary<string, int> { [ruleId] = count } };

        // ---- Обычное чтение ----

        [Theory]
        [InlineData("مِن رَّبِّهِمْ", "мин роббиhим")]            // идгам без гунны
        [InlineData("مَن يَقُولُ", "ман йаqууль")]                  // идгам с гунной
        [InlineData("عَبَدتُّمْ", "'абадтум")]                      // идгам мутаджанисайн
        [InlineData("مِنۢ بَعْدِ", "мин ба'д")]                      // икляб
        [InlineData("وَلَا ٱلضَّآلِّينَ", "уаля-дIдIооллиин")]        // мадд лязим, удвоение остаётся
        [InlineData("جَآءَ", "джааъ")]                               // мадд муттасиль
        [InlineData("لَآ أَعْبُدُ", "ляя а'буд")]                     // мадд мунфасиль
        [InlineData("إِنَّهُۥ كَانَ", "иннаhу каан")]                 // мадд силя, размеченная мусхафом
        [InlineData("لَهُ مَا", "ляhу маа")]                         // мадд силя без разметки
        public void Plain_DropsTajweedOnlyRules(string arabic, string expected) =>
            Assert.Equal(expected, Plain(arabic));

        [Theory]
        [InlineData("بِسْمِ ٱللَّهِ ٱلرَّحْمَٰنِ ٱلرَّحِيمِ")]   // васля, солнечный лям, лям имени Аллаха
        [InlineData("ٱلْحَمْدُ لِلَّهِ رَبِّ ٱلْعَٰلَمِينَ")]     // лунный лям, тафхим ра
        [InlineData("قُلْ أَعُوذُ بِرَبِّ ٱلْفَلَقِ")]           // изхар, вакф
        public void Plain_KeepsTheBase(string arabic) =>
            // Где правил ступени «таджвид» нет, обычное чтение совпадает с полным.
            Assert.Equal(TransliterationPipeline.Transliterate(arabic), Plain(arabic));

        [Theory]
        [InlineData("مِن نُّورٍ")]     // нун с нуном
        [InlineData("لَهُم مَّا")]     // мим с мимом — идгам мисляйн
        [InlineData("قَد دَّخَلُوا")]  // одинаковые не носовые
        public void Plain_StillMergesIdenticalLetters(string arabic) =>
            // Два одинаковых согласных подряд сливаются и без таджвида.
            Assert.Single(TransliterationPipeline.Consonants(arabic, ReadingOptions.Plain), s => s.IsGeminateFirstHalf);

        [Fact]
        public void Plain_HasNoNasalisationAndNoEcho()
        {
            var segments = TransliterationPipeline.Consonants("أَنتُمْ بِهِۦ أَحَدٌ", ReadingOptions.Plain);

            Assert.DoesNotContain(segments, s => s.Ghunna);
            Assert.DoesNotContain(segments, s => s.Qalqalah != Qalqalah.None);
        }

        public static IEnumerable<object[]> TajweedOnlyExamples =>
            EmbeddedTajweedRuleCatalog.Catalog.Rules
                .Where(r => r.Tier == Transliterator.Domain.Entities.TajweedTier.Tajweed)
                .SelectMany(r => r.Examples.Select(e => new object[] { r.Id, e.Arabic }));

        [Theory]
        [MemberData(nameof(TajweedOnlyExamples))]
        public void Plain_UndoesEveryTajweedOnlyExample(string ruleId, string arabic)
        {
            // Пример правила ступени «таджвид» показывает, что правило сделало. Без
            // таджвида от этого не должно остаться следа — хотя бы в сегментах: гунну
            // и отзвук Standard не пишет, и по тексту их отсутствия не видно.
            Assert.NotEqual(Sounds(arabic, ReadingOptions.FullTajweed), Sounds(arabic, ReadingOptions.Plain));
            Assert.NotNull(ruleId);
        }

        /// <summary>Всё, что правила решают о звуке согласного, одной строкой.</summary>
        private static string Sounds(string arabic, ReadingOptions reading) =>
            string.Join(" ", TransliterationPipeline.Consonants(arabic, reading).Select(s =>
                $"{s.Letter}{(s.Shadda ? "ّ" : "")}{s.Vowel}x{s.VowelLength}" +
                $"{(s.Ghunna ? "~" : "")}{(s.Qalqalah != Qalqalah.None ? "!" : "")}{(s.IsGeminateFirstHalf ? "=" : "")}"));

        [Fact]
        public void FullTajweed_IsTheDefault() =>
            Assert.Equal(TransliterationPipeline.Transliterate("مِن رَّبِّهِمْ"),
                         TransliterationPipeline.Transliterate("مِن رَّبِّهِمْ", reading: ReadingOptions.FullTajweed));

        // ---- Варианты внутри таджвида ----

        [Theory]
        [InlineData(2, "ар-рохIиим")]
        [InlineData(4, "ар-рохIииим")]
        [InlineData(6, "ар-рохIиииим")]
        public void MaddArid_TakesTheChosenCount(int count, string expected) =>
            Assert.Equal(expected, TransliterationPipeline.Transliterate(
                "ٱلرَّحِيمِ", reading: WithCount(TajweedRuleIds.MaddArid, count)));

        [Fact]
        public void MaddArid_DoesNotShortenMuttasilAtPause()
        {
            // ٱلسَّمَآءِ на паузе: муттасиль в четыре счёта, арид в два его не укорачивает.
            var mim = TransliterationPipeline.Consonants("ٱلسَّمَآءِ").First(s => s.Letter == "م");

            Assert.Equal(4, mim.VowelLength);
        }

        [Fact]
        public void MaddArid_OnlyAtPause()
        {
            // В соединении последняя буква слова звучит, и ариду места нет.
            var reading = WithCount(TajweedRuleIds.MaddArid, 6);
            var ha = TransliterationPipeline.Consonants("ٱلرَّحِيمِ مَٰلِكِ", reading).First(s => s.Letter == "ح");

            Assert.Equal(2, ha.VowelLength);
        }

        [Theory]
        [InlineData(4)]
        [InlineData(6)]
        public void MaddLin_TakesTheChosenCount(int count)
        {
            var waw = TransliterationPipeline.Consonants("خَوْفٌ", WithCount(TajweedRuleIds.MaddLin, count))
                                             .First(s => s.Letter == "و");

            Assert.Equal(count, waw.VowelLength);
        }

        [Fact]
        public void MuttasilAndMunfasil_TakeTheirOwnCounts()
        {
            // Муттасиль — хамза своего слова, мунфасиль — следующего: счёт у каждого свой.
            var reading = new ReadingOptions
            {
                Counts = new Dictionary<string, int>
                {
                    [TajweedRuleIds.MaddMuttasil] = 4,
                    [TajweedRuleIds.MaddMunfasil] = 5,
                }
            };

            var segments = TransliterationPipeline.Consonants("جَآءَ مَآ أَنزَلَ", reading);

            Assert.Equal(4, segments.First(s => s.Letter == "ج").VowelLength);   // муттасиль
            Assert.Equal(5, segments.First(s => s.Letter == "م").VowelLength);   // мунфасиль
        }

        [Fact]
        public void EveryRuleWithCounts_IsReadByThePipeline()
        {
            // Правило с вариантами в каталоге, которого конвейер не читает, дало бы
            // на странице выбор, ничего не меняющий.
            var withCounts = EmbeddedTajweedRuleCatalog.Catalog.Rules.Where(r => r.HasVariants).Select(r => r.Id);

            Assert.Equal(withCounts.Order(), TajweedRuleIds.WithCounts.Order());
        }

        [Fact]
        public void EveryCountOfTheCatalog_ReachesTheSegment()
        {
            // Каждый допустимый счёт каждого правила доходит до сегмента как есть.
            var cases = new Dictionary<string, (string Arabic, string Letter)>
            {
                [TajweedRuleIds.MaddArid] = ("ٱلرَّحِيمِ", "ح"),
                [TajweedRuleIds.MaddLin] = ("خَوْفٌ", "و"),
                [TajweedRuleIds.MaddMuttasil] = ("جَآءَ", "ج"),
                [TajweedRuleIds.MaddMunfasil] = ("لَآ أَعْبُدُ", "ل"),
            };

            foreach (var rule in EmbeddedTajweedRuleCatalog.Catalog.Rules.Where(r => r.HasVariants))
            {
                var (arabic, letter) = cases[rule.Id];

                foreach (var count in rule.Counts)
                {
                    var segment = TransliterationPipeline.Consonants(arabic, WithCount(rule.Id, count))
                                                         .First(s => s.Letter == letter);

                    Assert.Equal(count, segment.VowelLength);
                }
            }
        }
    }
}
