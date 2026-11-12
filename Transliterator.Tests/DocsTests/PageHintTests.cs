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

        [Fact]
        public void SecondTier_IsCalledPartialTajweed_AndItsNoteNamesBothChoices()
        {
            // Решение владельца к I5: вторая ступень — «Неполный таджвид» и под полем,
            // и в панели (оба места берут имя из ModeName), а пояснение говорит и про
            // счёт мадда, и про правила, которые можно не читать (I4).
            var picker = Regex.Replace(File.ReadAllText(Path.Combine(ReadmeTests.RepositoryRoot(),
                                                                     "Transliterator.Web", "Components", "ReadingPicker.razor")), @"\s+", " ");

            Assert.Contains("ReadingMode.Variants => \"Неполный таджвид\"", picker);
            Assert.DoesNotContain("Свои варианты", picker);

            var note = Regex.Match(picker, "string ModeNote.*?ReadingMode.Variants => \"([^\"]+)\"");
            Assert.True(note.Success, "the second tier has no note in the panel");
            Assert.Contains("мадд", note.Groups[1].Value);
            Assert.Contains("можно не читать", note.Groups[1].Value);
        }

        [Fact]
        public void ProfileButton_SaysWhatItChooses_AndKeepsTheCaptionVisible()
        {
            // Решение владельца к E6: на кнопке профиля подпись перед именем —
            // «Профиль: Standard», свой профиль — своим именем («Профиль: Мой»).
            // Подпись — часть текста кнопки (а значит, и её имени для диктора),
            // и длинное имя обрезается с конца, не съедая подпись. aria-label нет
            // нарочно: отдельная строка для диктора разошлась бы с видимой.
            var root = ReadmeTests.RepositoryRoot();
            var picker = Regex.Replace(File.ReadAllText(Path.Combine(root, "Transliterator.Web", "Components", "ProfilePicker.razor")), @"\s+", " ");

            Assert.Contains("ToggleText => $\"Профиль: {Profile.Name}\"", picker);

            var toggle = Regex.Match(picker, "<button [^>]*aria-controls=\"profile-menu\"[^>]*>(.*?)</button>");
            Assert.True(toggle.Success, "the profile button is not found");
            Assert.Contains("<span class=\"picker-toggle-text\">@ToggleText</span>", toggle.Groups[1].Value);
            Assert.DoesNotContain("@Profile.Name", toggle.Groups[1].Value);
            Assert.DoesNotContain("aria-label", toggle.Value);

            var css = Regex.Replace(File.ReadAllText(Path.Combine(root, "Transliterator.Web", "wwwroot", "css", "app.css")), @"\s+", " ");
            var text = Regex.Match(css, @"\.picker-toggle-text \{([^}]*)\}");
            Assert.True(text.Success, "the button text has no style");
            Assert.Contains("text-overflow: ellipsis", text.Groups[1].Value);
            Assert.Contains("white-space: nowrap", text.Groups[1].Value);
        }

        private static readonly Dictionary<string, int> Numbers = new()
        {
            ["два"] = 2, ["две"] = 2, ["три"] = 3, ["четыре"] = 4, ["пять"] = 5, ["шесть"] = 6,
        };

        [Fact]
        public void CountHint_ExplainsTheLengthScale_AsThePipelineWritesIt()
        {
            // Условие владельца к G7: там, где выбирают счёт, сказано, почему букв
            // не столько, сколько харакатов, — и шкала в пояснении та же, что у рендерера.
            var picker = File.ReadAllText(Path.Combine(ReadmeTests.RepositoryRoot(),
                                                       "Transliterator.Web", "Components", "ReadingPicker.razor"));
            var note = Regex.Replace(picker, @"\s+", " ");

            Assert.Contains("не считает харакаты буквами", note);

            var scale = Regex.Match(note, @"сколько харакатов: ([^.]+)\.");
            Assert.True(scale.Success, "the count hint does not state the length scale");

            var pairs = scale.Groups[1].Value.Split(", ")
                             .Select(pair => pair.Split(" — "))
                             .Select(parts => (Count: Numbers[parts[0].Split(' ')[0]], Letters: Numbers[parts[1].Split(' ')[0]]))
                             .ToList();

            // Каждый счёт, который можно выбрать, в пояснении есть.
            var chosen = Transliterator.Core.Repositories.EmbeddedTajweedRuleCatalog.Catalog.Rules
                .SelectMany(r => r.Counts).Distinct();
            Assert.Subset(pairs.Select(p => p.Count).ToHashSet(), chosen.ToHashSet());

            foreach (var (count, letters) in pairs)
                Assert.Equal("ар-рохI" + new string('и', letters) + "м", TransliterationPipeline.Transliterate("ٱلرَّحِيمِ",
                    reading: new Transliterator.Domain.Phonology.ReadingOptions
                    {
                        Counts = new Dictionary<string, int> { [Transliterator.Core.Services.Rules.TajweedRuleIds.MaddArid] = count }
                    }));
        }
    }
}
