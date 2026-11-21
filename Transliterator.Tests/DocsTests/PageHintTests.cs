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
            Assert.Contains("<h1 class=\"topbar-name\">@L[\"Page.Heading\"]</h1>", app);
            Assert.Contains("L[\"Page.Title\"].Value", app);
        }

        [Fact]
        public void TheHint_StopsTypingAndCollapses_WhileTheFieldHasText()
        {
            // D16: с текстом в поле подсказка схлопывается (.fold), а таймеры печати
            // гасятся совсем — не опрашивают поле. Состояние ведёт страница: Blazor
            // меняет значение поля сам, и событие input тогда не приходит.
            var root = ReadmeTests.RepositoryRoot();
            var app = Regex.Replace(File.ReadAllText(Path.Combine(root, "Transliterator.Web", "App.razor")), @"\s+", " ");
            var script = File.ReadAllText(Path.Combine(root, "Transliterator.Web", "wwwroot", "js", "page.js"));

            Assert.Contains("class=\"fold hint-fold @(HasText ? null : \"is-open\")\" inert=\"@HasText\"", app);
            Assert.Contains("\"page.hintPaused\", _hint, _hintPaused", app);
            Assert.Contains("function hintPaused(", script);
            Assert.Contains("clearTimeout(timer)", script);
            Assert.DoesNotContain("PausedDelay", script);
        }

        [Fact]
        public void SecondTier_IsCalledPartialTajweed_AndEachTierHasOneShortDistinctNote()
        {
            // Решение владельца к I5: вторая ступень — «Неполный таджвид» и под полем,
            // и в панели (оба места берут имя из ModeName). I14: пояснение каждой ступени —
            // одна короткая различимая фраза.
            var picker = Regex.Replace(File.ReadAllText(Path.Combine(ReadmeTests.RepositoryRoot(),
                                                                     "Transliterator.Web", "Components", "ReadingPicker.razor")), @"\s+", " ");

            Assert.Contains("ReadingMode.Variants => L[\"Mode.Variants\"]", picker);
            Assert.Contains("ReadingMode.Variants => L[\"ModeNote.Variants\"]", picker);

            var russian = PageStrings.Web("ru");
            Assert.Equal("Неполный таджвид", russian["Mode.Variants"]);
            Assert.DoesNotContain(russian.Values, value => value.Contains("Свои варианты"));
            // I14: пояснения ступеней — одна короткая различимая фраза; мадд и «можно не
            // читать» ушли в «Подробнее» над правилами (Reading.*Lead / *Note).
            Assert.Equal("Сами выбираете правила ниже", russian["ModeNote.Variants"]);
            Assert.Equal("Все правила таджвида", russian["ModeNote.Full"]);
            Assert.Equal("Обычное чтение, без таджвида", russian["ModeNote.Plain"]);

            var english = PageStrings.Web("en");
            Assert.Equal("Partial tajweed", english["Mode.Variants"]);
            Assert.Equal("You choose the rules below", english["ModeNote.Variants"]);
            Assert.Equal("All tajweed rules", english["ModeNote.Full"]);
            Assert.Equal("Plain reading, no tajweed", english["ModeNote.Plain"]);
        }

        [Fact]
        public void ProfileButton_SaysWhatItChooses_AndKeepsTheCaptionVisible()
        {
            // Решение владельца к E6: у выбора профиля подпись перед именем —
            // «Профиль: Cyrillic», свой профиль — своим именем. Кнопки профиля сбоку
            // больше нет (D44: шестерёнка), и подпись держит метка над выводом (D17):
            // подпись и значение — текст метки, длинное имя обрезается с конца,
            // не съедая подпись.
            var root = ReadmeTests.RepositoryRoot();
            var app = Regex.Replace(File.ReadAllText(Path.Combine(root, "Transliterator.Web", "App.razor")), @"\s+", " ");

            Assert.Contains("<span class=\"result-chip-label\">@L[\"Result.ProfileChipLabel\"]</span><span class=\"result-chip-text\">@_profile.Name</span>", app);

            var css = Regex.Replace(File.ReadAllText(Path.Combine(root, "Transliterator.Web", "wwwroot", "css", "app.css")), @"\s+", " ");
            var text = Regex.Match(css, @"\.result-chip-text \{([^}]*)\}");
            Assert.True(text.Success, "the chip text has no style");
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
        public void RulesNotes_AreOneShortPhraseWithDetailsBehindAMoreLink(string language)
        {
            // I14: над правилами стоят короткие фразы, длинные пояснения — под «Подробнее»
            // (aria-expanded, тело свёрнуто inert + aria-hidden, как описания правил).
            var picker = File.ReadAllText(Path.Combine(ReadmeTests.RepositoryRoot(),
                                                       "Transliterator.Web", "Components", "ReadingPicker.razor"));
            Assert.Contains("NoteWithDetails(\"counts\", \"Reading.CountsLead\", \"Reading.CountsNote\")", picker);
            Assert.Contains("NoteWithDetails(\"switchable\", \"Reading.SwitchableLead\", \"Reading.SwitchableNote\")", picker);
            Assert.Contains("class=\"reading-note-more\" aria-expanded=", picker);
            Assert.Contains("inert=\"@(!open)\" aria-hidden=\"@(open ? null : \"true\")\"", picker);

            var strings = PageStrings.Web(language);
            foreach (var lead in new[] { "Reading.CountsLead", "Reading.SwitchableLead" })
                Assert.True(strings[lead].Length <= 50, $"{lead} is not a short phrase");
            Assert.NotEqual(strings["Reading.More"], strings["Reading.Less"]);
        }

        [Theory]
        [MemberData(nameof(Languages))]
        public void CountHint_ExplainsTheLengthScale_AsThePipelineWritesIt(string language)
        {
            // Условие владельца к G7: там, где выбирают счёт, сказано, почему букв
            // не столько, сколько харакатов, — и шкала в пояснении та же, что у рендерера.
            var picker = File.ReadAllText(Path.Combine(ReadmeTests.RepositoryRoot(),
                                                       "Transliterator.Web", "Components", "ReadingPicker.razor"));
            Assert.Contains("\"Reading.CountsLead\", \"Reading.CountsNote\")", picker);

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
