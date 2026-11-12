using System.Text.RegularExpressions;
using Transliterator.Core.Repositories;
using Transliterator.Core.Services;
using Transliterator.Core.Services.Rules;
using Transliterator.Domain.Entities;
using Transliterator.Domain.Exceptions;
using Transliterator.Domain.Localization;
using Xunit;

namespace Transliterator.Tests.RulesTests
{
    /// <summary>
    /// Каталог правил таджвида (I1) — обещание человеку, такое же, как подсказка
    /// страницы в <c>PageHintTests</c>: описанное правило должно быть в конвейере,
    /// а показанный пример — давать ровно показанный вывод.
    /// </summary>
    public class TajweedRuleCatalogTests
    {
        private static TajweedRuleCatalog Catalog => EmbeddedTajweedRuleCatalog.Catalog;

        /// <summary>Номер первой стадии, которую исполняет <see cref="RulesService"/>.</summary>
        private const int FirstRuleStage = 3;

        public static IEnumerable<object[]> Examples =>
            EmbeddedTajweedRuleCatalog.Catalog.Rules
                .SelectMany(rule => rule.Examples.Select(example => new object[] { rule.Id, example.Arabic }));

        [Fact]
        public void EveryStageOfThePipeline_HasItsEntry_InPipelineOrder()
        {
            // Стадии конвейера — это параметры конструктора RulesService, в порядке
            // исполнения. Новая стадия без записи в каталоге роняет этот тест,
            // а не пропадает со страницы выбора правил молча.
            var pipeline = typeof(RulesService).GetConstructors().Single()
                                               .GetParameters()
                                               .Select(p => p.ParameterType.Name)
                                               .ToList();

            Assert.Equal(pipeline, Catalog.Stages.Select(s => s.Class));
            Assert.Equal(Enumerable.Range(FirstRuleStage, pipeline.Count), Catalog.Stages.Select(s => s.Number));
        }

        [Fact]
        public void EveryStage_HasRules_AndRulesFollowStageOrder()
        {
            var ruleStages = Catalog.Rules.Select(r => r.Stage).ToList();

            Assert.Equal(ruleStages.OrderBy(n => n), ruleStages);
            Assert.All(Catalog.Stages, stage => Assert.Contains(stage.Number, ruleStages));
        }

        [Theory]
        [MemberData(nameof(Examples))]
        public void Example_IsWhatThePipelineGives(string ruleId, string arabic)
        {
            var example = Catalog.Rules.Single(r => r.Id == ruleId).Examples.First(e => e.Arabic == arabic);

            Assert.Equal(example.Transliteration, TransliterationPipeline.Transliterate(arabic));
        }

        [Fact]
        public void ArabicName_IsWrittenInArabic()
        {
            var arabic = new Regex(@"^[؀-ۿ ]+$");

            Assert.All(Catalog.Rules, rule => Assert.Matches(arabic, rule.ArabicName));
        }

        /// <summary>
        /// Перевод названия на каждом языке страницы — буквами этого языка, со строчной
        /// буквы. Третий язык страницы без строки здесь роняет тест, а не проходит молча.
        /// </summary>
        private static readonly Dictionary<string, Regex> LowercaseWords = new()
        {
            ["ru"] = new Regex(@"^[а-яё][а-яё «»-]*$"),
            ["en"] = new Regex(@"^[a-z][a-z ""'-]*$"),
        };

        [Fact]
        public void EveryRule_HasATranslation_InEveryPageLanguage_ThatIsNotItsName()
        {
            // Перевод названия (I7) — то, что поймёт человек без арабского: короткий,
            // на языке страницы, со строчной буквы (на странице он стоит в скобках после
            // названия) и не повтор самого названия (D7: на каждом языке).
            Assert.Equal(PageLanguages.All.Select(l => l.Code).Order(), LowercaseWords.Keys.Order());

            Assert.All(Catalog.Rules, rule =>
            {
                foreach (var language in PageLanguages.All)
                {
                    var translation = rule.Translation.For(language.Code);

                    Assert.Matches(LowercaseWords[language.Code], translation);
                    Assert.NotEqual(rule.Name.For(language.Code), translation, StringComparer.OrdinalIgnoreCase);
                    Assert.True(translation.Length <= 40, $"translation of '{rule.Id}' is not short: {translation}");
                }
            });
        }

        [Fact]
        public void EveryText_OfTheCatalog_ExistsInEveryPageLanguage()
        {
            // Загрузчик и сам отказывается от каталога без перевода; тест говорит,
            // чего именно не хватает, а не только что каталог не загрузился.
            var texts = Catalog.Stages.Select(stage => ($"stage {stage.Number} name", stage.Name))
                .Concat(Catalog.Rules.SelectMany(rule => new[]
                {
                    ($"{rule.Id} name", rule.Name),
                    ($"{rule.Id} translation", rule.Translation),
                    ($"{rule.Id} description", rule.Description),
                }));

            Assert.All(texts, text => Assert.Empty(text.Item2.MissingLanguages()));
        }

        [Fact]
        public void EnglishTexts_AreNotRussian()
        {
            // Английский текст, скопированный из русского, тоже «есть» — но не перевод.
            var cyrillic = new Regex("[а-яА-ЯёЁ]");

            Assert.All(Catalog.Rules, rule =>
            {
                Assert.DoesNotMatch(cyrillic, rule.Name.For("en"));
                Assert.DoesNotMatch(cyrillic, rule.Description.For("en"));
            });
        }

        [Fact]
        public void Translation_IsShownNextToTheName_WhereverThePageShowsARule()
        {
            // Правило на странице видно в трёх местах — у счёта и у переключателя
            // в «Неполном таджвиде» (I3, I4) и в «Правилах этого чтения»; перевод должен
            // стоять рядом с названием во всех.
            var picker = File.ReadAllText(Path.Combine(DocsTests.ReadmeTests.RepositoryRoot(),
                                                       "Transliterator.Web", "Components", "ReadingPicker.razor"));

            var names = Regex.Matches(picker, @"class=""reading-rule-name"">@rule\.Name\.Current</span>").Count;
            var translations = Regex.Matches(picker, @"@rule\.Name\.Current</span>\s*<span class=""reading-rule-translation"">\(@rule\.Translation\.Current\)</span>").Count;

            Assert.Equal(3, names);
            Assert.Equal(names, translations);
        }

        /// <summary>
        /// Правила, которые в обычном чтении убираются. Раскладка утверждена
        /// владельцем 2026-11-04 (#91); тест держит её, чтобы правка каталога
        /// не передвинула правило на другую ступень молча.
        /// </summary>
        private static readonly string[] TajweedOnly =
        {
            "idgham-mutajanisayn", "idgham-mutaqaribayn",
            "idgham-bi-ghunna", "idgham-bila-ghunna", "iqlab", "ikhfa", "ikhfa-shafawi",
            "madd-lazim", "madd-muttasil", "madd-munfasil", "madd-sila",
            "qalqalah-sughra", "qalqalah-kubra",
        };

        [Fact]
        public void Tiers_AreTheOwnersLayout()
        {
            var tajweed = Catalog.Rules.Where(r => r.Tier == TajweedTier.Tajweed).Select(r => r.Id);

            Assert.Equal(TajweedOnly.Order(), tajweed.Order());
        }

        [Theory]
        [InlineData("madd-arid", new[] { 2, 4, 6 }, 2)]
        [InlineData("madd-lin", new[] { 2, 4, 6 }, 2)]
        [InlineData("madd-muttasil", new[] { 4, 5 }, 4)]
        [InlineData("madd-munfasil", new[] { 4, 5 }, 4)]
        public void Variants_AreTheOwnersLayout(string id, int[] counts, int defaultCount)
        {
            var rule = Catalog.Rules.Single(r => r.Id == id);

            Assert.Equal(counts, rule.Counts);
            Assert.Equal(defaultCount, rule.DefaultCount);
        }

        [Fact]
        public void OnlyTheOwnersVariants_HaveCounts() =>
            Assert.Equal(new[] { "madd-arid", "madd-lin", "madd-muttasil", "madd-munfasil" }.Order(),
                         Catalog.Rules.Where(r => r.HasVariants).Select(r => r.Id).Order());

        [Theory]
        [InlineData("madd-arid", "ٱلرَّحِيمِ", "ح")]       // долгая ī перед конечной буквой на паузе
        [InlineData("madd-lin", "خَوْفٌ", "و")]            // сам глайд
        [InlineData("madd-muttasil", "جَآءَ", "ج")]        // долгая ā перед хамзой того же слова
        [InlineData("madd-munfasil", "لَآ أَعْبُدُ", "ل")]  // долгая ā перед хамзой следующего слова
        public void DefaultCount_IsWhatThePipelineCounts(string id, string arabic, string letter)
        {
            // Счёт по умолчанию — не подпись, а то, что конвейер и правда тянет:
            // каталог показывает его выбранным, пока человек не выбрал свой.
            var rule = Catalog.Rules.Single(r => r.Id == id);
            var segment = TransliterationPipeline.Consonants(arabic).First(s => s.Letter == letter);

            Assert.Equal(rule.DefaultCount, segment.VowelLength);
        }

        public static IEnumerable<object[]> VariantRules =>
            EmbeddedTajweedRuleCatalog.Catalog.Rules.Where(r => r.HasVariants).Select(r => new object[] { r.Id });

        [Theory]
        [MemberData(nameof(VariantRules))]
        public void VariantRule_IsExplained_WhereItsCountIsChosen(string id)
        {
            // «Неполный таджвид» (I3) показывает рядом со счётом описание и пример,
            // и пример пересчитывается под выбранный счёт: так видно, что счёт меняет.
            // Пример, в котором разные счёты пишутся одинаково, ничего не объяснит.
            var rule = Catalog.Rules.Single(r => r.Id == id);
            var example = rule.Examples.First();

            Assert.False(rule.Description.MissingLanguages().Count > 0);

            var variants = ReadingChoice.Default.WithMode(ReadingMode.Variants);
            var outputs = rule.Counts.Select(count => Transliterate(example.Arabic, variants.WithCount(id, count))).ToList();

            // Каждый счёт пишется по-своему: шкала сдвинута, и 5 больше не совпадает с 4 (G7).
            Assert.Equal(rule.Counts.Count, outputs.Distinct().Count());

            // Пример показывает своё правило, а не соседнее: счёты остальных правил
            // его не меняют. Слово с маддом перед хамзой на паузе так не годится —
            // там гласную тянет и мадд арид, и при ариде на шести выбор муттасиля
            // в примере не виден.
            var othersLongest = Catalog.Rules.Where(r => r.HasVariants && r.Id != id)
                                       .Aggregate(variants, (choice, other) => choice.WithCount(other.Id, other.Counts.Max()));

            Assert.Equal(outputs, rule.Counts.Select(count => Transliterate(example.Arabic, othersLongest.WithCount(id, count))));
        }

        private static string Transliterate(string arabic, ReadingChoice choice) =>
            TransliterationPipeline.Transliterate(arabic, reading: choice.ToOptions());

        [Fact]
        public void Catalog_IsReadFromTheAssembly_NotFromDisk()
        {
            // Страница в браузере каталога рядом со сборкой не найдёт: он должен
            // быть ресурсом ядра, а не файлом вывода.
            Assert.Contains(EmbeddedTajweedRuleCatalog.ResourceName,
                            typeof(EmbeddedTajweedRuleCatalog).Assembly.GetManifestResourceNames());
        }

        [Theory]
        [InlineData("UnknownStage", "unknown stage")]
        [InlineData("BadDefaultCount", "default count outside its counts")]
        [InlineData("NoTranslation", "no translation of its name in ru, en")]
        [InlineData("NoEnglishTranslation", "no translation of its name in en")]
        [InlineData("SwitchableBaseRule", "switchable but not in the tajweed tier")]
        public void BrokenCatalog_FailsToLoad(string name, string message)
        {
            var exception = Assert.Throws<TransliterationException>(() =>
                EmbeddedTajweedRuleCatalog.Load(typeof(TajweedRuleCatalogTests).Assembly,
                                                $"Transliterator.Tests.Resources.BrokenRuleCatalogs.{name}.json"));

            Assert.Contains(message, exception.Message);
        }
    }
}
