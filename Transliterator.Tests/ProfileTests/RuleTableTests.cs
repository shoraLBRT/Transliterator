using Transliterator.Core.Services;
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
            var hamza = RuleTable.Group(TestProfiles.Standard.Rules).Single(g => g.BaseKey == "ء");

            Assert.Equal("ъ", hamza.BaseValue);
            Assert.Equal(new[] { "initial", "hiatus" }, hamza.Variants.Select(v => v.Name));
            Assert.Equal(new[] { "ء|initial", "ء|hiatus" }, hamza.Variants.Select(v => v.Key));
        }

        [Fact]
        public void Groups_KeepTheOrderOfTheProfile()
        {
            var groups = RuleTable.Group(TestProfiles.Standard.Rules).Select(g => g.BaseKey).ToList();

            Assert.Equal("ء", groups[0]);
            Assert.True(groups.IndexOf("ب") < groups.IndexOf("ي"));
            Assert.True(groups.IndexOf("ي") < groups.IndexOf("َ"));
        }

        [Fact]
        public void EmptyValue_IsEmpty_NotMissing()
        {
            // Начальная хамза и отзвук кальканя в Standard заданы пустыми нарочно.
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
            var found = RuleTable.Filter(RuleTable.Group(TestProfiles.Standard.Rules), "ة");

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
            var found = RuleTable.Filter(RuleTable.Group(TestProfiles.Standard.Rules), query);

            var fatha = Assert.Single(found);
            Assert.Equal("َ", fatha.BaseKey);
            Assert.Equal("а", fatha.BaseValue);
            Assert.Equal(new[] { "heavy", "soft" }, fatha.Variants.Select(v => v.Name));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void EmptySearch_ShowsEverything(string? query)
        {
            var groups = RuleTable.Group(TestProfiles.Standard.Rules);

            Assert.Same(groups, RuleTable.Filter(groups, query));
        }

        [Fact]
        public void SearchWithoutMatches_IsEmpty()
        {
            Assert.Empty(RuleTable.Filter(RuleTable.Group(TestProfiles.Standard.Rules), "nosuchkey"));
        }
    }
}
