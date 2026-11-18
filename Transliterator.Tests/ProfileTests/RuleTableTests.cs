using Transliterator.Core.Services;
using Transliterator.Core.Services.Phonology;
using Xunit;

namespace Transliterator.Tests.ProfileTests
{
    /// <summary>
    /// Правила профиля таблицей (D4): варианты под своей буквой, пустое отличимо
    /// от отсутствующего, поиск находит букву вместе с вариантами.
    /// </summary>
    public class RuleTableTests
    {
        public static TheoryData<string> Profiles()
        {
            var data = new TheoryData<string>();
            foreach (var profile in TestProfiles.All)
                data.Add(profile.Name);
            return data;
        }

        [Theory]
        [MemberData(nameof(Profiles))]
        public void EveryRule_AppearsExactlyOnce(string name)
        {
            // Таблица — тот же профиль, а не выборка из него: строка, потерянная
            // группировкой, выглядела бы в просмотре как незаданный ключ.
            var profile = TestProfiles.All.Single(p => p.Name == name);
            var groups = RuleTable.Group(profile.Rules);

            var keys = groups.Where(g => g.BaseValue is not null).Select(g => g.BaseKey)
                             .Concat(groups.SelectMany(g => g.Variants).Select(v => v.Key))
                             .ToList();

            Assert.Equal(profile.Rules.Count, keys.Count);
            Assert.Equal(profile.Rules.Keys.OrderBy(k => k, StringComparer.Ordinal),
                         keys.OrderBy(k => k, StringComparer.Ordinal));
        }

        [Fact]
        public void Variants_StandUnderTheirLetter_InProfileOrder()
        {
            var hamza = RuleTable.Group(TestProfiles.Cyrillic.Rules).Single(g => g.BaseKey == "ء");

            Assert.Equal("-", hamza.BaseValue);
            Assert.Equal(new[] { "initial", "hiatus", "final" }, hamza.Variants.Select(v => v.Name));
            Assert.Equal(new[] { "ء|initial", "ء|hiatus", "ء|final" }, hamza.Variants.Select(v => v.Key));
        }

        [Fact]
        public void Groups_KeepTheOrderOfTheProfile()
        {
            var groups = RuleTable.Group(TestProfiles.Cyrillic.Rules).Select(g => g.BaseKey).ToList();

            Assert.Equal("ء", groups[0]);
            Assert.True(groups.IndexOf("ب") < groups.IndexOf("ي"));
            Assert.True(groups.IndexOf("ي") < groups.IndexOf("َ"));
        }

        [Fact]
        public void EmptyValue_IsEmpty_NotMissing()
        {
            // Начальная хамза и отзвук кальканя в Cyrillic заданы пустыми нарочно.
            // Пустая строка — решение профиля, null — ключа нет вовсе.
            var rules = new Dictionary<string, string>
            {
                ["ب"] = "б",
                ["ب|qalqalah"] = "",
                ["ر|heavy"] = "р",
            };

            var groups = RuleTable.Group(rules);
            var ba = groups.Single(g => g.BaseKey == "ب");
            var ra = groups.Single(g => g.BaseKey == "ر");

            Assert.Equal(string.Empty, ba.Variants.Single().Value);
            Assert.Null(ra.BaseValue);
            Assert.Equal("heavy", ra.Variants.Single().Name);
        }

        [Fact]
        public void Search_ByLetter_FindsTheLetterWithItsVariants()
        {
            var found = RuleTable.Filter(RuleTable.Group(TestProfiles.Cyrillic.Rules), "ة");

            var group = Assert.Single(found);
            Assert.Equal("ة", group.BaseKey);
            Assert.Equal("waqf", Assert.Single(group.Variants).Name);
        }

        [Theory]
        [InlineData("heavy")]
        [InlineData("HEAVY")]
        [InlineData(" |heavy ")]
        public void Search_ByVariant_FindsWholeGroups(string query)
        {
            var found = RuleTable.Filter(RuleTable.Group(TestProfiles.Cyrillic.Rules), query);

            var fatha = Assert.Single(found);
            Assert.Equal("َ", fatha.BaseKey);
            Assert.Equal("а", fatha.BaseValue);
            Assert.Equal(new[] { "heavy", "soft" }, fatha.Variants.Select(v => v.Name));
        }

        [Fact]
        public void Search_ByFinal_FindsHamzaWithEveryPosition()
        {
            // Хамза на конце слова (G2) — вариант той же буквы: поиск показывает
            // её целиком, со всеми позициями, и правка одной видна рядом с прочими.
            var hamza = Assert.Single(RuleTable.Filter(RuleTable.Group(TestProfiles.Cyrillic.Rules), "final"));

            Assert.Equal("ء", hamza.BaseKey);
            Assert.Equal(new[] { "initial", "hiatus", "final" }, hamza.Variants.Select(v => v.Name));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void EmptySearch_ShowsEverything(string? query)
        {
            var groups = RuleTable.Group(TestProfiles.Cyrillic.Rules);

            Assert.Same(groups, RuleTable.Filter(groups, query));
        }

        [Theory]
        [InlineData("х")]
        [InlineData("Х")]
        public void Search_ByGrapheme_FindsLettersWrittenThatWay(string query)
        {
            // Тот, кто не знает арабского, ищет букву по тому, как она пишется (E7).
            var found = RuleTable.Filter(RuleTable.Group(TestProfiles.Cyrillic.Rules), query);

            Assert.Equal(new[] { "ح", "خ" }, found.Select(g => g.BaseKey).Order());
        }

        [Fact]
        public void ShortQuery_DoesNotSearchTheNotes()
        {
            // «х» — это графема, а не начало слова «хамза» в пояснении к hiatus.
            var found = RuleTable.Filter(RuleTable.Group(TestProfiles.Cyrillic.Rules), "х");

            Assert.DoesNotContain(found, g => g.BaseKey == "ء");
        }

        [Fact]
        public void Search_ByGrapheme_LooksIntoVariantsToo()
        {
            // Графема, которой нет ни у одной базы, а есть только у варианта:
            // поиск находит букву и по ней.
            var groups = RuleTable.Group(TestProfiles.Cyrillic.Rules);
            var variant = groups.SelectMany(g => g.Variants)
                                .First(v => v.Value.Length > 0
                                         && !groups.Any(g => g.BaseValue?.Contains(v.Value, StringComparison.OrdinalIgnoreCase) == true));

            var found = RuleTable.Filter(groups, variant.Value);

            Assert.Contains(found, g => g.Variants.Contains(variant));
        }

        [Theory]
        [InlineData("пауз", "waqf")]
        [InlineData("эмфаз", "heavy")]
        [InlineData("в начале слова", "initial")]
        [InlineData("на конце слова", "final")]        // G2
        public void Search_ByRussianNote_FindsTheVariant(string query, string variant)
        {
            var found = RuleTable.Filter(RuleTable.Group(TestProfiles.Cyrillic.Rules), query);

            Assert.NotEmpty(found);
            Assert.All(found, group => Assert.Contains(group.Variants, v => v.Name == variant));
        }

        [Theory]
        [MemberData(nameof(Profiles))]
        public void EveryVariantOfABuiltInProfile_IsExplainedInRussian(string name)
        {
            // Панель профиля показывает рядом с «initial» и «hiatus» пояснение:
            // вариант без него человеку без английского ничего не скажет (E7).
            var profile = TestProfiles.All.Single(p => p.Name == name);
            var variants = RuleTable.Group(profile.Rules).SelectMany(g => g.Variants);

            Assert.All(variants, variant => Assert.False(string.IsNullOrWhiteSpace(variant.Note), variant.Key));
        }

        [Fact]
        public void EveryVariantTheRendererKnows_IsExplainedInRussian()
        {
            var known = typeof(CyrillicRenderer).GetFields()
                                                .Where(f => f.IsLiteral && f.Name.EndsWith("Variant"))
                                                .Select(f => (string)f.GetRawConstantValue()!);

            Assert.Equal(known.Order(), RuleTable.VariantNotes.Keys.Order());
        }

        [Theory]
        [InlineData("pause", "waqf")]
        [InlineData("emphatic", "heavy")]
        [InlineData("start of a word", "initial")]
        public void Search_InTheEnglishInterface_FindsTheVariantByItsEnglishNote(string query, string variant)
        {
            // Поиск — на языке интерфейса (D7): в английском пояснения английские.
            using var _ = TestCulture.Use("en");

            var found = RuleTable.Filter(RuleTable.Group(TestProfiles.Latin.Rules), query);

            Assert.NotEmpty(found);
            Assert.All(found, group => Assert.Contains(group.Variants, v => v.Name == variant));
        }

        [Fact]
        public void Search_InTheEnglishInterface_StillFindsACyrillicGrapheme()
        {
            // Графема своего профиля — буква, а не перевод: «х» ищется в любом интерфейсе.
            using var _ = TestCulture.Use("en");

            var found = RuleTable.Filter(RuleTable.Group(TestProfiles.Cyrillic.Rules), "х");

            Assert.Equal(new[] { "ح", "خ" }, found.Select(g => g.BaseKey).Order());
        }

        [Fact]
        public void EveryVariantNote_ExistsInEnglish()
        {
            using var _ = TestCulture.Use("en");

            Assert.All(RuleTable.VariantNotes, pair =>
                Assert.Matches("^[a-z][a-z ,]+$", pair.Value));
        }

        [Fact]
        public void Search_FindsALetter_ByHowItSounds()
        {
            // Звучание буквы (E11) — в той же таблице, и поиск находит букву по нему.
            static string? Describe(string letter) => letter == "ع" ? "Айн: звонкий звук глубоко в горле" : null;

            var groups = RuleTable.Group(TestProfiles.Cyrillic.Rules);

            Assert.Equal("ع", Assert.Single(RuleTable.Filter(groups, "горле", Describe)).BaseKey);
            Assert.DoesNotContain(RuleTable.Filter(groups, "горле"), g => g.BaseKey == "ع");
        }

        [Fact]
        public void Search_ByHowItSounds_NeedsThreeCharacters()
        {
            // Как у пояснения варианта: одна-две буквы находили бы в описаниях что угодно.
            static string? Describe(string letter) => letter == "ع" ? "Айн: звонкий звук глубоко в горле" : null;

            var found = RuleTable.Filter(RuleTable.Group(TestProfiles.Cyrillic.Rules), "зв", Describe);

            Assert.DoesNotContain(found, g => g.BaseKey == "ع");
        }

        [Fact]
        public void SearchWithoutMatches_IsEmpty()
        {
            Assert.Empty(RuleTable.Filter(RuleTable.Group(TestProfiles.Cyrillic.Rules), "nosuchkey"));
        }
    }
}
