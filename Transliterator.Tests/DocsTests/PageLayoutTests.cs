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

            // D28: быстрого старта D24 под полем больше нет — ни чипов, ни подписи,
            // ни пояснения; сура выбирается одной кнопкой.
            Assert.DoesNotContain("quick-start", field);
            Assert.DoesNotContain("@onclick=\"() => ChooseAsync(surah)\"", field);
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

            Assert.Contains(".shell:not(.boot) { animation: shell-in var(--motion-duration) var(--motion-ease) backwards; }", css);
            Assert.Contains("@keyframes shell-in { from { opacity: 0.6; } }", css);
            Assert.Matches(@"\.boot-block \{[^}]*animation: shimmer var\(--motion-shimmer\) linear infinite;", css);
            var reduced = Regex.Match(css, @"@media \(prefers-reduced-motion: reduce\) \{(.*)\}\s*$");
            Assert.True(reduced.Success, "the reduced-motion block is not found");
            Assert.Contains(".boot-block { background: var(--line); animation: none; }", reduced.Groups[1].Value);
        }

        [Fact]
        public void TheHeader_NamesThePage_AndHoldsTheLanguage_InTheFlow()
        {
            // D13: шапка над полем — знак и видимое имя (единственный h1) слева,
            // язык справа. Она в потоке: переключатель языка больше не стоит
            // поверх страницы и не наезжает на подсказку; с 64rem шапка стоит в одной
            // колонке с полем и, как оно, при открытии панели не двигается (D32).
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

        /// <summary>
        /// D31: браузер отбрасывает SVG, который не разбирается как XML (раньше
        /// его ломало «--» в комментарии), и вкладка остаётся без значка. Рядом —
        /// запасной PNG 32×32 для браузеров без SVG-значков.
        /// </summary>
        [Fact]
        public void TabIcon_SvgIsWellFormedXml_AndAPngFallbackIsLinked()
        {
            var wwwroot = Path.Combine(ReadmeTests.RepositoryRoot(), "Transliterator.Web", "wwwroot");

            var svg = System.Xml.Linq.XDocument.Load(Path.Combine(wwwroot, "favicon.svg"));
            Assert.Equal("{http://www.w3.org/2000/svg}svg", svg.Root!.Name.ToString());

            var index = Web("wwwroot", "index.html");
            Assert.Contains("<link rel=\"icon\" href=\"favicon-32.png\" type=\"image/png\" sizes=\"32x32\" />", index);

            var png = File.ReadAllBytes(Path.Combine(wwwroot, "favicon-32.png"));
            Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, png.Take(8).ToArray());
            int BigEndian(int at) => (png[at] << 24) | (png[at + 1] << 16) | (png[at + 2] << 8) | png[at + 3];
            Assert.Equal(32, BigEndian(16));
            Assert.Equal(32, BigEndian(20));
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
            // aria-controls — только пока панель в разметке (D25): ссылка на id,
            // которого нет, — ошибка ARIA.
            Assert.Contains($"aria-controls=\"@(IsShown ? \"{panel}\" : null)\"", picker);
            Assert.Contains("@onkeydown=\"OnPanelKeyDownAsync\"", picker);
        }

        [Fact]
        public void RailButtons_ShowTheCurrentChoice()
        {
            // Условие E6 в новой раскладке: у круглой кнопки видно, что выбрано.
            var reading = Regex.Replace(Web("Components", "ReadingPicker.razor"), @"\s+", " ");
            var toggle = Regex.Match(reading, "<button [^>]*aria-controls=\"@\\(IsShown \\? \"reading-menu\" : null\\)\"[^>]*>(.*?)</button>");

            Assert.True(toggle.Success, "the reading button is not found");
            Assert.Contains("<span class=\"picker-toggle-text\">@ModeName(Choice.Mode)</span>", toggle.Groups[1].Value);
        }

        /// <summary>
        /// D23: с 64rem кнопки сбоку — одна капсула на высоте поля, открытая кнопка
        /// залита акцентом и отмечена чертой. D40: капсула всегда столбик значков —
        /// с колонкой и без, has-panel её не меняет; подпись — подсказка справа от
        /// значка при наведении и фокусе: с задержкой, на токенах движения, поверх
        /// колонки и по-прежнему имя кнопки для диктора (opacity, не visibility).
        /// Нативного title у кнопок нет — подсказка не двоится.
        /// </summary>
        [Fact]
        public void RailIcons_AlwaysShowTheirNameAsATooltip()
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
                "padding: var(--space-1);", "border-radius: var(--radius-pill);",
            })
                Assert.Contains(declaration, rail.Groups[1].Value);
            // Вид капсулы и подписей один, открыта колонка или нет (D40).
            Assert.DoesNotContain(".shell.has-panel .side-rail {", css);
            Assert.DoesNotMatch(@"has-panel[^{]*\.picker-toggle-text", css);
            Assert.DoesNotContain("justify-content: center", rail.Groups[1].Value);
            Assert.Contains(".shell.is-active .side-rail { top: max(var(--space-3) + var(--topbar-height) + var(--space-4), 7vh); }", rules);

            Assert.Contains(".side-rail .picker-toggle[aria-expanded=\"true\"] svg { background: var(--foam-700); border-color: var(--foam-700); color: var(--paper); }", css);
            Assert.Matches(@"\.side-rail \.picker-toggle\[aria-expanded=""true""\]::before \{[^}]*background: var\(--foam-700\);", rules);

            var tip = Regex.Match(rules, @"(?<![\w-]) \.side-rail \.picker-toggle-text \{([^}]*)\}");
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
            Assert.Contains(" .side-rail .picker-toggle:hover .picker-toggle-text, "
                            + ".side-rail .picker-toggle:focus-visible .picker-toggle-text "
                            + "{ opacity: 1; transform: translate(0, -50%); transition: opacity var(--motion-fast) var(--ease-soft), "
                            + "transform var(--motion-move-fast) var(--motion-ease); transition-delay: var(--motion-hint-delay); }", rules);
            Assert.Matches(@":root \{[^}]*--motion-hint-delay: 0\.3s;", css);

            foreach (var (file, panel) in new[] { ("ReadingPicker.razor", "reading-menu"), ("ProfilePicker.razor", "profile-menu"), ("AboutPicker.razor", "about-menu") })
            {
                var picker = Regex.Replace(Web("Components", file), @"\s+", " ");
                var button = Regex.Match(picker, $"<button [^>]*aria-controls=\"@\\(IsShown \\? \"{panel}\" : null\\)\"[^>]*>");
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
            Assert.True(picker.IndexOf("<table class=\"profile-table ", StringComparison.Ordinal) > settings, "the table is not inside the settings");

            // Свёрнуто при каждом закрытии панели, раскрыто у только что созданного профиля.
            Assert.Matches(@"OnClosed\(\) \{ Cancel\(\); _focusSettings = false; _settingsOpen = false; _settingsKept = false; \}", picker);
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

            var toggle = Regex.Match(about, "<button [^>]*aria-controls=\"@\\(IsShown \\? \"about-menu\" : null\\)\"[^>]*>(.*?)</button>");
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
            Assert.Contains("<AboutPicker Transliterate=\"TransliterateExample\" OpenFeedback=\"OpenFeedbackAsync\" OpenSounds=\"SoundsOpener\" />", app);
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
        /// D11, E11: «Как читать буквы». Звучание каждой буквы — четвёртый столбец
        /// таблицы графем под «Настройки профиля», одной ячейкой на букву, как сама
        /// буква; у звука, которого нет в русском, — пометка. Отдельной панели звуков
        /// и отдельного входа в панели профиля нет.
        /// </summary>
        [Fact]
        public void SoundOfEachLetter_IsAColumnOfTheProfileTable()
        {
            var profile = Regex.Replace(Web("Components", "ProfilePicker.razor"), @"\s+", " ");
            profile = Regex.Replace(profile, @"@\*.*?\*@ ?", string.Empty);

            Assert.Contains("<table class=\"profile-table @(Sounds is not null ? \"has-sounds\" : null)\">", profile);
            Assert.Contains("@if (Sounds is not null) { <col class=\"profile-col-sound\" /> }", profile);
            Assert.Contains("@if (Sounds is not null) { <th scope=\"col\">@L[\"Profile.SoundColumn\"]</th> }", profile);

            // В строке буквы, после графемы, на все её строки — как сама буква.
            Assert.Contains("<th scope=\"rowgroup\" rowspan=\"@rows\" class=\"profile-key\"", profile);
            Assert.Contains("<td class=\"profile-value-cell\">@Cell(group.BaseKey)</td> @if (Sounds is not null) { <td rowspan=\"@rows\" class=\"profile-sound-cell\">@Sound(group.BaseKey)</td> } </tr>", profile);

            Assert.Contains("<span class=\"profile-sound-name\">@sound.Name.Current</span>", profile);
            Assert.Contains("@if (sound.NotInRussian) { <span class=\"profile-sound-mark\">@L[\"Profile.NotInRussian\"]</span> }", profile);
            Assert.Contains("<span class=\"profile-sound-how\"><MixedText Text=\"@sound.Pronunciation.Current\" /></span>", profile);

            // Поиск находит букву и по звучанию.
            Assert.Contains("RuleTable.Filter(_groups, _query, Describe)", profile);

            // Отдельного входа больше нет.
            Assert.DoesNotContain("OpenSounds", profile);
            Assert.DoesNotContain("profile-sounds", profile);
            Assert.False(File.Exists(Path.Combine(ReadmeTests.RepositoryRoot(), "Transliterator.Web", "Components", "SoundsPicker.razor")),
                         "the separate sound panel is still there");

            var app = Regex.Replace(Web("App.razor"), @"\s+", " ");
            Assert.Matches(@"<ProfilePicker [^>]*Sounds=""_sounds"" @ref=""_profilePicker"" />", app);
            Assert.DoesNotContain("SoundsPicker", app);

            // Столбцы — без прокрутки вбок: ширина фиксирована у узких, звучание — остаток.
            var css = Regex.Replace(Web("wwwroot", "css", "app.css"), @"\s+", " ");
            Assert.Contains(".profile-table.has-sounds .profile-col-value { width: 4.75rem; }", css);
            Assert.Contains(".profile-table.has-sounds .profile-col-variant { width: 30%; }", css);
            Assert.DoesNotContain(".sounds-panel", css);
            Assert.DoesNotContain(".sounds-fold-toggle", css);

            Assert.Equal("Звучание", PageStrings.Web("ru")["Profile.SoundColumn"]);
            Assert.Equal("Sound", PageStrings.Web("en")["Profile.SoundColumn"]);
            Assert.Equal("нет в русском", PageStrings.Web("ru")["Profile.NotInRussian"]);
        }

        /// <summary>
        /// E11 (пересмотр D29): «Как читать буквы» в шапке вывода — перед «С арабским»,
        /// на телефоне одной иконкой с подписью для диктора, — и в «О чём проект?»
        /// открывают панель профиля с раскрытыми «Настройками профиля». Фокус — на их
        /// заголовок; Escape возвращает его на кнопку панели, как у меток над выводом.
        /// </summary>
        [Fact]
        public void HowToSayTheLetters_OpensTheProfileSettings()
        {
            var app = Regex.Replace(Web("App.razor"), @"\s+", " ");
            var head = Regex.Match(app, "<div class=\"result-head\">(.*?)<div class=\"result-text\"");
            Assert.True(head.Success, "the result head is not found");
            Assert.Matches("<button type=\"button\" class=\"sounds-toggle\" title=\"@L\\[\"Sounds.ToggleTitle\"\\]\" @onclick=\"OpenSoundsAsync\">.*<span class=\"sounds-toggle-label\">@L\\[\"Sounds.Toggle\"\\]</span> </button> } .*<button type=\"button\" class=\"arabic-toggle\"", head.Groups[1].Value);
            Assert.Contains("private Func<Task>? SoundsOpener => _sounds is null || _profile is null ? null : OpenSoundsAsync;", app);
            Assert.Contains("private Task OpenSoundsAsync() => _profilePicker?.OpenSettingsAsync() ?? Task.CompletedTask;", app);

            var about = Regex.Replace(Web("Components", "AboutPicker.razor"), @"\s+", " ");
            Assert.Contains("@onclick=\"OpenSoundsAsync\">@L[\"Sounds.Toggle\"]</button>", about);

            var profile = Regex.Replace(Web("Components", "ProfilePicker.razor"), @"\s+", " ");
            Assert.Contains("public async Task OpenSettingsAsync() { _settingsOpen = true; _settingsKept = true; _focusSettings = true; await OpenAsync(); }", profile);
            Assert.Contains("@onclick=\"ToggleSettings\" @ref=\"_settingsToggle\">", profile);
            Assert.Contains("if (_focusSettings) { _focusSettings = false; await _settingsToggle.FocusAsync(); return; } await base.FocusOpenedAsync();", profile);

            // Подпись видна и на телефоне (D42), цель нажатия — 44px.
            var css = Regex.Replace(Web("wwwroot", "css", "app.css"), @"\s+", " ");
            Assert.DoesNotContain(".sounds-toggle-label { position: absolute;", css);
            Assert.Contains(".sounds-toggle::after,", css);

            Assert.Equal("Как читать буквы", PageStrings.Web("ru")["Sounds.Toggle"]);
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
            Assert.Matches(@"\.result-line \+ \.result-source\.is-open \{ margin-top: var\(--space-1\); padding-top: var\(--space-2\); border-top: 1px solid var\(--foam-100\); \}", css);
            Assert.Matches(@"\.result-source\.is-open \+ \.result-line \{ margin-top: 0; padding-top: 0; \}", css);
            Assert.Matches(@"\.result-source-text \{[^}]*line-height: var\(--leading-arabic-line\);", css);

            // Подпись видна и на телефоне (D42).
            Assert.DoesNotContain(".arabic-toggle-label { position: absolute;", css);

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
            Assert.Matches(@"class=""result-chip""[^>]*aria-label=""@L\[""Result\.ReadingChip"", ReadingName\]"" @onclick=""OpenReadingAsync""><span class=""result-chip-label"">@L\[""Result\.ReadingChipLabel""\]</span><span class=""result-chip-text"">@ReadingName</span></button>", head.Groups[1].Value);
            Assert.Matches(@"class=""result-chip""[^>]*aria-label=""@L\[""Result\.ProfileChip"", _profile\.Name\]"" @onclick=""OpenProfileAsync""><span class=""result-chip-label"">@L\[""Result\.ProfileChipLabel""\]</span><span class=""result-chip-text"">@_profile\.Name</span></button>", head.Groups[1].Value);
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
            // Строка вывода прижата вправо, как арабская над ней (D39); направление
            // текста прежнее — у строки нет dir, меняется только выравнивание.
            Assert.Matches(@"\.result-line \{[^}]*text-align: right;", css);
            Assert.Matches(@"\.result-source-text \{[^}]*text-align: right;", css);
            Assert.DoesNotMatch(@"class=""result-line""[^>]*dir=", card.Groups[1].Value);
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
        /// уходящую панель. С prefers-reduced-motion движения нет, но прозрачность
        /// меняется плавно (D30).
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
        public void PanelsAndTheOutput_ShareOneAnimation_AndReducedMotionKeepsOnlyTheFade()
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
            // D30: reduce снимает только движение — токены ходов и множитель сдвига
            // обнуляются, а animation и transition целиком не снимаются; уходящие
            // панели и плашка гаснут на месте, а не пропадают сразу.
            Assert.DoesNotContain("animation: none !important", reduced.Groups[1].Value);
            Assert.DoesNotContain("transition: none !important", reduced.Groups[1].Value);
            Assert.DoesNotContain("display: none", reduced.Groups[1].Value);
            Assert.Contains(":root { --motion-move-fast: var(--motion-instant); --motion-move: var(--motion-instant); --motion-move-slow: var(--motion-instant); --motion-travel: 0; }", reduced.Groups[1].Value);
            Assert.Contains(".picker-panel { animation-name: scrim-in; } .picker-panel.is-closing { animation-name: scrim-out; }", reduced.Groups[1].Value);
        }

        /// <summary>
        /// D30: появление панелей и карточки — не короче 0.2 с. Ходы, которые
        /// двигают, берут токены --motion-move*, сдвиг в @keyframes умножен на
        /// --motion-travel — их и обнуляет reduce. Смена текста вывода проявляется.
        /// </summary>
        [Fact]
        public void Movement_HasItsOwnTokens_AndTheOutputTextFadesIn()
        {
            var css = Regex.Replace(Regex.Replace(Web("wwwroot", "css", "app.css"), @"/\*.*?\*/", " ", RegexOptions.Singleline), @"\s+", " ");

            var duration = Regex.Match(css, @"--motion-duration: ([0-9.]+)s;");
            Assert.True(duration.Success, "the motion duration is not found");
            Assert.True(double.Parse(duration.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) >= 0.2,
                        "panels and the output card appear faster than 0.2s");
            foreach (var token in new[] { "--motion-move-fast: var(--motion-fast);", "--motion-move: var(--motion-duration);", "--motion-move-slow: var(--motion-slow);", "--motion-travel: 1;" })
                Assert.Matches(@"^ ?:root \{[^}]*" + Regex.Escape(token), css);

            Assert.DoesNotMatch(@"(?:transform|grid-template-rows|margin-(?:top|bottom|left)|width|top) var\(--motion-(?:fast|duration|slow)\)", css);
            foreach (Match frame in Regex.Matches(css, @"@keyframes [a-z-]+ \{ (?:[^{}]*\{[^}]*\} )+\}"))
                foreach (Match shift in Regex.Matches(frame.Value, @"translate[XY]?\(([^;]*)\);"))
                    Assert.Contains("var(--motion-travel)", shift.Groups[1].Value);

            Assert.Matches(@"\.result-text \{[^}]*animation: text-in var\(--motion-duration\) var\(--ease-soft\);", css);
            Assert.Contains("@keyframes text-in { from { opacity: 0.35; } }", css);
            var app = Regex.Replace(Web("App.razor"), @"\s+", " ");
            Assert.Contains("<div class=\"result-text\" @key=\"_changeKey\">", app);
        }

        /// <summary>
        /// Токены дизайна (D12): цвета и длительности заданы только в <c>:root</c>,
        /// правила ниже берут их ссылками, а у полосы браузера — цвет фона страницы.
        /// </summary>
        [Fact]
        public void ColorsAndDurations_LiveInTokens()
        {
            var css = Regex.Replace(Web("wwwroot", "css", "app.css"), @"/\*.*?\*/", " ", RegexOptions.Singleline);
            css = Regex.Replace(css, @"\s+", " ");

            var root = Regex.Match(css, @"^ ?:root \{([^}]*)\}");
            Assert.True(root.Success, "the :root tokens are not first in the file");

            var tokens = Regex.Matches(root.Groups[1].Value, @"(--[a-z0-9-]+): ([^;]+);")
                              .ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value);
            foreach (var name in new[] { "--cream", "--paper", "--ink", "--ink-muted", "--foam-700", "--error", "--error-bg", "--warn-bg", "--warn-ink", "--shadow-md", "--scrim" })
                Assert.True(tokens.ContainsKey(name), $"the theme does not set {name}");

            var rules = css.Remove(root.Index, root.Length);
            Assert.DoesNotMatch(@"#[0-9a-fA-F]{3,8}\b|rgba?\(|hsla?\(", rules);
            foreach (Match declaration in Regex.Matches(rules, @"(?:transition|animation): ([^;]*);"))
                Assert.DoesNotMatch(@"(?<![\w-])[0-9.]+m?s\b", declaration.Groups[1].Value);

            var index = Web("wwwroot", "index.html");
            Assert.Contains($"<meta name=\"theme-color\" content=\"{tokens["--cream"]}\" />", index);
        }

        /// <summary>
        /// Тёмной темы нет (D27): страница, поля ввода, полосы прокрутки и значок
        /// вкладки светлые при любой теме системы, светлые токены прежние.
        /// </summary>
        [Fact]
        public void ThePageIsAlwaysLight_ThereIsNoDarkTheme()
        {
            var rawCss = Web("wwwroot", "css", "app.css");
            var css = Regex.Replace(Regex.Replace(rawCss, @"/\*.*?\*/", " ", RegexOptions.Singleline), @"\s+", " ");
            Assert.DoesNotContain("prefers-color-scheme", rawCss);
            var root = Regex.Match(css, @"^ ?:root \{([^}]*)\}");
            Assert.True(root.Success, "the :root tokens are not first in the file");
            Assert.Contains("color-scheme: light;", root.Groups[1].Value);
            Assert.Equal(new[] { "light" },
                         Regex.Matches(css, @"color-scheme: ([^;]+);").Select(m => m.Groups[1].Value).Distinct());

            // Светлые токены остались прежними: тема не перекрашена, а только лишилась тёмной пары.
            var tokens = Regex.Matches(root.Groups[1].Value, @"(--[a-z0-9-]+): ([^;]+);")
                              .ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value);
            Assert.Equal("#fbf7ef", tokens["--cream"]);
            Assert.Equal("#ffffff", tokens["--paper"]);
            Assert.Equal("#1f2d2a", tokens["--ink"]);
            Assert.Equal("#2e7363", tokens["--foam-700"]);
            Assert.Equal("#def0e9", tokens["--foam-100"]);

            var index = Web("wwwroot", "index.html");
            Assert.DoesNotContain("prefers-color-scheme", index);
            Assert.Contains("<meta name=\"color-scheme\" content=\"light\" />", index);
            Assert.Single(Regex.Matches(index, "<meta name=\"theme-color\""));

            var svg = Web("wwwroot", "favicon.svg");
            Assert.DoesNotContain("prefers-color-scheme", svg);
            Assert.Contains($".tile {{ fill: {tokens["--foam-100"]}; }}", svg);
            Assert.Contains($".mark {{ fill: {tokens["--foam-700"]}; }}", svg);
        }

        [Fact]
        public void OnAWideScreen_ThePanelIsAColumnBesideTheFieldAndTheOutput()
        {
            var css = Regex.Replace(Web("wwwroot", "css", "app.css"), @"\s+", " ");
            var wide = Regex.Match(css, @"@media \(min-width: 64rem\) \{(.*?)\} /\* ---- Загрузка");
            Assert.True(wide.Success, "the wide-screen layout is not found");

            var rules = wide.Groups[1].Value;

            Assert.Matches(@"\.picker-panel, \.reading-panel, \.profile-panel \{[^}]*position: fixed", rules);
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
        /// D37: с 64rem открытая кнопка сбоку — часть своей колонки, как вкладка:
        /// значок на фоне колонки, мост того же фона с той же рамкой тянется до
        /// колонки и накрывает её левую рамку. Колонка во всю высоту экрана и
        /// раскрывается срезом от кнопки, уходит в неё обратно; при reduce срез
        /// открыт (--motion-travel 0) — только проявление. Подпись кнопки при
        /// открытии уходит сразу, у открытой кнопки подсказки нет. Лист D14 уже
        /// 64rem прежний.
        /// </summary>
        [Fact]
        public void OnAWideScreen_TheOpenRailButtonIsATabOfItsColumn_AndTheColumnFlowsOutOfIt()
        {
            var css = Regex.Replace(Web("wwwroot", "css", "app.css"), @"\s+", " ");
            var wide = Regex.Match(css, @"@media \(min-width: 64rem\) \{(.*?)\} /\* ---- Загрузка");
            Assert.True(wide.Success, "the wide-screen layout is not found");
            var rules = wide.Groups[1].Value;

            Assert.Matches(@"\.shell \{[^}]*--rail-left: 1\.25rem;", rules);
            Assert.Matches(@"\.side-rail \{ position: fixed;[^}]*left: var\(--rail-left\);", rules);

            var icon = Regex.Match(rules, @"\.shell\.has-panel \.side-rail \.picker-toggle\[aria-expanded=""true""\] svg \{([^}]*)\}");
            Assert.True(icon.Success, "the open tab icon rule is not found");
            foreach (var declaration in new[] { "border-right-color: transparent;", "background: var(--paper);" })
                Assert.Contains(declaration, icon.Groups[1].Value);

            var bridge = Regex.Match(rules, @"\.shell\.has-panel \.side-rail \.picker-toggle\[aria-expanded=""true""\]::before \{ top: 0;([^}]*)\}");
            Assert.True(bridge.Success, "the tab bridge rule is not found");
            foreach (var declaration in new[]
            {
                "right: calc(var(--rail-left) + 1px + var(--space-1) + 100% - var(--panel-left) - 1px);", "left: 100%;",
                "z-index: 12;", "border-block: 1px solid var(--line);", "background: var(--paper);",
            })
                Assert.Contains(declaration, bridge.Groups[1].Value);

            var column = Regex.Match(rules, @"\.side-rail \.picker-panel \{([^}]*)\}");
            Assert.True(column.Success, "the rail column rule is not found");
            foreach (var declaration in new[] { "min-height: calc(100dvh - 2.5rem);", "animation-name: column-in;" })
                Assert.Contains(declaration, column.Groups[1].Value);
            Assert.Contains(".side-rail .picker-panel.is-closing { animation-name: column-out; }", rules);
            foreach (var name in new[] { "column-in", "column-out" })
                Assert.Matches($@"@keyframes {name} \{{[^@]*clip-path: inset\(var\(--panel-clip-room\) calc\(var\(--panel-clip-room\) \+ \(100% - var\(--panel-clip-room\)\) \* var\(--motion-travel\)\)", css);

            Assert.Matches(@"(?<![\w-]) \.side-rail \.picker-toggle-text \{[^}]*transition: none;", rules);
            Assert.Contains(" .side-rail .picker-toggle[aria-expanded=\"true\"] .picker-toggle-text { opacity: 0; transition: none; }", rules);

            // Лист уже 64rem не тронут: срез только в широкой раскладке.
            Assert.DoesNotContain("animation-name: column-in", css.Replace(wide.Value, ""));
        }

        /// <summary>
        /// D32, вариант «В»: открытая колонка не сдвигает шапку, поле, вывод и подвал —
        /// отступ слева у них один, с панелью и без. С 110rem колонка помещается в
        /// свободное поле слева; уже — ложится поверх, и под ней прозрачная подложка,
        /// как у списка сур: щелчок мимо закрывает панель и возвращает фокус на кнопку.
        /// Ширина колонки прежняя (таблица графем), лист D14 уже 64rem не тронут.
        /// </summary>
        [Fact]
        public void OnAWideScreen_OpeningThePanelMovesNothing_AndWhereItCoversThePageAClickOutsideClosesIt()
        {
            var css = Regex.Replace(Web("wwwroot", "css", "app.css"), @"\s+", " ");
            var wide = Regex.Match(css, @"@media \(min-width: 64rem\) \{(.*?)\} /\* ---- Загрузка");
            Assert.True(wide.Success, "the wide-screen layout is not found");
            var rules = wide.Groups[1].Value;

            // Отступ контента — от --rail-space, и has-panel его не меняет нигде.
            Assert.Matches(@"\.shell \{ --panel-left: 4\.75rem; --panel-width: 25rem; --rail-space: 6rem;", rules);
            Assert.Matches(@"\.topbar, \.stage, \.footer \{[^}]*margin-left: max\(var\(--rail-space\), \(100% - 46rem\) / 2\);", rules);
            Assert.DoesNotMatch(@"has-panel[^{]*\{[^}]*--rail-space", css);
            Assert.DoesNotMatch(@"has-panel[^{]*(\.topbar|\.stage|\.footer)[^{]*\{", css);

            // Где колонка не помещается слева (край 4.75 + 25 = 29.75rem, поле 46rem
            // по центру — нужно не меньше ~108.6rem с полосой прокрутки) — подложка.
            // Подложка ниже колонки и капсулы кнопок: соседняя кнопка переключает панель.
            var overlay = Regex.Match(css, @"@media \(min-width: 64rem\) and \(max-width: 109\.99rem\) \{ \.shell\.has-panel \.picker-backdrop \{([^}]*)\} \.shell\.has-panel \.side-rail \.picker-backdrop \{ z-index: -1; \} \}");
            Assert.True(overlay.Success, "the overlay backdrop for a column that does not fit is not found");
            foreach (var declaration in new[] { "display: block;", "z-index: 10;", "background: transparent;", "animation: none;" })
                Assert.Contains(declaration, overlay.Groups[1].Value);

            // Подложка — в каждой колонке, и щелчок по ней закрывает её к кнопке.
            foreach (var picker in new[] { "ReadingPicker.razor", "ProfilePicker.razor", "AboutPicker.razor" })
                Assert.Contains("<div class=\"picker-backdrop @Closing.CssClass\" @onclick=\"CloseToToggleAsync\"></div>",
                                Web("Components", picker));
            var close = Web("Components", "PanelPicker.cs");
            Assert.Contains("if (e.Key == \"Escape\")", close);
            Assert.Contains("await CloseToToggleAsync();", close);
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

            // Отсчёт — от карточки поля (D36): список шириной поля, от края до края рамки.
            Assert.Contains(".surah-picker { position: static; }", rules);
            Assert.Contains(".composer { position: relative;", css);
            var dropdown = Regex.Match(rules, @"\.picker-panel\.surah-panel \{([^}]*)\}");
            Assert.True(dropdown.Success, "the surah dropdown rule is not found");
            foreach (var declaration in new[]
            {
                "position: absolute;", "top: calc(100% + var(--space-2));", "right: -1px;", "left: -1px;", "width: auto;",
                "max-height: 60dvh;", "box-shadow: var(--shadow-lg);", "--panel-shift-x: 0rem;",
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
            Assert.Contains("<ul id=\"surah-list\" class=\"picker-list surah-grid\" role=\"listbox\"", picker);
            Assert.Matches(@"<li id=""@OptionId\(surah\)"" role=""option"" aria-selected=""@\(current \? ""true"" : ""false""\)""", picker);
            Assert.Contains("SurahSearch.Filter(Surahs, _query)", picker);
            Assert.Contains("<p class=\"surah-empty\" role=\"status\">@(Found.Count == 0 ? L[\"Surah.NothingFound\"].Value : null)</p>", picker);
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
        /// D36: суры — сеткой auto-fill (столбец не уже 14rem в листе, 12rem на ширине
        /// поля), строки плотные, но не ниже цели нажатия. Строка «Ничего не найдено»
        /// стоит в списке всегда, вне сетки — во всю ширину, и диктор читает её, когда
        /// в ней появляется текст. Стрелки идут по сурам по порядку, как прежде.
        /// </summary>
        [Fact]
        public void TheSurahList_IsADenseGrid_WithAStatusLineForNothingFound()
        {
            var picker = Regex.Replace(Web("Components", "SurahPicker.razor"), @"\s+", " ");
            Assert.Contains("<ul id=\"surah-list\" class=\"picker-list surah-grid\" role=\"listbox\"", picker);
            var status = picker.IndexOf("<p class=\"surah-empty\" role=\"status\">", StringComparison.Ordinal);
            var list = picker.IndexOf("@if (Found.Count > 0) { <ul id=\"surah-list\"", StringComparison.Ordinal);
            Assert.True(status >= 0 && list > status, "the status line must stand outside the list, before it");
            Assert.Contains("_active = Math.Min(_active + 1, found.Count - 1);", picker);
            Assert.Contains("_active = Math.Max(_active - 1, 0);", picker);

            var css = Regex.Replace(Web("wwwroot", "css", "app.css"), @"\s+", " ");
            Assert.Contains(".surah-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(min(100%, 14rem), 1fr));", css);
            Assert.DoesNotContain("repeat(auto-fit", css);
            Assert.Contains(".surah-empty { margin: 0;", css);
            Assert.Contains(".surah-empty:not(:empty) { margin:", css);
            Assert.Matches(@"\.surah-panel \.picker-item \{[^}]*min-height: var\(--target-min\);", css);

            var wide = Regex.Match(css, @"@media \(min-width: 64rem\) \{(.*?)\} /\* ---- Загрузка");
            Assert.True(wide.Success, "the wide-screen layout is not found");
            Assert.Contains(".surah-grid { grid-template-columns: repeat(auto-fill, minmax(12rem, 1fr)); }", wide.Groups[1].Value);
            // Поиск липнет к верху списка и не уезжает при прокрутке.
            Assert.Matches(@"\.surah-search \{ position: sticky;", css);
        }

        /// <summary>
        /// D36: кнопки под полем и сбоку на ступень меньше на вид, а цель нажатия —
        /// прежние 44px через ::after (D25). В капсуле сбоку зазор между значками
        /// вырос, чтобы цели соседних кнопок не заходили друг на друга.
        /// </summary>
        [Fact]
        public void TheButtonsUnderTheFieldAndAtTheSide_AreAStepSmaller_TheirTargetsStay44px()
        {
            var css = Regex.Replace(Web("wwwroot", "css", "app.css"), @"\s+", " ");
            Assert.Matches(@"\.picker-toggle \{ display: inline-flex;[^}]*padding: var\(--space-1\) var\(--space-3\);[^}]*font-size: var\(--text-xs\);", css);
            Assert.Matches(@":root \{[^}]*--bar-icon-size: 1\.75rem;", css);
            Assert.Contains(".composer-bar .icon-button { width: var(--bar-icon-size); height: var(--bar-icon-size); }", css);
            Assert.Matches(@"\.side-rail \.picker-toggle svg \{ box-sizing: content-box; padding: var\(--space-2\);", css);
            Assert.Matches(@"\.picker-toggle::after,[^{]*\{ content: """"; position: absolute;[^}]*width: max\(100%, var\(--target-min\)\); height: max\(100%, var\(--target-min\)\);", css);
            Assert.Contains(".icon-button::after,", css);

            var wide = Regex.Match(css, @"@media \(min-width: 64rem\) \{(.*?)\} /\* ---- Загрузка");
            Assert.True(wide.Success, "the wide-screen layout is not found");
            Assert.Matches(@"\.side-rail \{ position: fixed;[^}]*gap: var\(--space-2\);", wide.Groups[1].Value);
        }

        /// <summary>
        /// D38: страница плотнее по вертикали. Межстрочные поля, вывода и арабской
        /// строки над ним (D18) — токены на :root, меньше прежних 1.9 / 1.75 / 1.6.
        /// Метки ступени и профиля в шапке вывода — подпись и значение в одну строку
        /// («Чтение: Полный таджвид»): метки не переносятся друг под друга, значение
        /// обрезается многоточием, подпись не сжимается (как E6), а имя кнопки для
        /// диктора начинается с той же видимой подписи.
        /// </summary>
        [Fact]
        public void ThePageIsDenser_AndTheOutputChipsSayLabelAndValueOnOneLine()
        {
            var css = Regex.Replace(Web("wwwroot", "css", "app.css"), @"\s+", " ");
            Assert.Matches(@":root \{[^}]*--leading-arabic: 1\.65;[^}]*--leading-arabic-line: 1\.45;[^}]*--leading-output: 1\.5;", css);
            Assert.Matches(@"\.composer-input \{[^}]*line-height: var\(--leading-arabic\);", css);
            Assert.Matches(@"\.result-text \{[^}]*line-height: var\(--leading-output\);", css);
            Assert.Matches(@"\.result-line \{ min-height: calc\(var\(--leading-output\) \* 1em\);", css);

            Assert.Matches(@"\.result-chips \{ display: flex; flex-wrap: nowrap;", css);
            Assert.Matches(@"\.result-chip \{ display: inline-flex;[^}]*flex: 0 1 auto;", css);
            Assert.Matches(@"\.result-chip-label \{ flex: none; white-space: nowrap; \}", css);

            foreach (var (language, reading, profile) in new[] { ("ru", "Чтение:", "Профиль:"), ("en", "Reading:", "Profile:") })
            {
                var strings = PageStrings.Web(language);
                Assert.Equal(reading, strings["Result.ReadingChipLabel"]);
                Assert.Equal(profile, strings["Result.ProfileChipLabel"]);
                Assert.StartsWith(reading + " {0}", strings["Result.ReadingChip"]);
                Assert.StartsWith(profile + " {0}", strings["Result.ProfileChip"]);
            }
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
        /// Поле ввода (D20): растёт по тексту без своей прокрутки, своей кнопки вставки
        /// у него нет (D34), а очистку можно отменить плашкой внизу страницы.
        /// </summary>
        [Fact]
        public void TheField_GrowsWithoutItsOwnScroll_HasNoPasteButton_AndClearingCanBeUndone()
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

            // Кнопки «Вставить» нет (D34) ни на странице, ни в скелете загрузки: у
            // поля с текстом крестик, у пустого — ничего; вставляют Ctrl+V и долгим
            // нажатием, как в любое поле, — обработчика paste страница не ставит.
            var shell = Web("wwwroot", "index.html");
            Assert.Matches(@"@if \(HasText\) \{ <button type=""button"" class=""icon-button""[^>]*@onclick=""ClearAsync"">.*?</button> \} </div>", app);
            foreach (var gone in new[] { "paste-button", "PasteAsync", "_canPaste", "_pasteNote", "composer-note", "Composer.Paste" })
                Assert.DoesNotContain(gone, app);
            Assert.DoesNotContain("paste-button", shell);
            Assert.DoesNotContain("paste-button", css);
            Assert.DoesNotContain("canPaste", script);
            Assert.DoesNotContain("readText", script);
            Assert.DoesNotContain("\"paste\"", script);
            Assert.DoesNotContain("@onpaste", app);

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
            // D33: слева от значка отзыва встаёт подсказка до 9rem — поля плашки шире значка с подсказкой.
            Assert.Matches(@"@media \(min-width: 56rem\) \{ \.toast-region \{[^}]*padding: 0 calc\(var\(--space-5\) \+ 2\.75rem \+ 9rem \+ var\(--space-6\) \+ var\(--space-4\)\);", css);
            Assert.Matches(@"\.feedback-label \{ position: absolute;[^}]*max-width: 9rem;", css);
        }

        /// <summary>
        /// Проверка «не робот» в отзыве переживает неудачу (H5): виджет Turnstile
        /// ставится с колбэками; ошибка, истечение и пропущенный флажок ставят его
        /// заново (remove + render, без reset); отправка ждёт исхода проверки, а не
        /// фиксированные десять секунд; когда нужен флажок, это сказано текстом в
        /// role="status".
        /// </summary>
        [Fact]
        public void FeedbackGuard_RetriesAfterAFailedOrExpiredCheck_AndSaysWhenItNeedsAHuman()
        {
            var script = Web("wwwroot", "js", "page.js");
            var button = Regex.Replace(Web("Components", "FeedbackButton.razor"), @"\s+", " ");

            foreach (var callback in new[] { "callback:", "\"error-callback\":", "\"expired-callback\":", "\"timeout-callback\":", "\"before-interactive-callback\":", "\"after-interactive-callback\":" })
                Assert.Contains(callback, script);

            Assert.DoesNotContain("turnstile?.reset(", script);
            Assert.DoesNotContain("turnstile.reset(", script);
            Assert.DoesNotContain("TokenAttempts", script);
            Assert.Matches(@"function guardRenew\(entry, failed\) \{[\s\S]*?window\.turnstile\.remove\(entry\.widget\);[\s\S]*?guardRender\(entry\);", script);
            Assert.Contains("invokeMethodAsync(\"OnGuard\", state)", script);

            Assert.Contains("[JSInvokable] public Task OnGuard(string state)", button);
            Assert.Contains("\"page.guard\", _guard, Settings.TurnstileSiteKey, PageLanguages.Current.Code, _self", button);
            Assert.Contains("return L[\"Feedback.Confirm\"];", button);
            Assert.Contains("<p class=\"feedback-note @StatusClass\" role=\"status\">@StatusText</p>", button);

            Assert.Contains("не робот", PageStrings.Web("ru")["Feedback.Confirm"]);
            Assert.Contains("robot", PageStrings.Web("en")["Feedback.Confirm"]);
        }

        /// <summary>
        /// Кнопка отзыва в углу (D33): при наведении и фокусе не меняет ширину и не
        /// раскрывается подписью — только подсвечивается; подпись показывает подсказка
        /// тем же видом, что у кнопок сбоку (D23), без title; имя для диктора — та же
        /// подпись, спрятанная через opacity.
        /// </summary>
        [Fact]
        public void FeedbackButton_HighlightsAndShowsATooltipInsteadOfExpanding()
        {
            var button = Regex.Replace(Web("Components", "FeedbackButton.razor"), @"\s+", " ");
            var css = Regex.Replace(Web("wwwroot", "css", "app.css"), @"\s+", " ");

            var toggle = Regex.Match(button, @"<button type=""button"" class=""feedback-toggle""[^>]*>(.*?)</button>");
            Assert.True(toggle.Success, "the feedback button is not found");
            Assert.DoesNotContain("title=", toggle.Value);
            Assert.DoesNotContain("aria-label", toggle.Value);
            Assert.Contains("<span class=\"feedback-label\">@L[\"Feedback.Toggle\"]</span>", toggle.Groups[1].Value);

            var wide = Regex.Match(css, @"@media \(min-width: 56rem\) \{ \.feedback \{.*?\.feedback-panel \{");
            Assert.True(wide.Success, "the wide-screen feedback block is not found");
            var rules = wide.Value;

            // Ширина постоянная, у подписи нет раскрытия по max-width.
            Assert.Matches(@"\.feedback-toggle \{[^}]*width: 2\.75rem;[^}]*height: 2\.75rem;", rules);
            Assert.DoesNotContain("max-width: 0", rules);
            Assert.DoesNotMatch(@"\.feedback-toggle\[aria-expanded=""true""\] \.feedback-label", rules);

            // Наведение и фокус — подсветка.
            Assert.Matches(@"\.feedback-toggle:hover, \.feedback-toggle:focus-visible, \.feedback-toggle\[aria-expanded=""true""\] \{[^}]*border-color: var\(--foam-300\);[^}]*background: var\(--foam-50\);[^}]*color: var\(--foam-700\);", rules);

            // Подсказка — как у кнопок сбоку: тёмная плашка рядом, скрыта через opacity,
            // появляется с задержкой при наведении и фокусе.
            Assert.Matches(@"\.feedback-label \{ position: absolute;[^}]*right: calc\(100% \+ var\(--space-3\)\);[^}]*background: var\(--ink\);[^}]*color: var\(--paper\);[^}]*opacity: 0;[^}]*pointer-events: none;", rules);
            Assert.DoesNotMatch(@"\.feedback-label \{[^}]*visibility", rules);
            Assert.Matches(@"\.feedback-toggle:hover \.feedback-label, \.feedback-toggle:focus-visible \.feedback-label \{ opacity: 1;[^}]*transition-delay: var\(--motion-hint-delay\);", rules);
        }

        /// <summary>
        /// D28: под пустым полем только кнопки; у «Выбрать суру» — подсказка тем же видом,
        /// что у значка отзыва (D33), без title. D35: в placeholder снова первый аят
        /// (арабским, справа налево, у поля всегда lang="ar"), видимого пояснения нет —
        /// диктору оно описание поля через aria-describedby на скрытый текст.
        /// </summary>
        [Fact]
        public void EmptyField_ShowsTheFirstAyah_ExplainsItselfToScreenReaders_AndTheSurahButtonHasATooltip()
        {
            var app = Regex.Replace(Web("App.razor"), @"\s+", " ");
            var picker = Regex.Replace(Web("Components", "SurahPicker.razor"), @"\s+", " ");
            var css = Regex.Replace(Web("wwwroot", "css", "app.css"), @"\s+", " ");
            var index = Web("wwwroot", "index.html");

            Assert.Contains("<p id=\"arabic-note\" class=\"visually-hidden\">@L[\"Composer.Placeholder\"]</p> <textarea id=\"arabic\" class=\"composer-input\" dir=\"rtl\" lang=\"ar\"", app);
            Assert.Contains("placeholder=\"@_placeholder\" aria-describedby=\"arabic-note\"", app);
            Assert.DoesNotContain("placeholder=\"@L[", app);
            Assert.Contains("private const string FallbackPlaceholder = \"بِسْمِ ٱللَّهِ ٱلرَّحْمَٰنِ ٱلرَّحِيمِ\";", app);
            Assert.Contains("_placeholder = _surahs.FirstOrDefault(s => s.Number == 1)?.Ayahs.FirstOrDefault()?.Arabic ?? FallbackPlaceholder;", app);
            Assert.DoesNotContain("QuickStart", app);
            Assert.DoesNotContain("quick-start", index);
            Assert.DoesNotContain("boot-chip", css);
            var placeholder = Regex.Match(css, @"\.composer-input::placeholder \{[^}]*\}");
            Assert.True(placeholder.Success, "the placeholder rule is not found");
            Assert.DoesNotContain("font-family", placeholder.Value);
            Assert.DoesNotContain("text-align", placeholder.Value);

            var toggle = Regex.Match(picker, @"<button type=""button"" class=""picker-toggle surah-toggle""[^>]*>.*?</button> <span id=""surah-tip"" class=""surah-tip"" role=""tooltip"">@L\[""Surah.Tip""\]</span>");
            Assert.True(toggle.Success, "the surah button with its tooltip is not found");
            Assert.Contains("aria-describedby=\"surah-tip\"", toggle.Value);
            Assert.DoesNotContain("title=", toggle.Value);

            Assert.Matches(@"\.surah-tip \{ position: absolute;[^}]*background: var\(--ink\);[^}]*color: var\(--paper\);[^}]*opacity: 0;[^}]*pointer-events: none;", css);
            Assert.DoesNotMatch(@"\.surah-tip \{[^}]*visibility", css);
            Assert.Matches(@"\.surah-toggle:hover \+ \.surah-tip, \.surah-toggle:focus-visible \+ \.surah-tip \{ opacity: 1;[^}]*transition-delay: var\(--motion-hint-delay\);", css);
            Assert.Matches(@"\.surah-toggle\[aria-expanded=""true""\] \+ \.surah-tip \{ opacity: 0;", css);
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
            Assert.Matches(@"if \(diff\.HasChanges\) \{ _changes = diff; _ = ExpireChangesAsync\(\+\+_changeKey\); AnnounceOutput\(\); \} else if \(before == _result\) \{ _ = ShowUnchangedNoteAsync\(\); \}", app);

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
            Assert.Single(Regex.Matches(css, @"--highlight: #[0-9a-f]{6};"));
            Assert.Contains("--motion-highlight: 1.2s;", css);

            // Без движения — ровный фон столько же и снимается разом.
            Assert.Matches(@"@media \(prefers-reduced-motion: reduce\) \{.*mark\.changed \{ animation: change-hold var\(--motion-highlight\) steps\(1\) !important; \}", css);
            Assert.Matches(@"@keyframes change-hold \{ from, to \{ background-color: var\(--highlight\);", css);
        }
            [Fact]
        public void Targets_AreAtLeast44Px_AndTheOutputUpdateIsAnnounced()
        {
            // D25: мелкие кнопки видны прежнего размера, а нажатие ловят в квадрате
            // не меньше 44px — прозрачный ::after по центру кнопки.
            var css = Regex.Replace(Web("wwwroot", "css", "app.css"), @"\s+", " ");
            Assert.Matches(@":root \{[^}]*--target-min: 2\.75rem;", css);
            var area = Regex.Match(css, @"((?:\.[a-z-]+(?: a)?::after, )+\.[a-z-]+::after) \{ content: """"; position: absolute; top: 50%; left: 50%; width: max\(100%, var\(--target-min\)\); height: max\(100%, var\(--target-min\)\); transform: translate\(-50%, -50%\); \}");
            Assert.True(area.Success, "the enlarged tap area is not found");
            var relative = Regex.Match(css, @"((?:\.[a-z-]+(?: a)?, )+\.[a-z-]+(?: a)?) \{ position: relative; \} \.language-option::after,");
            Assert.True(relative.Success, "the anchor of the tap area is not found");
            foreach (var target in new[] { ".language-option", ".reading-rule-toggle", ".reading-count", ".icon-button", ".result-chip", ".arabic-toggle", ".copy-button", ".reading-note-more", ".footer a", ".feedback-toggle", ".toast-action", ".picker-toggle" })
            {
                Assert.Contains(target + "::after", area.Groups[1].Value);
                Assert.Contains(target + ",", relative.Groups[1].Value + ",");
            }

            // Обрезающий overflow — не у самой метки, а у её подписи: иначе он срезал бы область.
            Assert.DoesNotMatch(@"\.result-chip \{[^}]*overflow: hidden", css);
            Assert.Matches(@"\.result-chip-text \{ display: block; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; \}", css);

            // Рамка фокуса — токеном акцента: пена-300 на светлом давала 1.5:1.
            Assert.Matches(@":root \{[^}]*--focus-ring: var\(--foam-700\);", css);
            Assert.Contains(":focus-visible { outline: 3px solid var(--focus-ring);", css);

            // Подписи кнопок под полем не переносятся (D25). Сжимать «Вставить» до
            // значка больше нечего (D34): page.fitBar и .is-compact убраны.
            Assert.Matches(@"\.composer-bar \.picker-toggle, \.composer-bar \.composer-tools \{ flex: none; white-space: nowrap; \}", css);
            Assert.DoesNotContain("is-compact", css);
            var app = Regex.Replace(Web("App.razor"), @"\s+", " ");
            Assert.Contains("<div class=\"composer-bar\">", app);
            Assert.DoesNotContain("page.fitBar", app);
            var script = Web("wwwroot", "js", "page.js");
            Assert.DoesNotContain("fitBar", script);

            // «Вывод обновлён» — своя вежливая живая область, всегда в разметке, через
            // секунду после последней правки, а не на каждый символ.
            Assert.Contains("<p class=\"visually-hidden\" role=\"status\">@_outputNote</p>", app);
            Assert.Contains("private static readonly TimeSpan OutputNoteDelay = TimeSpan.FromSeconds(1);", app);
            Assert.Matches(@"private void AnnounceOutput\(\) \{ _outputNote = null; _ = ShowOutputNoteAsync\(\+\+_outputNoteStamp\); \}", app);
            Assert.Matches(@"await Task\.Delay\(OutputNoteDelay\);[^}]*if \(stamp != _outputNoteStamp", app);
            Assert.Matches(@"private void OnInput\(ChangeEventArgs e\) \{[^}]*Calculate\(\); AnnounceOutput\(\);", app);
            Assert.Single(Regex.Matches(app, @"_outputNote</p>"));
            Assert.Equal("Вывод обновлён", PageStrings.Web("ru")["Output.Updated"]);
            Assert.Equal("Output updated", PageStrings.Web("en")["Output.Updated"]);

            // Арабские буквы в русском описании правила — с lang="ar": диктор читает их арабским голосом.
            Assert.Contains("<span class=\"reading-rule-text\"><MixedText Text=\"@rule.Description.Current\" /></span>", Web("Components", "ReadingPicker.razor"));
            Assert.Contains("{<span lang=\"ar\" dir=\"rtl\">@text</span>}", Web("Components", "MixedText.razor"));

            // Лист на телефоне держит Tab в себе: за подложкой кнопок не видно.
            Assert.Contains("const sheet = document.querySelector(\".picker-panel:not(.is-closing)\");", script);
            // Лист модален только листом: aria-modal ставится уже 64rem и снимается в колонке.
            Assert.Contains("panel.setAttribute(\"aria-modal\", \"true\");", script);
            Assert.Contains("requestAnimationFrame(markModal);", script);

            // Поле в фокусе — рамкой карточки цвета --focus-ring; Tab не прячет цель под липкими шапками.
            Assert.Matches(@"\.composer:focus-within \{ border-color: var\(--focus-ring\);", css);
            Assert.Contains("scroll-padding-top: calc(var(--panel-head-height) + var(--target-min));", css);
        }

        /// <summary>
        /// D42: уже 64rem в шапке вывода нет меток «Чтение» и «Профиль» — их повторяют
        /// кнопки под полем, — а кнопки шапки на телефоне подписаны и стоят своей
        /// строкой под подписью и «Копировать». С 64rem обёртка кнопок вёрстке не видна.
        /// </summary>
        [Fact]
        public void ResultHead_OnThePhone_HasLabelledButtonsInsteadOfChips()
        {
            var app = Regex.Replace(Web("App.razor"), @"\s+", " ");
            var css = Regex.Replace(Web("wwwroot", "css", "app.css"), @"\s+", " ");

            Assert.Matches(@"<div class=""result-tools""> .*class=""sounds-toggle"".*class=""arabic-toggle"".*</button> </div> .*<button type=""button"" class=""copy-button", app);

            Assert.Contains(".result-tools { display: contents; }", css);
            Assert.Contains(".result-tools + .copy-button { margin-left: 0; }", css);
            Assert.Contains("@media (max-width: 63.99rem) { .result-meta { display: none; } }", css);
            Assert.Matches(@"@media \(max-width: 36rem\) \{.*\.result-tools \{ display: flex; flex-wrap: wrap; order: 2; flex: 1 1 100%;", css);
            Assert.Matches(@"@media \(max-width: 36rem\) \{.*\.result-tools \+ \.copy-button \{ margin-left: auto; \}", css);
            Assert.DoesNotContain("-toggle-label { position: absolute;", css);
        }
    }
}
