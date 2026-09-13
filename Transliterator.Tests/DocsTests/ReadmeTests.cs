using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;
using System.Text.RegularExpressions;
using Transliterator.Core.Repositories;
using Transliterator.Core.Services;
using Transliterator.Domain.Entities;
using Xunit;

namespace Transliterator.Tests.DocsTests
{
    /// <summary>
    /// Примеры в README — не иллюстрация, а обещание (F3). Прежде README показывал
    /// профиль, которого в коде не было, и вывод, которого конвейер уже не давал.
    /// Здесь каждый пример сверяется с тем, откуда он взят: фрагмент Аль-Фатихи —
    /// с корпусом, прогоны — с конвейером, выдержка профиля — со <c>Standard.json</c>.
    /// </summary>
    /// <remarks>
    /// Примеры находятся по невидимым меткам <c>&lt;!-- readme-example:имя --&gt;</c>
    /// перед блоком: заголовки и порядок разделов README можно менять свободно,
    /// а пропавшая метка роняет тест, а не выключает проверку молча.
    /// </remarks>
    public class ReadmeTests
    {
        public const string WebVersionUrl = "https://shoralbrt.github.io/Transliterator/";

        public static TheoryData<string> Readmes() => new() { "README.md", "README.ru.md" };

        private static string RepositoryRoot()
        {
            for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
                if (File.Exists(Path.Combine(dir.FullName, "Transliterator.sln")))
                    return dir.FullName;

            throw new InvalidOperationException("Repository root (Transliterator.sln) not found above " + AppContext.BaseDirectory);
        }

        private static string Readme(string name) =>
            File.ReadAllText(Path.Combine(RepositoryRoot(), name)).Replace("\r\n", "\n");

        private static int MarkerEnd(string readme, string example)
        {
            var marker = $"<!-- readme-example:{example} -->";
            var index = readme.IndexOf(marker, StringComparison.Ordinal);

            Assert.True(index >= 0, $"README has no marker {marker}");
            return index + marker.Length;
        }

        /// <summary>Содержимое первого блока кода после метки, без ограждений.</summary>
        private static string Block(string readme, string example)
        {
            var match = new Regex(@"```[a-z]*\n(.*?)\n```", RegexOptions.Singleline)
                .Match(readme, MarkerEnd(readme, example));

            Assert.True(match.Success, $"README has no code block after readme-example:{example}");
            return match.Groups[1].Value;
        }

        /// <summary>Строки таблицы после метки: ячейки в обратных апострофах, без шапки.</summary>
        private static List<(string Arabic, string Expected)> Table(string readme, string example)
        {
            var rows = readme[MarkerEnd(readme, example)..]
                .TrimStart('\n')
                .Split('\n')
                .TakeWhile(line => line.StartsWith('|'))
                .Skip(2)
                .Select(line => Regex.Matches(line, "`([^`]*)`").Select(m => m.Groups[1].Value).ToList())
                .Select(cells => (cells[0], cells[1]))
                .ToList();

            Assert.NotEmpty(rows);
            return rows;
        }

        private static CorpusSurah Fatiha() =>
            new EmbeddedCorpusRepository(NullLogger<EmbeddedCorpusRepository>.Instance)
                .GetSurahAsync(1).GetAwaiter().GetResult()!;

        [Theory]
        [MemberData(nameof(Readmes))]
        public void FatihaExample_IsTheCorpus_AndWhatThePipelineGives(string name)
        {
            var readme = Readme(name);
            var input = Block(readme, "fatiha-input");
            var output = Block(readme, "fatiha-output");

            var ayahs = Fatiha().Ayahs.Take(4).ToList();

            Assert.Equal(string.Join(' ', ayahs.Select(a => $"{a.Arabic} {CorpusText.ArabicDigits(a.Number)}")), input);
            Assert.Equal(string.Join(' ', ayahs.Select(a => $"{a.Expected} {a.Number}")), output);
            Assert.Equal(output, TransliterationPipeline.Transliterate(input));
        }

        [Theory]
        [MemberData(nameof(Readmes))]
        public void Runs_AreWhatThePipelineGives(string name)
        {
            Assert.All(Table(Readme(name), "runs"),
                row => Assert.Equal(row.Expected, TransliterationPipeline.Transliterate(row.Arabic)));
        }

        [Theory]
        [MemberData(nameof(Readmes))]
        public void CliExample_PrintsWhatThePipelineGives(string name)
        {
            var readme = Readme(name);
            var command = Block(readme, "cli");
            var arabic = Regex.Match(command, "-- \"([^\"]+)\"").Groups[1].Value;

            Assert.NotEmpty(arabic);
            Assert.Equal(Block(readme, "cli-output"), TransliterationPipeline.Transliterate(arabic));
        }

        [Theory]
        [MemberData(nameof(Readmes))]
        public void ProfileExample_IsAnExcerptOfStandardJson(string name)
        {
            var excerpt = JsonSerializer.Deserialize<TransliterationProfile>(Block(Readme(name), "profile"))!;
            var standard = TestProfiles.Standard;

            Assert.Equal(standard.Name, excerpt.Name);
            Assert.EndsWith("...", excerpt.Description);
            Assert.StartsWith(excerpt.Description[..^3], standard.Description);
            Assert.NotEmpty(excerpt.Rules);
            Assert.All(excerpt.Rules, rule =>
            {
                Assert.True(standard.Rules.ContainsKey(rule.Key), $"Standard.json has no key '{rule.Key}'");
                Assert.Equal(standard.Rules[rule.Key], rule.Value);
            });
        }

        [Fact]
        public void EnglishAndRussian_ShowTheSameExamples()
        {
            var english = Readme("README.md");
            var russian = Readme("README.ru.md");

            foreach (var example in new[] { "fatiha-input", "fatiha-output", "cli", "cli-output", "profile" })
                Assert.Equal(Block(english, example), Block(russian, example));

            Assert.Equal(Table(english, "runs"), Table(russian, "runs"));
        }

        [Theory]
        [MemberData(nameof(Readmes))]
        public void Readme_LinksToTheWebVersion(string name)
        {
            Assert.Contains(WebVersionUrl, Readme(name));
        }
    }
}
