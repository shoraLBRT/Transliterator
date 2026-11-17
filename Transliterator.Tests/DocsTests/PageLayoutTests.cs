using System.Text.RegularExpressions;
using Xunit;

namespace Transliterator.Tests.DocsTests
{
    /// <summary>
    /// Раскладка страницы (D6), решение владельца 2026-11-12: под полем остаётся
    /// только выбор суры, ступень чтения и профиль — круглые кнопки сбоку, и на
    /// широком экране их панель открывается колонкой, не закрывая поле и вывод.
    /// Список сур с D15 на широком экране — выпадающий под своей кнопкой.
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

            // D24: быстрый старт под полем — тоже выбор суры (ChooseAsync), а не
            // настройка чтения или профиля; те по-прежнему только сбоку.
            Assert.Contains("class=\"fold quick-start", field);
            Assert.Contains("@onclick=\"() => ChooseAsync(surah)\"", field);
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
        public void TheLoadingScreen_IsASkeletonOfThePage_WithProgress()
        {
            // D22: пока грузится WebAssembly, в #app — скелет той же страницы на тех же
            // классах каркаса, что в App.razor, поэтому поле встаёт на своё место без
            // скачка. Полоса и проценты — по --blazor-load-percentage, диктору —
            // progressbar с aria-valuenow от page.js. Готовая страница проявляется.
            var index = Web("wwwroot", "index.html");
            var css = Regex.Replace(Web("wwwroot", "css", "app.css"), @"\s+", " ");
            var script = Web("wwwroot", "js", "page.js");

            var app = Regex.Match(index, "<div id=\"app\">(.*?)<div id=\"blazor-error-ui\">", RegexOptions.Singleline);
            Assert.True(app.Success, "the app root is not found");
            var boot = app.Groups[1].Value;

            Assert.Contains("<div class=\"shell boot\" aria-busy=\"true\">", boot);
            foreach (var part in new[] { "class=\"topbar\"", "class=\"stage\"", "class=\"hint\"", "class=\"composer\"", "class=\"composer-input\"", "class=\"composer-bar\"", "class=\"side-rail\"" })
                Assert.Contains(part, boot);
            Assert.True(boot.IndexOf("class=\"topbar\"", StringComparison.Ordinal) < boot.IndexOf("class=\"hint\"", StringComparison.Ordinal)
                        && boot.IndexOf("class=\"hint\"", StringComparison.Ordinal) < boot.IndexOf("class=\"composer\"", StringComparison.Ordinal),
                        "the skeleton is not in the order of the page");
            Assert.DoesNotContain("<h1", boot);

            Assert.Matches(@"role=""progressbar"" aria-labelledby=""boot-loading""\s+aria-valuemin=""0"" aria-valuemax=""100"" aria-valuenow=""0""", boot);
            Assert.Contains("<span id=\"boot-loading\" data-shell=\"loading\">", boot);
            Assert.Contains("data-shell=\"heading\"", boot);
            Assert.Matches(@"\.boot-progress-bar \{[^}]*width: var\(--blazor-load-percentage, 0%\)", css);
            Assert.Contains("content: \" \" var(--blazor-load-percentage-text, \"\");", css);
            Assert.Contains("root.style.getPropertyValue(\"--blazor-load-percentage\")", script);
            Assert.Contains("bar.setAttribute(\"aria-valuenow\"", script);

            Assert.Contains(".shell:not(.boot) { animation: shell-in var(--motion-slow) var(--motion-ease) backwards; }", css);
            Assert.Matches(@"\.boot-block \{[^}]*animation: shimmer var\(--motion-shimmer\) linear infinite;", css);
            var reduced = Regex.Match(css, @"@media \(prefers-reduced-motion: reduce\) \{(.*)\}\s*$");
            Assert.True(reduced.Success, "the reduced-motion block is not found");
            Assert.Contains(".boot-block { background: var(--line); }", reduced.Groups[1].Value);
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

        /// <summary>
        /// D23: с 64rem кнопки сбоку — одна капсула на высоте поля, открытая кнопка
        /// залита акцентом и отмечена чертой. Пока открыта колонка, подпись — подсказка
        /// справа от значка при наведении и фокусе: с задержкой, на токенах движения,
        /// поверх колонки и по-прежнему имя кнопки для диктора (opacity, не visibility).
        /// Нативного title у кнопок нет — подсказка не двоится.
        /// </summary>
        [Fact]
        public void RailIcons_ShowTheirNameAsATooltip_WhileThePanelIsOpen()
        {
            var css = Regex.Replace(Web("wwwroot", "css", "app.css"), @"\s+", " ");
            var wide = Regex.Match(css, @"@media \(min-width: 64rem\) \{(.*?)\} /\* ---- Загрузка");
            Assert.True(wide.Success, "the wide-screen layout is not found");
            var rules = wide.Groups[1].Value;

            var rail = Regex.Match(rules, @"\.side-rail \{([^}]*)\}");
            Assert.True(rail.Success, "the rail rule is not found");
            foreach (var declaration in new[]
            {
                "position: fixed;", "top: calc(max(var(--space-3) + var(--topbar-height) + var(--space-6), 24vh) + 2.8 * var(--hint-size) + var(--space-5));",
                "background: var(--paper);", "border: 1px solid var(--line);", "box-shadow: var(--shadow-md);",
            })
                Assert.Contains(declaration, rail.Groups[1].Value);
            Assert.DoesNotContain("justify-content: center", rail.Groups[1].Value);
            Assert.Contains(".shell.is-active .side-rail { top: max(var(--space-3) + var(--topbar-height) + var(--space-4), 7vh); }", rules);

            Assert.Contains(".side-rail .picker-toggle[aria-expanded=\"true\"] svg { background: var(--foam-700); border-color: var(--foam-700); color: var(--paper); }", css);
            Assert.Matches(@"\.side-rail \.picker-toggle\[aria-expanded=""true""\]::before \{[^}]*background: var\(--foam-700\);", rules);

            var tip = Regex.Match(rules, @"\.shell\.has-panel \.side-rail \.picker-toggle-text \{([^}]*)\}");
            Assert.True(tip.Success, "the tooltip rule is not found");
            foreach (var declaration in new[]
            {
                "position: absolute;", "left: calc(100% + var(--space-3));", "z-index: 12;", "opacity: 0;", "pointer-events: none;",
            })
                Assert.Contains(declaration, tip.Groups[1].Value);
            Assert.DoesNotContain("visibility", tip.Groups[1].Value);
            Assert.DoesNotContain("clip", tip.Groups[1].Value);
            // Поверх колонки: подсказка выше её z-index.
            Assert.Matches(@"\.picker-panel \{ right: auto; bottom: auto; z-index: 11;", rules);
            Assert.Contains(".shell.has-panel .side-rail .picker-toggle:hover .picker-toggle-text, "
                            + ".shell.has-panel .side-rail .picker-toggle:focus-visible .picker-toggle-text "
                            + "{ opacity: 1; transform: translate(0, -50%); transition-delay: var(--motion-hint-delay); }", rules);
            Assert.Matches(@":root \{[^}]*--motion-hint-delay: 0\.3s;", css);

            foreach (var (file, panel) in new[] { ("ReadingPicker.razor", "reading-menu"), ("ProfilePicker.razor", "profile-menu"), ("AboutPicker.razor", "about-menu") })
            {
                var picker = Regex.Replace(Web("Components", file), @"\s+", " ");
                var button = Regex.Match(picker, $"<button [^>]*aria-controls=\"{panel}\"[^>]*>");
                Assert.True(button.Success, $"the {panel} button is not found");
                Assert.DoesNotContain("title=", button.Value);
            }
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
            // D21: свёрнутый блок остаётся в разметке и раскрывается плавно, но
            // свёрнутый — inert и aria-hidden: Tab и диктор его не достают.
            Assert.Contains("<div id=\"@blockId\" class=\"fold @(open ? \"is-open\" : null)\" inert=\"@(!open)\" aria-hidden=\"@(open ? null : \"true\")\"> <div class=\"fold-inner\"> <div class=\"reading-block-body\">", reading);
            Assert.DoesNotContain("hidden=\"@(!open)\"", reading);

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
                Assert.Contains("@if (string.IsNullOrEmpty(Preview)) {@L[\"Preview.Empty\"]} else {<ChangedText Segments=\"ChangedText.Of(Changes, Preview)\" />}", picker);
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
            var description = At("<span id=\"@descriptionId\" class=\"fold @(described ? \"is-open\" : null)\" inert=\"@(!described)\" aria-hidden=\"@(described ? null : \"true\")\"><span class=\"fold-inner\"><span class=\"reading-rule-text\">");
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
            Assert.Matches(@"OnClosed\(\) \{ Cancel\(\); _settingsOpen = false; _settingsKept = false; \}", picker);
            // D21: свёрнутые настройки — inert и aria-hidden, раскрываются плавно.
            Assert.Contains("<div id=\"profile-settings\" class=\"fold @(_settingsOpen ? \"is-open\" : null)\" inert=\"@(!_settingsOpen)\" aria-hidden=\"@(_settingsOpen ? null : \"true\")\"> <div class=\"fold-inner\"> <div class=\"profile-rules\">", picker);
            Assert.Matches(@"CreateAsync\(\) \{.*?_settingsOpen = true; await ProfileChanged\.InvokeAsync\(copy\);", picker);

            var css = Regex.Replace(Web("wwwroot", "css", "app.css"), @"\s+", " ");
            Assert.Matches(@"\.profile-table \{[^}]*table-layout: fixed;[^}]*border-collapse: collapse;", css);
            Assert.Matches(@"\.profile-table th, \.profile-table td \{[^}]*border: 1px solid", css);
            Assert.Matches(@"\.profile-col-letter \{ width: [0-9.]+rem; \}", css);
            Assert.Matches(@"\.profile-col-value \{ width: [0-9.]+rem; \}", css);
            Assert.Matches(@"\.profile-row\.is-variant > td \{[^}]*color: var\(--ink-muted\);", css);
            // D21: свёрнутый блок — строка сетки 0fr и невидим, раскрытый — 1fr.
            Assert.Matches(@"\.fold \{[^}]*grid-template-rows: 0fr;[^}]*visibility: hidden;", css);
            Assert.Matches(@"\.fold\.is-open \{[^}]*grid-template-rows: 1fr;[^}]*visibility: visible;", css);
            Assert.Matches(@"\.fold-inner \{[^}]*min-height: 0;[^}]*overflow: hidden;", css);

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
        /// (D9) — она же имя для диктора и подсказка у значка (D23), в панели —
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
            // D23: имя у значка показывает подсказка, title её бы двоил.
            Assert.DoesNotContain("title=", toggle.Value);
            Assert.Contains("<span class=\"picker-toggle-text\">@L[\"About.Toggle\"]</span>", toggle.Groups[1].Value);
            Assert.DoesNotContain("visually-hidden", toggle.Groups[1].Value);
            Assert.Contains("class=\"picker-panel about-panel @Closing.CssClass\"", about);

            foreach (var heading in new[] { "WhyTitle", "ExampleTitle" })
                Assert.Contains($"<h3 class=\"about-heading\">@L[\"About.{heading}\"]</h3>", about);

            // D26: лид и пример видны сразу, остальные разделы — под заголовком-кнопкой
            // (aria-expanded, свёрнутое тело inert + aria-hidden).
            foreach (var (id, heading) in new[] { ("how", "HowTitle"), ("profiles", "ProfilesTitle"), ("thanks", "ThanksTitle") })
                Assert.Contains($"@Folded(\"{id}\", \"About.{heading}\", ", about);
            Assert.Contains("class=\"about-fold-toggle\" aria-expanded=", about);
            Assert.Contains("inert=\"@(!open)\" aria-hidden=\"@(open ? null : \"true\")\"", about);

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
        /// «С арабским» (D18): тумблер в шапке карточки над каждой строкой вывода
        /// раскрывает её арабскую строку поля — справа налево, тише, шрифтом поля,
        /// не выделяемую мышью. Пары — из ядра (ResultLine.Sources), строке без пары
        /// арабской нет. Выбор помнится в браузере, а кнопка копирует только вывод.
        /// </summary>
        [Fact]
        public void TheArabicToggle_ShowsEachLinesArabicAbove_RemembersTheChoice_AndCopiesOnlyTheResult()
        {
            var app = Regex.Replace(Web("App.razor"), @"\s+", " ");
            var css = Regex.Replace(Web("wwwroot", "css", "app.css"), @"\s+", " ");

            var card = Regex.Match(app, @"<section class=""result"" aria-labelledby=""result-title"">(.*?)</section>");
            Assert.True(card.Success, "the output card is not found");
            var head = Regex.Match(card.Groups[1].Value, @"<div class=""result-head"">(.*?)<div class=""result-text""");
            Assert.True(head.Success, "the card head is not found");

            // Тумблер — в шапке перед «Копировать», с aria-pressed и подписью из строк.
            Assert.Matches(@"<button type=""button"" class=""arabic-toggle"" aria-pressed=""@\(_arabicLines \? ""true"" : ""false""\)"" title=""@L\[""Result\.ArabicTitle""\]"" @onclick=""ToggleArabicLinesAsync"">.*<span class=""arabic-toggle-label"">@L\[""Result\.Arabic""\]</span> </button>.*<button type=""button"" class=""copy-button", head.Groups[1].Value);

            // Над строкой вывода — её арабская строка, свёрнутая и inert без тумблера.
            Assert.Contains("var sources = ResultLine.Sources(_arabic, lines.Count);", card.Groups[1].Value);
            Assert.Contains("if (sources[i] is { } source) { <div class=\"fold result-source @(_arabicLines ? \"is-open\" : null)\" inert=\"@(!_arabicLines)\"><div class=\"fold-inner\"><p class=\"result-source-text\" dir=\"rtl\" lang=\"ar\">@source</p></div></div> }", card.Groups[1].Value);
            Assert.Matches(@"\.result-source-text \{[^}]*font-family: var\(--font-arabic\);[^}]*color: var\(--ink-muted\); text-align: right;[^}]*user-select: none;", css);

            // Близость: арабская вплотную к своей строке, пары — с воздухом и чертой.
            Assert.Matches(@"\.result-line \+ \.result-source\.is-open \{ margin-top: var\(--space-2\); padding-top: var\(--space-3\); border-top: 1px solid var\(--foam-100\); \}", css);
            Assert.Matches(@"\.result-source\.is-open \+ \.result-line \{ margin-top: 0; padding-top: 0\.15rem; \}", css);
            Assert.Matches(@"\.result-source-text \{[^}]*line-height: 1\.6;", css);

            // На телефоне — одна иконка, подпись остаётся имени кнопки.
            Assert.Matches(@"@media \(max-width: 36rem\) \{.*\.arabic-toggle-label \{ position: absolute; width: 1px; height: 1px; overflow: hidden; clip: rect\(0 0 0 0\);", css);

            // Выбор помнится; копирование — только вывод.
            Assert.Contains("private const string ArabicLinesKey = \"transliterator.arabicLines\";", app);
            Assert.Contains("await JS.InvokeAsync<string?>(\"page.load\", ArabicLinesKey)", app);
            Assert.Contains("await JS.InvokeVoidAsync(\"page.save\", ArabicLinesKey, _arabicLines ? \"1\" : string.Empty);", app);
            Assert.Matches(@"if \(firstRender\) \{.*await RestoreArabicLinesAsync\(\);", app);
            Assert.Contains("copied = await JS.InvokeAsync<bool>(\"page.copy\", _result);", app);

            Assert.Equal("С арабским", PageStrings.Web("ru")["Result.Arabic"]);
            Assert.Equal("With Arabic", PageStrings.Web("en")["Result.Arabic"]);
        }

        /// <summary>
        /// Карточка вывода (D17): видимая подпись, метки ступени и профиля, которые
        /// открывают свои панели, и кнопка копирования в шапке, липнущей к верху
        /// экрана. Аят — строкой, номер — круглым знаком с цифрой текстом, а кнопка
        /// копирует прежний вывод целиком, не собранный из строк.
        /// </summary>
        [Fact]
        public void TheOutputCard_HasAStickyHead_MarksAyahNumbers_AndCopiesTheWholeResult()
        {
            var app = Regex.Replace(Web("App.razor"), @"\s+", " ");
            var css = Regex.Replace(Web("wwwroot", "css", "app.css"), @"\s+", " ");
            var script = Web("wwwroot", "js", "page.js");

            var card = Regex.Match(app, @"<section class=""result"" aria-labelledby=""result-title"">(.*?)</section>");
            Assert.True(card.Success, "the output card is not found");
            var head = Regex.Match(card.Groups[1].Value, @"<div class=""result-head"">(.*?)</div> (?:@\*(?:(?!\*@).)*\*@ )?<div class=""result-text"" @key=""_changeKey"">");
            Assert.True(head.Success, "the card head is not found before the lines");

            Assert.Contains("<h2 id=\"result-title\" class=\"result-title\">@L[\"Result.Title\"]</h2>", head.Groups[1].Value);
            Assert.DoesNotContain("visually-hidden", head.Groups[1].Value);
            Assert.Matches(@"class=""result-chip""[^>]*aria-label=""@L\[""Result\.ReadingChip"", ReadingName\]"" @onclick=""OpenReadingAsync"">@ReadingName</button>", head.Groups[1].Value);
            Assert.Matches(@"class=""result-chip""[^>]*aria-label=""@L\[""Result\.ProfileChip"", _profile\.Name\]"" @onclick=""OpenProfileAsync"">@_profile\.Name</button>", head.Groups[1].Value);
            Assert.Matches(@"<button type=""button"" class=""copy-button @CopyClass"" @onclick=""CopyAsync"">.*<span class=""copy-label"" aria-live=""polite"">@CopyLabel</span> </button>", head.Groups[1].Value);

            // Метки — та же ступень, что у кнопки сбоку, и та же панель.
            Assert.Contains("private string ReadingName => ReadingPicker.ModeName(L, _reading.Mode);", app);
            Assert.Contains("private Task OpenReadingAsync() => _readingPicker?.OpenAsync() ?? Task.CompletedTask;", app);
            Assert.Contains("private Task OpenProfileAsync() => _profilePicker?.OpenAsync() ?? Task.CompletedTask;", app);
            Assert.Matches(@"<ReadingPicker [^>]*@ref=""_readingPicker"" />", app);
            Assert.Matches(@"<ProfilePicker [^>]*@ref=""_profilePicker"" />", app);

            // Номер аята — знак с цифрой текстом, пробел перед ним — в тексте строки.
            // Текст строки — через ChangedText (D19): изменённые сменой настройки слова в mark.
            Assert.Contains("@{ var lines = ResultLine.Split(_result); var sources = ResultLine.Sources(_arabic, lines.Count); } @for (var i = 0; i < lines.Count; i++)", card.Groups[1].Value);
            Assert.Contains("<div class=\"result-line\"><ChangedText Segments=\"Part(i, 0, line.Lead)\" /><span class=\"ayah-tail\"><ChangedText Segments=\"Part(i, line.Lead.Length, line.Word)\" />@(line.Gap)<span class=\"ayah-mark\">@line.Ayah</span></span></div>", card.Groups[1].Value);
            Assert.Matches(@"\.ayah-tail \{ white-space: nowrap; \}", css);
            Assert.Contains("<div class=\"result-line\"><ChangedText Segments=\"Part(i, 0, line.Text)\" /></div>", card.Groups[1].Value);
            Assert.DoesNotMatch(@"\.ayah-mark[^{]*\{[^}]*(?<![\w-])content:", css);

            // Кнопка копирует весь вывод, как и прежде.
            Assert.Contains("copied = await JS.InvokeAsync<bool>(\"page.copy\", _result);", app);
            Assert.Contains("copy(text)", script);

            Assert.Matches(@"\.result-head \{ position: sticky; top: 0;", css);
            Assert.Matches(@"\.result-line \{[^}]*white-space: pre-wrap; overflow-wrap: anywhere;", css);
            Assert.Matches(@"\.ayah-mark \{[^}]*font-variant-numeric: tabular-nums;", css);
            Assert.Matches(@"\.copy-button\.is-copied \.copy-icon-check \{ opacity: 1; stroke-dashoffset: 0; \}", css);

            foreach (var language in new[] { "ru", "en" })
            {
                Assert.Contains("{0}", PageStrings.Web(language)["Result.ReadingChip"]);
                Assert.Contains("{0}", PageStrings.Web(language)["Result.ProfileChip"]);
            }
            Assert.Equal("Транслитерация", PageStrings.Web("ru")["Result.Title"]);
        }

        /// <summary>
        /// D10: панели и вывод появляются и уходят коротко и плавно. Уходящая панель
        /// ещё миг в разметке, но inert и скрыта от диктора. Подложка формы отзыва
        /// уходит сразу; подложка листа панели (D14) гаснет вместе с ним, уже не ловя
        /// щелчков, а щелчок по открытой закрывает лист и возвращает фокус на кнопку.
        /// Одна длительность на всё, и та же длительность — в коде, что держит
        /// уходящую панель. С prefers-reduced-motion движения нет.
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
            picker = Regex.Replace(picker, @"@\*.*?\*@ ?", string.Empty);

            Assert.Contains($"@if ({shown}) {{ ", picker);
            if (file == "FeedbackButton.razor")
                Assert.Matches(@"if \(_open\) \{ <div class=""feedback-backdrop""", picker);
            else
                Assert.Contains($"@if ({shown}) {{ <div class=\"picker-backdrop @Closing.CssClass\" @onclick=\"CloseToToggleAsync\"></div>", picker);
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
            foreach (var block in new[] { "feedback-panel", "result" })
                Assert.Matches($@"\.{block} \{{[^}}]*animation: panel-in var\(--motion-duration\) var\(--motion-ease\);", css);
            Assert.Matches(@"\.picker-panel\.is-closing, \.feedback-panel\.is-closing \{ animation: panel-out var\(--motion-duration\) var\(--motion-ease\) forwards; pointer-events: none; \}", css);
            // D14: уже 64rem панель — лист и выезжает снизу на той же длительности;
            // колонка с 64rem берёт прежнюю пару panel-in / panel-out.
            Assert.Matches(@"\.picker-panel \{[^}]*animation: sheet-in var\(--motion-duration\) var\(--motion-ease\);", css);
            Assert.Contains(".picker-panel.is-closing { animation-name: sheet-out; }", css);
            Assert.Matches(@"\.picker-backdrop \{[^}]*animation: scrim-in var\(--motion-duration\) var\(--motion-ease\);", css);
            Assert.Contains(".picker-backdrop.is-closing { animation: scrim-out var(--motion-duration) var(--motion-ease) forwards; pointer-events: none; }", css);
            Assert.DoesNotMatch(@"animation: rise", css);

            var reduced = Regex.Match(css, @"@media \(prefers-reduced-motion: reduce\) \{(.*)$");
            Assert.True(reduced.Success, "the reduced-motion block is not found");
            Assert.Contains("animation: none !important; transition: none !important;", reduced.Groups[1].Value);
            // D20: уходящая плашка отмены очистки скрыта сразу — как уходящие панели
            // и подложка листа (D14).
            Assert.Contains(".picker-panel.is-closing, .picker-backdrop.is-closing, .feedback-panel.is-closing, .toast.is-closing { display: none; }", reduced.Groups[1].Value);
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
            // в панелях профиля (E4) и «Как читать» (I10): вывод и так рядом. Ни ручки
            // листа (D14), ни запрета прокрутки страницы рядом с колонкой.
            Assert.Matches(@"\.picker-backdrop, \.sheet-handle, \.panel-preview \{ display: none; \}", rules);
            Assert.Contains("html.is-sheet-open { overflow: visible;", rules);
            Assert.Contains(".picker-panel.is-closing { animation-name: panel-out; }", rules);
            Assert.Matches(@"\.picker-panel \{[^}]*border-radius: var\(--radius-xl\);[^}]*animation-name: panel-in;", rules);
        }

        /// <summary>
        /// D15, пересмотр D6 только для сур: на широком экране список сур — не колонка
        /// у края страницы, а выпадающий под своей кнопкой. Он ничего не сдвигает
        /// (страница не ставит has-panel для сур), закрывается щелчком мимо по
        /// прозрачной подложке, а «Как читать» и профиль остаются колонкой.
        /// </summary>
        [Fact]
        public void OnAWideScreen_TheSurahListDropsDownUnderItsButton_AndMovesNothing()
        {
            var css = Regex.Replace(Web("wwwroot", "css", "app.css"), @"\s+", " ");
            var wide = Regex.Match(css, @"@media \(min-width: 64rem\) \{(.*?)\} /\* ---- Загрузка");
            Assert.True(wide.Success, "the wide-screen layout is not found");
            var rules = wide.Groups[1].Value;

            Assert.Contains(".surah-picker { position: relative; }", rules);
            var dropdown = Regex.Match(rules, @"\.picker-panel\.surah-panel \{([^}]*)\}");
            Assert.True(dropdown.Success, "the surah dropdown rule is not found");
            foreach (var declaration in new[]
            {
                "position: absolute;", "top: calc(100% + var(--space-2));", "left: 0;",
                "max-height: 60dvh;", "box-shadow: var(--shadow-lg);", "--panel-shift-x: 0;",
            })
                Assert.Contains(declaration, dropdown.Groups[1].Value);
            Assert.Contains(".surah-picker .picker-backdrop { display: block; background: transparent;", rules);
            // Колонкой по-прежнему открываются остальные панели.
            Assert.Matches(@"\.picker-panel, \.reading-panel, \.profile-panel \{[^}]*position: fixed", rules);

            var app = Regex.Replace(Web("App.razor"), @"\s+", " ");
            Assert.Contains("@(Panels.Current is not (null or \"surah\") ? \"has-panel\" : null)", app);

            var picker = Regex.Replace(Web("Components", "SurahPicker.razor"), @"\s+", " ");
            Assert.Contains("protected override string Panel => \"surah\";", picker);
            Assert.Contains("<div class=\"picker surah-picker\">", picker);
            Assert.Contains("class=\"picker-panel surah-panel @Closing.CssClass\"", picker);
        }

        /// <summary>
        /// D15: в списке сур — поиск (combobox с aria-activedescendant над listbox),
        /// стрелки и Enter; Escape закрывает, как у всех панелей. Фокус в поиск —
        /// только на широком экране: на телефоне клавиатура не выскакивает сама.
        /// Под именем — число аятов, текущая сура отмечена.
        /// </summary>
        [Fact]
        public void TheSurahList_HasASearch_KeyboardChoice_AyahCounts_AndTheCurrentMark()
        {
            var picker = Regex.Replace(Web("Components", "SurahPicker.razor"), @"\s+", " ");

            Assert.Matches(@"<input type=""search"" class=""surah-search-input"" role=""combobox"" aria-label=""@L\[""Surah\.SearchLabel""\]""(?:(?!/>).)*aria-controls=""surah-list""(?:(?!/>).)*aria-activedescendant=""@ActiveId""", picker);
            Assert.Contains("<ul id=\"surah-list\" class=\"picker-list\" role=\"listbox\"", picker);
            Assert.Matches(@"<li id=""@OptionId\(surah\)"" role=""option"" aria-selected=""@\(current \? ""true"" : ""false""\)""", picker);
            Assert.Contains("SurahSearch.Filter(Surahs, _query)", picker);
            Assert.Contains("<p class=\"surah-empty\" role=\"status\">@L[\"Surah.NothingFound\"]</p>", picker);
            foreach (var key in new[] { "case \"ArrowDown\":", "case \"ArrowUp\":", "case \"Enter\":" })
                Assert.Contains(key, picker);
            Assert.Contains("@L.Count(\"Surah.AyahCount\", surah.Ayahs.Count)", picker);
            Assert.Contains("surah.Number == Current", picker);

            Assert.Contains("if (await JS.InvokeAsync<bool>(\"page.wide\")) await _search.FocusAsync(); else await base.FocusOpenedAsync();", picker);
            var script = Web("wwwroot", "js", "page.js");
            Assert.Contains("return window.matchMedia(\"(min-width: 64rem)\").matches;", script);

            var app = Regex.Replace(Web("App.razor"), @"\s+", " ");
            Assert.Contains("<SurahPicker Surahs=\"_surahs\" Chosen=\"ChooseAsync\" Current=\"CurrentSurah\" />", app);
        }

        /// <summary>
        /// D14: уже 64rem панель — нижний лист: прижат к низу, скруглён сверху, до 85%
        /// высоты экрана, со своей прокруткой, которая не утягивает страницу, и с
        /// отступом под вырез телефона. Под ним затемнённая подложка из токена обеих
        /// тем; шапка с ручкой липнет к верху листа, заголовки внутри — под неё.
        /// Пока панель открыта, страница получает класс, снимающий её прокрутку.
        /// </summary>
        [Fact]
        public void OnAPhone_ThePanelIsABottomSheetOverAScrim_AndThePageDoesNotScroll()
        {
            var css = Regex.Replace(Web("wwwroot", "css", "app.css"), @"\s+", " ");
            var wide = css.IndexOf("@media (min-width: 64rem) {", StringComparison.Ordinal);
            Assert.True(wide > 0, "the wide-screen layout is not found");
            var narrow = css[..wide];

            var sheet = Regex.Match(narrow, @"\.picker-panel \{([^}]*)\}");
            Assert.True(sheet.Success, "the sheet rule is not found");
            foreach (var declaration in new[]
            {
                "position: fixed;", "right: 0;", "bottom: 0;", "left: 0;", "max-height: 85dvh;",
                "overflow-y: auto;", "overscroll-behavior: contain;",
                "border-radius: var(--radius-xl) var(--radius-xl) 0 0;",
                "env(safe-area-inset-bottom, 0px)",
            })
                Assert.Contains(declaration, sheet.Groups[1].Value);

            Assert.Matches(@"\.picker-backdrop \{[^}]*position: fixed; inset: 0;[^}]*background: var\(--scrim\);", narrow);
            Assert.Matches(@":root \{[^}]*--scrim: ", css);
            Assert.Matches(@"@media \(prefers-color-scheme: dark\) \{ :root \{[^}]*--scrim: ", css);

            Assert.Matches(@"\.panel-head \{ position: sticky; top: 0;[^}]*height: var\(--panel-head-height\);", narrow);
            Assert.Matches(@"\.reading-group \{ position: sticky; top: var\(--panel-head-height\);", narrow);
            Assert.Matches(@"\.profile-preview \{ position: sticky; top: var\(--panel-head-height\);", narrow);
            Assert.Contains("html.is-sheet-open { overflow: hidden;", narrow);

            var head = Regex.Replace(Web("Components", "PanelHead.razor"), @"\s+", " ");
            Assert.Contains("<div class=\"panel-head\"> ", head);
            Assert.Contains("<span class=\"sheet-handle\" aria-hidden=\"true\"></span>", head);

            var app = Regex.Replace(Web("App.razor"), @"\s+", " ");
            Assert.Contains("JS.InvokeVoidAsync(\"page.sheetOpen\", Panels.Current is not null)", app);
            var script = Web("wwwroot", "js", "page.js");
            Assert.Contains("document.documentElement.classList.toggle(\"is-sheet-open\", open);", script);
        }

        /// <summary>
        /// Поле ввода (D20): растёт по тексту без своей прокрутки, пустое предлагает
        /// вставку из буфера, а очистку можно отменить плашкой внизу страницы.
        /// </summary>
        [Fact]
        public void TheField_GrowsWithoutItsOwnScroll_OffersPaste_AndClearingCanBeUndone()
        {
            var app = Regex.Replace(Web("App.razor"), @"\s+", " ");
            var css = Regex.Replace(Web("wwwroot", "css", "app.css"), @"\s+", " ");
            var script = Web("wwwroot", "js", "page.js");

            // Своей прокрутки нет: ни потолка в стилях, ни потолка в скрипте; ширину
            // поля, которую сдвигает панель-колонка, отслеживает ResizeObserver.
            var input = Regex.Match(css, @"\.composer-input \{([^}]*)\}");
            Assert.True(input.Success, "the field style is not found");
            Assert.DoesNotContain("max-height", input.Groups[1].Value);
            Assert.Contains("overflow-y: hidden;", input.Groups[1].Value);
            Assert.DoesNotContain("maxHeight", script);
            Assert.Contains("element.style.height = `${element.scrollHeight}px`;", script);
            Assert.Contains("new ResizeObserver(", script);

            // «Вставить» — только у пустого поля и только если браузер читает буфер.
            Assert.Contains("_canPaste = await JS.InvokeAsync<bool>(\"page.canPaste\");", app);
            Assert.Matches(@"@if \(HasText\) \{ <button type=""button"" class=""icon-button""[^>]*@onclick=""ClearAsync"">.*?</button> \} else if \(_canPaste\) \{ <button type=""button"" class=""picker-toggle paste-button""[^>]*@onclick=""PasteAsync"">", app);
            Assert.Contains("@if (!HasText && _canPaste) { <p class=\"composer-note\" role=\"status\">@_pasteNote</p> }", app);
            Assert.Contains("navigator.clipboard.readText()", script);
            Assert.Matches(@"_pasteNote = text is null \? L\[""Composer.PasteFailed""\]", app);

            // Плашка: живая область в разметке всегда, внутри — текст и «Вернуть».
            Assert.Matches(@"<div class=""toast-region"" role=""status"" aria-live=""polite""> @if \(_cleared is not null\) \{ <div class=""toast[^>]*> <span class=""toast-text"">@L\[""Toast.Cleared""\]</span> <button type=""button"" class=""toast-action""[^>]*@onclick=""UndoClearAsync"">@L\[""Toast.Undo""\]</button>", app);
            Assert.Contains("_cleared = _arabic;", app);
            Assert.Matches(@"private async Task UndoClearAsync\(\) \{ .*?_arabic = _cleared; _resizePending = true; Calculate\(\);.*?await _input.FocusAsync\(\);", app);
            Assert.Matches(@"private void OnInput\(ChangeEventArgs e\) \{ [^}]*_ = HideToastAsync\(\); \}", app);

            // Снизу по центру, над вырезом телефона, на токенах движения, и с 56rem
            // поля шире значка отзыва в углу.
            Assert.Matches(@"\.toast-region \{[^}]*position: fixed;[^}]*bottom: calc\(var\(--space-4\) \+ env\(safe-area-inset-bottom, 0px\)\);[^}]*justify-content: center;", css);
            Assert.Matches(@"\.toast \{[^}]*animation: toast-in var\(--motion-duration\) var\(--motion-ease\);", css);
            Assert.Matches(@"\.toast\.is-closing \{ animation: toast-out var\(--motion-duration\) var\(--motion-ease\) forwards; \}", css);
            // D23: значок отзыва раскрывается подписью до 9rem — поля плашки шире раскрытой капсулы.
            Assert.Matches(@"@media \(min-width: 56rem\) \{ \.toast-region \{[^}]*padding: 0 calc\(var\(--space-5\) \+ 2\.75rem \+ 9rem \+ var\(--space-6\) \+ var\(--space-4\)\);", css);
            Assert.Matches(@"\.feedback-toggle\[aria-expanded=""true""\] \.feedback-label \{ max-width: 9rem;", css);
        }

        [Fact]
        public void SettingChange_HighlightsChangedWords_TypingDoesNot()
        {
            // D19: смена ступени, правила, счёта и профиля идёт через RecalculateSetting —
            // сравнение с прежним выводом; набор текста, сура и восстановление — через
            // Calculate, который подсветку сбрасывает.
            var app = Regex.Replace(Web("App.razor"), @"\s+", " ");
            var css = Regex.Replace(Web("wwwroot", "css", "app.css"), @"\s+", " ");

            Assert.Matches(@"private async Task ChangeReadingAsync\(ReadingChoice reading\) \{ _reading = reading; RecalculateSetting\(\);", app);
            Assert.Matches(@"private async Task ChangeProfileAsync\(TransliterationProfile profile\) \{.*?if \(sameRules\) Calculate\(\); else RecalculateSetting\(\);", app);
            Assert.Matches(@"private async Task ResetProfileAsync\(\) \{.*?_profile = profile; RecalculateSetting\(\);", app);
            Assert.Matches(@"private void OnInput\(ChangeEventArgs e\) \{ _arabic = [^}]*; Calculate\(\);", app);
            Assert.Matches(@"private async Task RestoreReadingAsync\(\) \{.*?_reading = reading; Calculate\(\);", app);
            Assert.Matches(@"private void Calculate\(\) \{.*?_changes = null; _unchangedNote = false;", app);
            Assert.Contains("var diff = OutputDiff.Compare(before, _result);", app);
            Assert.Matches(@"if \(diff\.HasChanges\) \{ _changes = diff; _ = ExpireChangesAsync\(\+\+_changeKey\); \} else if \(before == _result\) \{ _ = ShowUnchangedNoteAsync\(\); \}", app);

            // Подсветка проигрывается заново: новый ключ пересоздаёт строки и повтор в панелях.
            Assert.Contains("<div class=\"result-text\" @key=\"_changeKey\">", app);
            Assert.Matches(@"<ReadingPicker [^>]*Changes=""_changes"" ChangeKey=""_changeKey"" Unchanged=""_unchangedNote""", app);
            Assert.Matches(@"<ProfilePicker [^>]*Changes=""_changes"" ChangeKey=""_changeKey"" Unchanged=""_unchangedNote""", app);
            foreach (var file in new[] { "ReadingPicker.razor", "ProfilePicker.razor" })
            {
                var picker = Regex.Replace(Web("Components", file), @"\s+", " ");
                Assert.Contains("<span class=\"panel-preview-text\" @key=\"ChangeKey\">", picker);
                Assert.Matches(@"<PanelHead Id=""[a-z]+-menu-title"" Title=""@L\[""[A-Za-z]+\.Title""\]"" Close=""CloseToToggleAsync"" Note=""@\(Unchanged \? L\[""Output\.Unchanged""\]\.Value : null\)"" />", picker);
                Assert.DoesNotContain("panel-preview-note", picker);
            }

            // «Не изменился» у шапки вывода — только когда панель рядом (64rem), живая область всегда в разметке.
            // Подпись делит ячейку с метками и на время сменяет их: шапка не растёт, строки не сдвигаются.
            Assert.Contains("<div class=\"result-meta @(_unchangedNote ? \"has-note\" : null)\"> <div class=\"result-chips\">", app);
            Assert.Contains("</div> <p class=\"result-note\" role=\"status\">@(_unchangedNote ? L[\"Output.Unchanged\"].Value : null)</p> </div>", app);
            Assert.Matches(@"\.result-meta \{ display: grid; grid-template-columns: minmax\(0, 1fr\);", css);
            Assert.Matches(@"\.result-meta > \* \{ grid-area: 1 / 1; \}", css);
            Assert.Matches(@"\.result-note \{ display: none;[^}]*white-space: nowrap; text-overflow: ellipsis; \}", css);
            Assert.DoesNotMatch(@"\.result-note \{[^}]*position: absolute", css);
            Assert.Matches(@"@media \(min-width: 64rem\) \{ \.result-note \{ display: block; opacity: 0;", css);
            Assert.Matches(@"\.result-meta\.has-note \.result-chips \{ visibility: hidden; opacity: 0;", css);

            // Уже 64rem подпись — в липкой шапке листа: видна на любой глубине прокрутки.
            var panelHead = Regex.Replace(Web("Components", "PanelHead.razor"), @"\s+", " ");
            Assert.Contains("<h2 id=\"@Id\" class=\"panel-title\">@Title</h2> @*", panelHead);
            Assert.Contains("<p class=\"panel-head-note\" role=\"status\">@Note</p>", panelHead);
            Assert.Matches(@"\.panel-head-note \{[^}]*-webkit-line-clamp: 2;", css);
            Assert.Matches(@"@media \(min-width: 64rem\) \{.*?\.panel-head-note \{ display: none; \}", css);

            // Повтор в панели обрезан до трёх строк — пока подсветка идёт, он начинается со строки изменения.
            Assert.Contains("private static readonly TimeSpan PreviewHoldDuration = TimeSpan.FromSeconds(3);", app);
            Assert.Matches(@"private async Task ExpireChangesAsync\(int key\) \{ await Task\.Delay\(PreviewHoldDuration\); if \(key != _changeKey \|\| _changes is null\) return;", app);
            Assert.Contains("return changes.Segments(first).Prepend(new DiffSegment(Ellipsis, false)).ToArray();", Web("Components", "ChangedText.razor"));

            // mark — только обёртка: цвет текста прежний, фон гаснет за токен движения.
            var text = Web("Components", "ChangedText.razor");
            Assert.Contains("{<mark class=\"changed\">@segment.Text</mark>}", text);
            Assert.Matches(@"mark\.changed \{ background: transparent; color: inherit;[^}]*animation: change-flash var\(--motion-highlight\) var\(--ease-soft\);", css);
            Assert.Equal(2, Regex.Matches(css, @"--highlight: #[0-9a-f]{6};").Count);
            Assert.Contains("--motion-highlight: 1.2s;", css);

            // Без движения — ровный фон столько же и снимается разом.
            Assert.Matches(@"@media \(prefers-reduced-motion: reduce\) \{.*mark\.changed \{ animation: change-hold var\(--motion-highlight\) steps\(1\) !important; \}", css);
            Assert.Matches(@"@keyframes change-hold \{ from, to \{ background-color: var\(--highlight\);", css);
        }
    }
}
