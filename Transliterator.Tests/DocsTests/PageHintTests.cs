using System.Text.RegularExpressions;
using Transliterator.Domain.Localization;
using Xunit;

namespace Transliterator.Tests.DocsTests
{
    /// <summary>
    /// Пример в подсказке над полем (D5) — такое же обещание, как примеры README:
    /// человек вставит тот же текст и должен получить ровно показанное. Подсказка
    /// живёт в ресурсах страницы (D7), и тест читает её оттуда, а не из копии, —
    /// на каждом языке, профилем этого языка по умолчанию.
    /// </summary>
    public class PageHintTests
    {
        /// <summary>Фраза вида <c>арабский текст — транслитерация</c>.</summary>
        private static readonly Regex Example = new("^([\\u0600-\\u06FF].*?) — (.+)$");

        public static IEnumerable<object[]> Languages => PageLanguages.All.Select(l => new object[] { l.Code });

        [Theory]
        [MemberData(nameof(Languages))]
        public void ArabicExampleInTheHint_IsWhatThePageGives(string language)
        {
            var profile = TestProfiles.All.Single(p => p.Name == PageLanguages.Find(language)!.DefaultProfile);
            var hints = PageStrings.Web(language).Where(pair => pair.Key.StartsWith("Hint.", StringComparison.Ordinal)).ToList();
            var examples = hints.Select(pair => Example.Match(pair.Value)).Where(m => m.Success).ToList();

            Assert.True(hints.Count >= 2, "the hint has fewer than two phrases");
            Assert.NotEmpty(examples);
            Assert.All(examples, example =>
                Assert.Equal(example.Groups[2].Value, TransliterationPipeline.Transliterate(example.Groups[1].Value, profile)));
        }

        [Fact]
        public void TheHint_IsTakenFromTheResources()
        {
            var app = Regex.Replace(File.ReadAllText(Path.Combine(ReadmeTests.RepositoryRoot(), "Transliterator.Web", "App.razor")), @"\s+", " ");

            Assert.Contains("private const string HintPrefix = \"Hint.\";", app);
            Assert.Contains("await JS.InvokeVoidAsync(\"page.typewriter\", _hint, Hints(), _input);", app);
            Assert.Contains("<h1 class=\"visually-hidden\">@L[\"Page.Heading\"]</h1>", app);
            Assert.Contains("L[\"Page.Title\"].Value", app);
        }

        [Fact]
        public void SecondTier_IsCalledPartialTajweed_AndItsNoteNamesBothChoices()
        {
            // Решение владельца к I5: вторая ступень — «Неполный таджвид» и под полем,
            // и в панели (оба места берут имя из ModeName), а пояснение говорит и про
            // счёт мадда, и про правила, которые можно не читать (I4).
            var picker = Regex.Replace(File.ReadAllText(Path.Combine(ReadmeTests.RepositoryRoot(),
                                                                     "Transliterator.Web", "Components", "ReadingPicker.razor")), @"\s+", " ");

            Assert.Contains("ReadingMode.Variants => L[\"Mode.Variants\"]", picker);
            Assert.Contains("ReadingMode.Variants => L[\"ModeNote.Variants\"]", picker);

            var russian = PageStrings.Web("ru");
            Assert.Equal("Неполный таджвид", russian["Mode.Variants"]);
            Assert.DoesNotContain(russian.Values, value => value.Contains("Свои варианты"));
            Assert.Contains("мадд", russian["ModeNote.Variants"]);
            Assert.Contains("можно не читать", russian["ModeNote.Variants"]);

            var english = PageStrings.Web("en");
            Assert.Equal("Partial tajweed", english["Mode.Variants"]);
            Assert.Contains("madd", english["ModeNote.Variants"]);
            Assert.Contains("left out", english["ModeNote.Variants"]);
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

            Assert.Contains("ToggleText => L[\"Profile.Toggle\", Profile.Name]", picker);
            Assert.Equal("Профиль: {0}", PageStrings.Web("ru")["Profile.Toggle"]);
            Assert.Equal("Profile: {0}", PageStrings.Web("en")["Profile.Toggle"]);

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

        /// <summary>Числа в пояснении к счёту, по языкам. Третий язык — свой словарь здесь.</summary>
        private static readonly Dictionary<string, Dictionary<string, int>> NumberWords = new()
        {
            ["ru"] = new() { ["два"] = 2, ["две"] = 2, ["три"] = 3, ["четыре"] = 4, ["пять"] = 5, ["шесть"] = 6 },
            ["en"] = new() { ["two"] = 2, ["three"] = 3, ["four"] = 4, ["five"] = 5, ["six"] = 6 },
        };

        /// <summary>Где в пояснении начинается шкала и как звучит оговорка про харакаты.</summary>
        private static readonly Dictionary<string, (string Scale, string Caveat)> ScaleWording = new()
        {
            ["ru"] = (@"сколько харакатов: ([^.]+)\.", "не считает харакаты буквами"),
            ["en"] = (@"as many letters as counts: ([^.]+)\.", "does not count harakat as letters"),
        };

        /// <summary>«ар-рохI» + долгота + «м» — так пишет каждый профиль по умолчанию.</summary>
        private static readonly Dictionary<string, (string Before, char Vowel, string After)> Rahim = new()
        {
            ["ru"] = ("ар-рохI", 'и', "м"),
            ["en"] = ("ar-roḥ", 'i', "m"),
        };

        [Theory]
        [MemberData(nameof(Languages))]
        public void CountHint_ExplainsTheLengthScale_AsThePipelineWritesIt(string language)
        {
            // Условие владельца к G7: там, где выбирают счёт, сказано, почему букв
            // не столько, сколько харакатов, — и шкала в пояснении та же, что у рендерера.
            var picker = File.ReadAllText(Path.Combine(ReadmeTests.RepositoryRoot(),
                                                       "Transliterator.Web", "Components", "ReadingPicker.razor"));
            Assert.Contains("@L[\"Reading.CountsNote\"]", picker);

            var note = PageStrings.Web(language)["Reading.CountsNote"];
            var numbers = NumberWords[language];
            var (scaleAt, caveat) = ScaleWording[language];
            var (before, vowel, after) = Rahim[language];
            var profile = TestProfiles.All.Single(p => p.Name == PageLanguages.Find(language)!.DefaultProfile);

            Assert.Contains(caveat, note);

            var scale = Regex.Match(note, scaleAt);
            Assert.True(scale.Success, "the count hint does not state the length scale");

            var pairs = scale.Groups[1].Value.Split(", ")
                             .Select(pair => pair.Split(" — "))
                             .Select(parts => (Count: numbers[parts[0].Split(' ')[0]], Letters: numbers[parts[1].Split(' ')[0]]))
                             .ToList();

            // Каждый счёт, который можно выбрать, в пояснении есть.
            var chosen = Transliterator.Core.Repositories.EmbeddedTajweedRuleCatalog.Catalog.Rules
                .SelectMany(r => r.Counts).Distinct();
            Assert.Subset(pairs.Select(p => p.Count).ToHashSet(), chosen.ToHashSet());

            foreach (var (count, letters) in pairs)
                Assert.Equal(before + new string(vowel, letters) + after, TransliterationPipeline.Transliterate("ٱلرَّحِيمِ", profile,
                    reading: new Transliterator.Domain.Phonology.ReadingOptions
                    {
                        Counts = new Dictionary<string, int> { [Transliterator.Core.Services.Rules.TajweedRuleIds.MaddArid] = count }
                    }));
        }
    }
}
