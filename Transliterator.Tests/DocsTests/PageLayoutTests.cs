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
            Assert.DoesNotContain("<SurahPicker", side);

            Assert.Single(Regex.Matches(app, "<SurahPicker"));
            Assert.Single(Regex.Matches(app, "<ReadingPicker"));
            Assert.Single(Regex.Matches(app, "<ProfilePicker"));
        }

        [Theory]
        [InlineData("SurahPicker.razor", "surah-menu")]
        [InlineData("ReadingPicker.razor", "reading-menu")]
        [InlineData("ProfilePicker.razor", "profile-menu")]
        public void AllThreePanels_BehaveAlike(string file, string panel)
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
            Assert.Contains("Reading.GroupOff", toggle.Groups[2].Value);
            Assert.Contains("<div id=\"@blockId\" class=\"reading-block-body\" hidden=\"@(!open)\">", reading);

            Assert.Matches(@"protected override Task BeforeOpenAsync\(\) \{ _openBlocks\.Clear\(\);", reading);
            Assert.Matches(@"if \(mode == ReadingMode\.Variants\) _openBlocks\.Add\(TajweedTier\.Tajweed\);", reading);

            var russian = PageStrings.Web("ru");
            Assert.Equal("Правила чтения на арабском", russian["Group.Base"]);
            Assert.Equal("Правила таджвида", russian["Group.Tajweed"]);

            var english = PageStrings.Web("en");
            Assert.Equal("Rules of reading Arabic", english["Group.Base"]);
            Assert.Equal("Tajweed rules", english["Group.Tajweed"]);
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
            // в панели профиля (E4): вывод и так рядом.
            Assert.Matches(@"\.picker-backdrop, \.profile-preview \{ display: none; \}", rules);
        }
    }
}
