using System.Text.RegularExpressions;
using Xunit;

namespace Transliterator.Tests.DocsTests
{
    /// <summary>
    /// Пример в подсказке над полем (D5) — такое же обещание, как примеры README:
    /// человек вставит тот же текст и должен получить ровно показанное. Подсказка
    /// живёт в разметке страницы, и тест читает её оттуда, а не из копии.
    /// </summary>
    public class PageHintTests
    {
        /// <summary>Фраза вида <c>"арабский текст — транслитерация"</c>.</summary>
        private static readonly Regex Example = new("\"([\\u0600-\\u06FF][^\"]*?) — ([^\"]+)\"");

        [Fact]
        public void ArabicExampleInTheHint_IsWhatThePageGives()
        {
            var app = File.ReadAllText(Path.Combine(ReadmeTests.RepositoryRoot(), "Transliterator.Web", "App.razor"));
            var examples = Example.Matches(app);

            Assert.NotEmpty(examples);
            Assert.All(examples, example =>
                Assert.Equal(example.Groups[2].Value, TransliterationPipeline.Transliterate(example.Groups[1].Value)));
        }
    }
}
