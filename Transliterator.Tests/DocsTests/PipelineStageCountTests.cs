using System.Text.RegularExpressions;
using Transliterator.Core.Services.Rules;
using Xunit;

namespace Transliterator.Tests.DocsTests
{
    /// <summary>
    /// Число стадий конвейера называет страница в «О приложении» (D8). Оно берётся из
    /// <see cref="TransliterationService.StageCount"/>, а этот тест сверяет константу
    /// с самим конвейером и с его таблицей в <c>docs/ROADMAP.md</c>: стадия, добавленная
    /// в одно место без другого, роняет тест.
    /// </summary>
    public class PipelineStageCountTests
    {
        [Fact]
        public void StageCount_IsTheRuleStagesPlusNormalizerParserAndRenderer()
        {
            // Стадии правил — то, что RulesService получает в конструкторе; нормализация,
            // разбор и рендеринг — у TransliterationService вокруг него.
            var rules = typeof(RulesService).GetConstructors().Single().GetParameters().Length;
            var service = typeof(TransliterationService).GetConstructors().Single().GetParameters()
                .Select(p => p.ParameterType.Name)
                .ToList();

            Assert.Contains("ArabicNormalizer", service);
            Assert.Contains("ArabicParser", service);
            Assert.Contains("CyrillicRenderer", service);
            Assert.Equal(TransliterationService.StageCount, rules + 3);
        }

        [Fact]
        public void StageCount_IsTheNumberOfRowsInTheRoadmapTable()
        {
            var roadmap = File.ReadAllText(Path.Combine(ReadmeTests.RepositoryRoot(), "docs", "ROADMAP.md"));
            var table = Regex.Match(roadmap, @"^## Стадии конвейера\s*\n(.*?)^---", RegexOptions.Multiline | RegexOptions.Singleline);

            Assert.True(table.Success, "the pipeline stage table is not found");

            var numbers = Regex.Matches(table.Groups[1].Value, @"^\| (\d+) \|", RegexOptions.Multiline)
                               .Select(m => int.Parse(m.Groups[1].Value))
                               .ToList();

            Assert.Equal(Enumerable.Range(1, TransliterationService.StageCount), numbers);
        }

        [Fact]
        public void TheAboutText_TakesTheCountFromThePipeline_NotFromItsOwnDigits()
        {
            var about = File.ReadAllText(Path.Combine(ReadmeTests.RepositoryRoot(), "Transliterator.Web", "Components", "AboutPicker.razor"));
            Assert.Contains("L[\"About.How\", L.Count(\"StageCount\", TransliterationService.StageCount)]", about);

            foreach (var language in new[] { "ru", "en" })
            {
                var how = PageStrings.Web(language)["About.How"];
                Assert.Contains("{0}", how);
                Assert.DoesNotMatch(@"\d", how.Replace("{0}", string.Empty));
            }
        }
    }
}
