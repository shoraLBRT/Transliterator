using System.Globalization;
using System.Text.RegularExpressions;
using Transliterator.Core.Localization;
using Transliterator.Core.Services;
using Transliterator.Domain.Localization;
using Xunit;

namespace Transliterator.Tests.DocsTests
{
    /// <summary>
    /// Интерфейс на языках страницы (D7). Решение владельца 2026-11-12: каждый текст
    /// страницы есть на каждом языке, и тест падает, если у строки интерфейса нет
    /// перевода. Здесь — ресурсы интерфейса, разметка, которая их берёт, и оболочка
    /// страницы до запуска. Каталог правил и имена сур проверяют свои тесты
    /// (<c>TajweedRuleCatalogTests</c>, <c>CorpusLoaderTests</c>).
    /// </summary>
    public class PageLocalizationTests
    {
        public static IEnumerable<object[]> Translations =>
            from resource in PageStrings.Resources
            from language in PageLanguages.All
            where language != PageLanguages.Default
            select new object[] { resource, language.Code };

        [Theory]
        [MemberData(nameof(Translations))]
        public void EveryStringOfTheInterface_IsTranslated(string resource, string language)
        {
            var original = PageStrings.Read(resource, PageLanguages.Default.Code);

            Assert.True(File.Exists(PageStrings.FileOf(resource, language)), $"{resource} has no {language} resource");
            var translation = PageStrings.Read(resource, language);

            Assert.Equal(original.Keys.Order(), translation.Keys.Order());
            Assert.All(translation, pair => Assert.False(string.IsNullOrWhiteSpace(pair.Value), $"{pair.Key} is empty in {language}"));

            // Подстановки те же: «{0}» без пары — исключение форматирования на странице.
            var placeholders = new Regex(@"\{\d+\}");
            Assert.All(original, pair => Assert.Equal(
                placeholders.Matches(pair.Value).Select(m => m.Value).Distinct().Order(),
                placeholders.Matches(translation[pair.Key]).Select(m => m.Value).Distinct().Order()));
        }

        [Fact]
        public void EnglishStrings_AreNotRussian()
        {
            // Строка, скопированная из русского ресурса, тоже «переведена» — но не для того,
            // кто читает по-английски.
            var cyrillic = new Regex("[а-яА-ЯёЁ]");

            foreach (var resource in PageStrings.Resources)
                Assert.All(PageStrings.Read(resource, "en"), pair => Assert.DoesNotMatch(cyrillic, pair.Value));
        }

        [Fact]
        public void EveryStringTheMarkupTakes_IsInTheResources()
        {
            // Ключа нет — IStringLocalizer молча показывает сам ключ: «Mode.Full» вместо названия.
            var keys = PageStrings.Web(PageLanguages.Default.Code).Keys.ToHashSet();
            var used = WebSources().SelectMany(source =>
                    Regex.Matches(source.Text, @"\bL\[""([^""]+)""").Select(m => m.Groups[1].Value)
                         .Concat(Regex.Matches(source.Text, @"\bL\.Count\(""([^""]+)""")
                                      .SelectMany(m => new[] { "One", "Few", "Many" }.Select(form => $"{m.Groups[1].Value}.{form}"))))
                .Distinct()
                .ToList();

            Assert.NotEmpty(used);
            Assert.All(used, key => Assert.Contains(key, keys));
        }

        [Fact]
        public void TheMarkup_HasNoRussianTextOfItsOwn()
        {
            // Русский текст прямо в разметке или коде страницы остался бы русским в любом
            // интерфейсе. Комментарии — для разработчика, и они по-русски, как везде в проекте.
            var cyrillic = new Regex("[а-яА-ЯёЁ]+");

            Assert.All(WebSources(), source =>
            {
                var code = WithoutComments(source.Text);
                var found = cyrillic.Match(code);

                Assert.False(found.Success, $"{source.Name} has text outside the resources: «{Around(code, found.Index)}»");
            });
        }

        [Fact]
        public void TheShellBeforeStart_KnowsEveryLanguage()
        {
            // «Загрузка…» и полоса ошибки видны раньше, чем загрузятся ресурсы, и их
            // тексты живут в скрипте страницы — тоже на каждом языке.
            var script = File.ReadAllText(Path.Combine(ReadmeTests.RepositoryRoot(), "Transliterator.Web", "wwwroot", "js", "page.js"));
            var shell = Regex.Match(script, @"const Shell = \{(.*?)\n    \};", RegexOptions.Singleline);

            Assert.True(shell.Success, "the shell texts are not found");

            var languages = Regex.Matches(shell.Groups[1].Value, @"^\s*(\w+): \{", RegexOptions.Multiline).Select(m => m.Groups[1].Value);
            Assert.Equal(PageLanguages.All.Select(l => l.Code).Order(), languages.Order());

            var index = File.ReadAllText(Path.Combine(ReadmeTests.RepositoryRoot(), "Transliterator.Web", "wwwroot", "index.html"));
            foreach (var key in Regex.Matches(index, @"data-shell=""(\w+)""").Select(m => m.Groups[1].Value))
                Assert.Equal(PageLanguages.All.Count, Regex.Matches(shell.Groups[1].Value, $@"\b{key}: ""[^""]+""").Count);
        }

        [Fact]
        public void TheLanguageSwitch_IsOnThePage_AndListsEveryLanguage()
        {
            var app = File.ReadAllText(Path.Combine(ReadmeTests.RepositoryRoot(), "Transliterator.Web", "App.razor"));
            var picker = File.ReadAllText(Path.Combine(ReadmeTests.RepositoryRoot(), "Transliterator.Web", "Components", "LanguagePicker.razor"));

            Assert.Contains("<LanguagePicker Chosen=\"ChangeLanguageAsync\" />", app);
            Assert.Contains("@foreach (var language in PageLanguages.All)", picker);
            Assert.Contains("aria-pressed", picker);
        }

        [Theory]
        [InlineData(null, new[] { "en-US", "ru" }, "en")]
        [InlineData(null, new[] { "ru-RU", "en" }, "ru")]
        [InlineData(null, new[] { "de-DE", "en-GB" }, "en")]   // первый язык браузера, который есть у страницы
        [InlineData(null, new[] { "kk-KZ" }, "ru")]             // ни одного — по умолчанию русский
        [InlineData(null, new string[0], "ru")]
        [InlineData("en", new[] { "ru-RU" }, "en")]             // сохранённый выбор важнее браузера
        [InlineData("ru", new[] { "en-US" }, "ru")]
        [InlineData("xx", new[] { "en-US" }, "en")]             // язык, которого больше нет, не мешает
        public void Language_IsTheSavedChoice_ElseTheBrowsers(string? stored, string[] browser, string expected) =>
            Assert.Equal(expected, PageLanguages.Resolve(stored, browser).Code);

        [Fact]
        public void EnglishInterface_WritesInLatin_ByDefault()
        {
            // Решение владельца (D7): с английским интерфейсом по умолчанию латиница.
            Assert.Equal("Standard", PageLanguages.Russian.DefaultProfile);
            Assert.Equal("Latin", PageLanguages.English.DefaultProfile);

            // Профиль по умолчанию — встроенный: страница загружает его до всякого выбора.
            Assert.All(PageLanguages.All, language =>
                Assert.Contains(TestProfiles.All, profile => profile.Name == language.DefaultProfile));
        }

        [Theory]
        [InlineData("ru", 1, "One")]
        [InlineData("ru", 2, "Few")]
        [InlineData("ru", 5, "Many")]
        [InlineData("ru", 11, "Many")]
        [InlineData("ru", 13, "Many")]
        [InlineData("ru", 21, "One")]
        [InlineData("ru", 24, "Few")]
        [InlineData("en", 1, "One")]
        [InlineData("en", 2, "Many")]
        [InlineData("en", 13, "Many")]
        public void RuleCount_TakesTheFormOfItsLanguage(string language, int count, string form) =>
            Assert.Equal($"RuleCount.{form}", Plural.Key("RuleCount", count, CultureInfo.GetCultureInfo(language)));

        [Fact]
        public void CoreTexts_FollowTheInterfaceLanguage()
        {
            Assert.Equal("на паузе", CoreText.Get("Variant.waqf"));

            using (TestCulture.Use("en-GB"))
                Assert.Equal("at a pause", CoreText.Get("Variant.waqf"));
        }

        [Fact]
        public void LocalizedText_FallsBackToTheDefaultLanguage()
        {
            var text = new LocalizedText(new Dictionary<string, string> { ["ru"] = "Аль-Фатиха", ["en"] = "Al-Fatiha" });

            Assert.Equal("Al-Fatiha", text.For(CultureInfo.GetCultureInfo("en-US")));
            Assert.Equal("Аль-Фатиха", text.For(CultureInfo.GetCultureInfo("de")));
            Assert.Equal("Аль-Фатиха", text.Current);
            Assert.Empty(text.MissingLanguages());
            Assert.Equal(new[] { "en" }, new LocalizedText(new Dictionary<string, string> { ["ru"] = "x" }).MissingLanguages());
        }

        private sealed record Source(string Name, string Text);

        private static IEnumerable<Source> WebSources()
        {
            var web = Path.Combine(ReadmeTests.RepositoryRoot(), "Transliterator.Web");

            return Directory.EnumerateFiles(web, "*.*", SearchOption.AllDirectories)
                            .Where(path => path.EndsWith(".razor", StringComparison.Ordinal) || path.EndsWith(".cs", StringComparison.Ordinal))
                            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                                        && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
                            .Select(path => new Source(Path.GetRelativePath(web, path), File.ReadAllText(path)));
        }

        /// <summary>Без комментариев Razor (<c>@* *@</c>), HTML и C# (<c>//</c>, <c>/* */</c>).</summary>
        private static string WithoutComments(string text)
        {
            text = Regex.Replace(text, @"@\*.*?\*@", " ", RegexOptions.Singleline);
            text = Regex.Replace(text, @"<!--.*?-->", " ", RegexOptions.Singleline);
            text = Regex.Replace(text, @"/\*.*?\*/", " ", RegexOptions.Singleline);
            return Regex.Replace(text, @"(?<!:)//.*$", " ", RegexOptions.Multiline);
        }

        private static string Around(string text, int index) =>
            text.Substring(Math.Max(0, index - 30), Math.Min(80, text.Length - Math.Max(0, index - 30))).Trim();
    }
}
