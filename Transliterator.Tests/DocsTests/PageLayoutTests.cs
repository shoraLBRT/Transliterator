using System.Text.RegularExpressions;
using Xunit;

namespace Transliterator.Tests.DocsTests
{
    /// <summary>
    /// Раскладка страницы (D6), решение владельца 2026-11-12: под полем остаётся
    /// только выбор суры, ступень чтения и профиль — круглые кнопки сбоку, и на
    /// широком экране их панель открывается колонкой, не закрывая поле и вывод.
    /// Тест читает разметку и стили страницы, а не их пересказ.
    /// </summary>
    public class PageLayoutTests
    {
        private static string Web(params string[] path) =>
            File.ReadAllText(Path.Combine(new[] { ReadmeTests.RepositoryRoot(), "Transliterator.Web" }.Concat(path).ToArray()));

        [Fact]
        public void UnderTheField_OnlyTheSurahPicker_TheOtherSettingsStandAtTheSide()
        {
            var app = Web("App.razor");

            var composer = app.IndexOf("<div class=\"composer\">", StringComparison.Ordinal);
            var rail = app.IndexOf("<aside class=\"side-rail\"", StringComparison.Ordinal);
            var railEnd = app.IndexOf("</aside>", StringComparison.Ordinal);

            Assert.True(composer >= 0, "the field is not found");
            Assert.True(rail > composer, "the side rail is not found after the field");
            Assert.True(railEnd > rail, "the side rail is not closed");

            var field = app[composer..rail];
            var side = app[rail..railEnd];

            Assert.Contains("<SurahPicker", field);
            Assert.DoesNotContain("<ReadingPicker", field);
            Assert.DoesNotContain("<ProfilePicker", field);

            Assert.Contains("<ReadingPicker", side);
            Assert.Contains("<ProfilePicker", side);
            Assert.Contains("<AboutPicker", side);
            Assert.DoesNotContain("<SurahPicker", side);

            Assert.Single(Regex.Matches(app, "<SurahPicker"));
            Assert.Single(Regex.Matches(app, "<ReadingPicker"));
            Assert.Single(Regex.Matches(app, "<ProfilePicker"));
            Assert.Single(Regex.Matches(app, "<AboutPicker"));
        }

        [Fact]
        public void TheHeader_NamesThePage_AndHoldsTheLanguage_InTheFlow()
        {
            // D13: шапка над полем — знак и видимое имя (единственный h1) слева,
            // язык справа. Она в потоке: переключатель языка больше не стоит
            // поверх страницы и не наезжает на подсказку; с 64rem шапка сдвигается
            // вместе с полем, и колонка панели её не накрывает.
            var app = Web("App.razor");
            var css = Web("wwwroot", "css", "app.css");
            var index = Web("wwwroot", "index.html");

            var header = Regex.Match(app, "<header class=\"topbar\">(.*?)</header>", RegexOptions.Singleline);
            Assert.True(header.Success, "the header is not found");
            Assert.True(header.Index < app.IndexOf("<main class=\"stage\">", StringComparison.Ordinal), "the header is not above the field");

            Assert.Contains("<svg class=\"topbar-mark\"", header.Groups[1].Value);
            Assert.Contains("<h1 class=\"topbar-name\">@L[\"Page.Heading\"]</h1>", header.Groups[1].Value);
            Assert.True(header.Groups[1].Value.IndexOf("<h1", StringComparison.Ordinal)
                        < header.Groups[1].Value.IndexOf("<LanguagePicker", StringComparison.Ordinal), "the language is not on the right");
            Assert.Single(Regex.Matches(app, "<h1"));
            Assert.Single(Regex.Matches(app, "<LanguagePicker"));

            var languageSwitch = Regex.Match(css, @"\.language-switch \{([^}]*)\}");
            Assert.True(languageSwitch.Success, "the language switch style is not found");
            Assert.DoesNotContain("position", languageSwitch.Groups[1].Value);
            Assert.Matches(@"\.topbar,\s*\.stage,\s*\.footer \{", css);

            Assert.Contains("<link rel=\"icon\" href=\"favicon.svg\" type=\"image/svg+xml\" />", index);
            Assert.Contains("<link rel=\"apple-touch-icon\" href=\"apple-touch-icon.png\" />", index);
            Assert.True(File.Exists(Path.Combine(ReadmeTests.RepositoryRoot(), "Transliterator.Web", "wwwroot", "favicon.svg")));
            Assert.True(File.Exists(Path.Combine(ReadmeTests.RepositoryRoot(), "Transliterator.Web", "wwwroot", "apple-touch-icon.png")));
        }

        [Theory]
        [InlineData("SurahPicker.razor", "surah-menu")]
        [InlineData("ReadingPicker.razor", "reading-menu")]
        [InlineData("ProfilePicker.razor", "profile-menu")]
        [InlineData("AboutPicker.razor", "about-menu")]
        public void AllPanels_BehaveAlike(string file, string panel)
        {
            // Одно поведение на все панели: общий учёт открытой (одна на страницу),
            // шапка с крестиком и имя диалога из неё.
            var picker = Regex.Replace(Web("Components", file), @"\s+", " ");

            Assert.Contains("@inherits PanelPicker", picker);
            Assert.Contains($"aria-labelledby=\"{panel}-title\"", picker);
            Assert.Contains($"<PanelHead Id=\"{panel}-title\"", picker);
            Assert.Contains("@onkeydown=\"OnPanelKeyDownAsync\"", picker);
        }

        [Fact]
        public void RailButtons_ShowTheCurrentChoice()
        {
            // Условие E6 в новой раскладке: у круглой кнопки видно, что выбрано.
            var reading = Regex.Replace(Web("Components", "ReadingPicker.razor"), @"\s+", " ");
            var toggle = Regex.Match(reading, "<button [^>]*aria-controls=\"reading-menu\"[^>]*>(.*?)</button>");

            Assert.True(toggle.Success, "the reading button is not found");
            Assert.Contains("<span class=\"picker-toggle-text\">@ModeName(Choice.Mode)</span>", toggle.Groups[1].Value);
        }

        [Fact]
        public void ReadingRules_AreTwoCollapsedBlocks_WithTheControlsInTheirRows()
        {
            // I8: под ступенями — два блока правил, без общей кнопки «Правила этого
            // чтения» и без отдельного списка «Можно не читать». Заголовок блока — кнопка
            // с aria-expanded; при открытии панели оба свёрнуты, «Неполный таджвид»
            // раскрывает «Правила таджвида».
            var reading = Regex.Replace(Web("Components", "ReadingPicker.razor"), @"\s+", " ");

            Assert.DoesNotContain("<details", reading);
            Assert.DoesNotContain("Reading.RulesSummary", reading);
            Assert.DoesNotContain("Reading.SwitchableTitle", reading);
            Assert.Single(Regex.Matches(reading, "class=\"reading-list\""));
            Assert.Single(Regex.Matches(reading, @"role=""radiogroup"" aria-label=""@\(L\[""Reading\.CountLabel"""));
            Assert.Single(Regex.Matches(reading, "role=\"switch\""));

            var toggle = Regex.Match(reading, @"<button type=""button"" class=""reading-group-toggle""(.*?)@onclick=""\(\) => ToggleBlock\(tier\)"">(.*?)</button>");
            Assert.True(toggle.Success, "the block header is not a button");
            Assert.Contains("aria-expanded=\"@(open ? \"true\" : \"false\")\"", toggle.Groups[1].Value);
            Assert.Contains("aria-controls=\"@blockId\"", toggle.Groups[1].Value);
            Assert.Contains("reading-group-count", toggle.Groups[2].Value);
            // I12: счёт — дробью «читаемых/всего», диктору — словами; приписки
            // «сейчас не читаются» больше нет, «0/13» говорит то же.
            Assert.Contains("<span aria-hidden=\"true\">@read/@total</span>", toggle.Groups[2].Value);
            Assert.Contains("<span class=\"visually-hidden\">@L[\"Reading.GroupCountLabel\", read, total]</span>", toggle.Groups[2].Value);
            Assert.DoesNotContain("Reading.GroupOff", reading);
            Assert.DoesNotContain("RuleCount", reading);
            Assert.Contains("var total = group.Count();", reading);
            Assert.Contains("var read = group.Count(rule => !IsOff(rule));", reading);
            Assert.Contains("<div id=\"@blockId\" class=\"reading-block-body\" hidden=\"@(!open)\">", reading);

            Assert.Matches(@"protected override Task BeforeOpenAsync\(\) \{ _openBlocks\.Clear\(\);", reading);
            Assert.Matches(@"if \(mode == ReadingMode\.Variants\) _openBlocks\.Add\(TajweedTier\.Tajweed\);", reading);

            var russian = PageStrings.Web("ru");
            Assert.Equal("Правила чтения на арабском", russian["Group.Base"]);
            Assert.Equal("Правила таджвида", russian["Group.Tajweed"]);

            var english = PageStrings.Web("en");
            Assert.Equal("Rules of reading Arabic", english["Group.Base"]);
            Assert.Equal("Tajweed rules", english["Group.Tajweed"]);

            Assert.Equal("читается 11 из 13", string.Format(russian["Reading.GroupCountLabel"], 11, 13));
            Assert.Equal("11 of 13 read", string.Format(english["Reading.GroupCountLabel"], 11, 13));
        }

        [Fact]
        public void ChoosingAReadingTier_KeepsThePanelOpen_AndShowsTheOutputInIt()
        {
            // I10: какую ступень ни выбери, панель «Как читать» остаётся открытой —
            // закрывают её крестиком, Escape или кнопкой, как другие панели. На телефоне
            // она закрывает вывод, поэтому вывод повторён в ней, как в панели профиля (E4).
            var reading = Regex.Replace(Web("Components", "ReadingPicker.razor"), @"\s+", " ");

            var choose = Regex.Match(reading, @"private Task ChooseModeAsync\(ReadingMode mode\) \{(.*?)\}(?= private)");
            Assert.True(choose.Success, "ChooseModeAsync is not found");
            Assert.DoesNotContain("Close", choose.Groups[1].Value);

            foreach (var picker in new[] { reading, Regex.Replace(Web("Components", "ProfilePicker.razor"), @"\s+", " ") })
            {
                Assert.Matches(@"<div class=""panel-preview[^""]*"" aria-live=""polite"">", picker);
                Assert.Contains("@L[\"Preview.Label\"]", picker);
                Assert.Contains("L[\"Preview.Empty\"] : Preview", picker);
            }

            // Повтор стоит под ступенями: выбор ступени и его вывод видны вместе.
            Assert.True(reading.IndexOf("class=\"reading-modes\"") < reading.IndexOf("class=\"panel-preview"));
            Assert.True(reading.IndexOf("class=\"panel-preview") < reading.IndexOf("class=\"reading-block\""));

            var app = Regex.Replace(Web("App.razor"), @"\s+", " ");
            Assert.Matches(@"<ReadingPicker [^>]*Preview=""@_result""", app);
            Assert.Matches(@"<ProfilePicker [^>]*Preview=""@_result""", app);
        }

        [Fact]
        public void RuleRow_IsATable_NameAndExampleLeft_ControlsRight_DescriptionBehindAnArrow()
        {
            // I9: строка правила — стрелка описания, слева название с примером под ним,
            // справа переключатель. Шаблон строки один на оба блока.
            var reading = Regex.Replace(Web("Components", "ReadingPicker.razor"), @"\s+", " ");
            Assert.Single(Regex.Matches(reading, "<li class=\"reading-rule "));

            var row = Regex.Match(reading, @"<li class=""reading-rule (.*?)</li>");
            Assert.True(row.Success, "the rule row is not found");
            var markup = row.Groups[1].Value;

            var toggle = Regex.Match(markup, @"^[^>]*> <button type=""button"" class=""reading-rule-toggle""([^>]*)>");
            Assert.True(toggle.Success, "the row does not start with the description arrow");
            Assert.Contains("aria-expanded=\"@(described ? \"true\" : \"false\")\"", toggle.Groups[1].Value);
            Assert.Contains("aria-controls=\"@descriptionId\"", toggle.Groups[1].Value);
            Assert.Contains("aria-label=\"@(L[\"Reading.DescriptionLabel\", rule.Name.Current])\"", toggle.Groups[1].Value);

            // Название, под ним пример, под ним свёрнутое описание — в левой колонке;
            // переключатели — после неё, в правой.
            int At(string part)
            {
                var index = markup.IndexOf(part, StringComparison.Ordinal);
                Assert.True(index >= 0, $"'{part}' is not in the rule row");
                return index;
            }

            var main = At("<span class=\"reading-rule-main\">");
            var name = At("class=\"reading-rule-name\"");
            var example = At("<span class=\"reading-example\">");
            var description = At("<span id=\"@descriptionId\" class=\"reading-rule-text\" hidden=\"@(!described)\">");
            var controls = At("<span class=\"reading-rule-controls\">");
            Assert.True(main < name && name < example && example < description && description < controls,
                        "the row is not: name, example, description, then the controls");
            Assert.Matches(@"_openDescriptions\.Clear\(\);", reading);

            var css = Regex.Replace(Web("wwwroot", "css", "app.css"), @"\s+", " ");
            Assert.Matches(@"\.reading-rule \{[^}]*grid-template-columns: auto minmax\(0, 1fr\) auto;", css);
            Assert.Matches(@"\.reading-rule-controls \{[^}]*grid-column: 3;[^}]*justify-self: end;[^}]*white-space: nowrap;", css);
            Assert.Matches(@"\.reading-rule-main \{[^}]*min-width: 0;", css);

            Assert.Contains("{0}", PageStrings.Web("ru")["Reading.DescriptionLabel"]);
            Assert.Contains("{0}", PageStrings.Web("en")["Reading.DescriptionLabel"]);
        }

        /// <summary>
        /// Свой профиль создаётся без инструкции (E9): кнопка «Создать профиль» берёт
        /// за основу выбранный, пояснения про копию нет, а поиск и графемы — под
        /// стрелкой «Настройки профиля», раскрытой у только что созданного. Графемы —
        /// таблица: столбцы одной ширины, границы ячеек, варианты тише буквы.
        /// </summary>
        [Fact]
        public void ProfileSettings_FoldTheGraphemeTable_AndCreateOpensThem()
        {
            var picker = Regex.Replace(Web("Components", "ProfilePicker.razor"), @"\s+", " ");

            Assert.Contains("@onclick=\"CreateAsync\">@L[\"Profile.Create\"]</button>", picker);
            Assert.DoesNotContain("Profile.BuiltInNote", picker);
            Assert.DoesNotContain("Profile.Copy\"", picker);

            var toggle = Regex.Match(picker, @"<button type=""button"" class=""profile-settings-toggle""(.*?)>(.*?)</button>");
            Assert.True(toggle.Success, "the profile settings toggle is not found");
            Assert.Contains("aria-expanded=\"@(_settingsOpen ? \"true\" : \"false\")\"", toggle.Groups[1].Value);
            Assert.Contains("aria-controls=\"profile-settings\"", toggle.Groups[1].Value);
            Assert.Contains("L[\"Profile.Settings\"]", toggle.Groups[2].Value);

            var settings = picker.IndexOf("<div id=\"profile-settings\"", StringComparison.Ordinal);
            Assert.True(settings > toggle.Index, "the settings block is not under its toggle");
            Assert.True(picker.IndexOf("profile-search", StringComparison.Ordinal) > settings, "the search is not inside the settings");
            Assert.True(picker.IndexOf("<table class=\"profile-table\">", StringComparison.Ordinal) > settings, "the table is not inside the settings");

            // Свёрнуто при каждом закрытии панели, раскрыто у только что созданного профиля.
            Assert.Matches(@"OnClosed\(\) \{ Cancel\(\); _settingsOpen = false; \}", picker);
            Assert.Matches(@"CreateAsync\(\) \{.*?_settingsOpen = true; await ProfileChanged\.InvokeAsync\(copy\);", picker);

            var css = Regex.Replace(Web("wwwroot", "css", "app.css"), @"\s+", " ");
            Assert.Matches(@"\.profile-table \{[^}]*table-layout: fixed;[^}]*border-collapse: collapse;", css);
            Assert.Matches(@"\.profile-table th, \.profile-table td \{[^}]*border: 1px solid", css);
            Assert.Matches(@"\.profile-col-letter \{ width: [0-9.]+rem; \}", css);
            Assert.Matches(@"\.profile-col-value \{ width: [0-9.]+rem; \}", css);
            Assert.Matches(@"\.profile-row\.is-variant > td \{[^}]*color: var\(--ink-muted\);", css);
            Assert.Matches(@"\.profile-rules\[hidden\] \{ display: none; \}", css);

            foreach (var language in new[] { "ru", "en" })
            {
                var strings = PageStrings.Web(language);
                Assert.False(strings.ContainsKey("Profile.BuiltInNote"), $"{language}: the built-in note is still there");
                Assert.False(strings.ContainsKey("Profile.Copy"), $"{language}: the copy button text is still there");
            }

            Assert.Equal("Создать профиль", PageStrings.Web("ru")["Profile.Create"]);
            Assert.Equal("Create profile", PageStrings.Web("en")["Profile.Create"]);
            Assert.Equal("Настройки профиля", PageStrings.Web("ru")["Profile.Settings"]);
            Assert.Equal("Profile settings", PageStrings.Web("en")["Profile.Settings"]);
        }

        /// <summary>
        /// «О приложении» (D8): круглая кнопка «?» сбоку с видимой подписью «О чём проект?»
        /// (D9) — она же имя для диктора и title, в панели —
        /// подзаголовки, пример «как пишут обычно» и «как пишет страница» (её считает
        /// конвейер, а не текст), имена кнопок профиля из тех же строк, что на кнопках,
        /// и кнопка, открывающая форму отзыва.
        /// </summary>
        [Fact]
        public void AboutButton_OpensAPanelThatExplainsThePage()
        {
            var about = Regex.Replace(Web("Components", "AboutPicker.razor"), @"\s+", " ");

            var toggle = Regex.Match(about, "<button [^>]*aria-controls=\"about-menu\"[^>]*>(.*?)</button>");
            Assert.True(toggle.Success, "the about button is not found");
            Assert.Contains("title=\"@L[\"About.Toggle\"]\"", toggle.Value);
            Assert.Contains("<span class=\"picker-toggle-text\">@L[\"About.Toggle\"]</span>", toggle.Groups[1].Value);
            Assert.DoesNotContain("visually-hidden", toggle.Groups[1].Value);
            Assert.Contains("class=\"picker-panel about-panel @Closing.CssClass\"", about);

            foreach (var heading in new[] { "WhyTitle", "ExampleTitle", "HowTitle", "ProfilesTitle", "ThanksTitle" })
                Assert.Contains($"<h3 class=\"about-heading\">@L[\"About.{heading}\"]</h3>", about);

            Assert.Contains("private string? Ours => Transliterate?.Invoke(ExampleArabic);", about);
            Assert.Contains("<dd>@ours</dd>", about);
            Assert.Contains("L[\"About.Profiles\", L[\"Profile.Create\"].Value, L[\"Profile.Settings\"].Value]", about);
            Assert.Contains("@onclick=\"OpenFeedbackAsync\">@L[\"About.FeedbackAction\"]</button>", about);

            var app = Regex.Replace(Web("App.razor"), @"\s+", " ");
            Assert.Contains("<AboutPicker Transliterate=\"TransliterateExample\" OpenFeedback=\"OpenFeedbackAsync\" />", app);
            Assert.Contains("<FeedbackButton Arabic=\"@_arabic\" Transliteration=\"@_result\" @ref=\"_feedback\" />", app);

            Assert.Equal("О чём проект?", PageStrings.Web("ru")["About.Toggle"]);
            Assert.Equal("What is this about?", PageStrings.Web("en")["About.Toggle"]);
            Assert.Equal("О приложении", PageStrings.Web("ru")["About.Title"]);

            foreach (var language in new[] { "ru", "en" })
            {
                var profiles = PageStrings.Web(language)["About.Profiles"];
                Assert.Contains("{0}", profiles);
                Assert.Contains("{1}", profiles);
            }
        }

        /// <summary>
        /// D10: панели и вывод появляются и уходят коротко и плавно. Уходящая панель
        /// ещё миг в разметке, но inert и скрыта от диктора; подложки у неё уже нет.
        /// Одна пара анимаций и одна длительность на всё, и та же длительность —
        /// в коде, что держит уходящую панель. С prefers-reduced-motion движения нет.
        /// </summary>
        [Theory]
        [InlineData("SurahPicker.razor", "Closing", "IsShown")]
        [InlineData("ReadingPicker.razor", "Closing", "IsShown")]
        [InlineData("ProfilePicker.razor", "Closing", "IsShown")]
        [InlineData("AboutPicker.razor", "Closing", "IsShown")]
        [InlineData("FeedbackButton.razor", "_closing", "_open || _closing.IsClosing")]
        public void ClosingPanel_StaysAMoment_InertAndHiddenFromTheReader(string file, string closing, string shown)
        {
            var picker = Regex.Replace(Web("Components", file), @"\s+", " ");

            Assert.Contains($"@if ({shown}) {{ if (", picker);
            Assert.Matches(@"if \((IsOpen|_open)\) \{ <div class=""(picker|feedback)-backdrop""", picker);
            Assert.Matches($@"class=""(picker|feedback)-panel[^""]*@{closing}\.CssClass"" inert=""@{closing}\.IsClosing"" aria-hidden=""@{closing}\.AriaHidden"" role=""dialog""", picker);
        }

        [Fact]
        public void PanelsAndTheOutput_ShareOneAnimation_AndReducedMotionTurnsItOff()
        {
            var css = Regex.Replace(Web("wwwroot", "css", "app.css"), @"\s+", " ");

            var duration = Regex.Match(css, @"--motion-duration: ([0-9.]+)s;");
            Assert.True(duration.Success, "the motion duration is not found");
            var code = File.ReadAllText(Path.Combine(ReadmeTests.RepositoryRoot(), "Transliterator.Web", "Components", "PanelClosing.cs"));
            var milliseconds = Regex.Match(code, @"Duration = TimeSpan\.FromMilliseconds\((\d+)\)");
            Assert.True(milliseconds.Success, "PanelClosing.Duration is not found");
            Assert.Equal(double.Parse(duration.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) * 1000,
                         double.Parse(milliseconds.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture));

            Assert.Contains("@keyframes panel-in {", css);
            Assert.Contains("@keyframes panel-out {", css);
            foreach (var block in new[] { "picker-panel", "feedback-panel", "result" })
                Assert.Matches($@"\.{block} \{{[^}}]*animation: panel-in var\(--motion-duration\) var\(--motion-ease\);", css);
            Assert.Matches(@"\.picker-panel\.is-closing, \.feedback-panel\.is-closing \{ animation: panel-out var\(--motion-duration\) var\(--motion-ease\) forwards; pointer-events: none; \}", css);
            Assert.DoesNotMatch(@"animation: rise", css);

            var reduced = Regex.Match(css, @"@media \(prefers-reduced-motion: reduce\) \{(.*)$");
            Assert.True(reduced.Success, "the reduced-motion block is not found");
            Assert.Contains("animation: none !important; transition: none !important;", reduced.Groups[1].Value);
            Assert.Contains(".picker-panel.is-closing, .feedback-panel.is-closing { display: none; }", reduced.Groups[1].Value);
        }

        /// <summary>
        /// Токены дизайна и тёмная тема (D12): цвета и длительности заданы только в
        /// <c>:root</c> и в блоке тёмной темы, правила ниже берут их ссылками, а у
        /// полосы браузера — цвет фона своей темы.
        /// </summary>
        [Fact]
        public void ColorsAndDurations_LiveInTokens_AndTheDarkThemeFollowsTheSystem()
        {
            var css = Regex.Replace(Web("wwwroot", "css", "app.css"), @"/\*.*?\*/", " ", RegexOptions.Singleline);
            css = Regex.Replace(css, @"\s+", " ");

            var root = Regex.Match(css, @"^ ?:root \{([^}]*)\}");
            Assert.True(root.Success, "the :root tokens are not first in the file");
            var dark = Regex.Match(css, @"@media \(prefers-color-scheme: dark\) \{ :root \{([^}]*)\} \}");
            Assert.True(dark.Success, "the dark theme block is not found");
            Assert.Contains("color-scheme: light dark;", root.Groups[1].Value);

            var tokens = Regex.Matches(root.Groups[1].Value, @"(--[a-z0-9-]+): ([^;]+);")
                              .ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value);
            var darkTokens = Regex.Matches(dark.Groups[1].Value, @"(--[a-z0-9-]+): ([^;]+);")
                                  .ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value);
            foreach (var name in darkTokens.Keys)
                Assert.True(tokens.ContainsKey(name), $"the dark theme overrides {name}, which the light theme does not define");
            foreach (var name in new[] { "--cream", "--paper", "--ink", "--ink-muted", "--foam-700", "--error", "--error-bg", "--warn-bg", "--warn-ink", "--shadow-md" })
                Assert.True(darkTokens.ContainsKey(name), $"the dark theme does not set {name}");

            var rules = css.Remove(dark.Index, dark.Length).Remove(root.Index, root.Length);
            Assert.DoesNotMatch(@"#[0-9a-fA-F]{3,8}\b|rgba?\(|hsla?\(", rules);
            foreach (Match declaration in Regex.Matches(rules, @"(?:transition|animation): ([^;]*);"))
                Assert.DoesNotMatch(@"(?<![\w-])[0-9.]+m?s\b", declaration.Groups[1].Value);

            var index = Web("wwwroot", "index.html");
            Assert.Contains($"<meta name=\"theme-color\" content=\"{tokens["--cream"]}\" media=\"(prefers-color-scheme: light)\" />", index);
            Assert.Contains($"<meta name=\"theme-color\" content=\"{darkTokens["--cream"]}\" media=\"(prefers-color-scheme: dark)\" />", index);
        }

        [Fact]
        public void OnAWideScreen_ThePanelIsAColumnBesideTheFieldAndTheOutput()
        {
            var css = Regex.Replace(Web("wwwroot", "css", "app.css"), @"\s+", " ");
            var wide = Regex.Match(css, @"@media \(min-width: 64rem\) \{(.*?)\} /\* ---- Загрузка");
            Assert.True(wide.Success, "the wide-screen layout is not found");

            var rules = wide.Groups[1].Value;

            Assert.Matches(@"\.picker-panel, \.reading-panel, \.profile-panel \{[^}]*position: fixed", rules);
            Assert.Matches(@"\.shell\.has-panel \{[^}]*--rail-space: calc\(var\(--panel-left\) \+ var\(--panel-width\)\)", rules);
            Assert.Matches(@"\.stage, \.footer \{[^}]*margin-left: max\(var\(--rail-space\)", rules);

            // Ни подложки, которая закрыла бы панель щелчком по полю, ни повтора вывода
            // в панелях профиля (E4) и «Как читать» (I10): вывод и так рядом.
            Assert.Matches(@"\.picker-backdrop, \.panel-preview \{ display: none; \}", rules);
        }
    }
}
