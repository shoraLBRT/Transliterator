using System.Text.RegularExpressions;
using Transliterator.Core.Repositories;
using Transliterator.Core.Services.Rules;
using Transliterator.Domain.Entities;
using Transliterator.Domain.Exceptions;
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
        [InlineData("NoReason", "does not say why")]
        public void BrokenCatalog_FailsToLoad(string name, string message)
        {
            var exception = Assert.Throws<TransliterationException>(() =>
                EmbeddedTajweedRuleCatalog.Load(typeof(TajweedRuleCatalogTests).Assembly,
                                                $"Transliterator.Tests.Resources.BrokenRuleCatalogs.{name}.json"));

            Assert.Contains(message, exception.Message);
        }
    }
}
