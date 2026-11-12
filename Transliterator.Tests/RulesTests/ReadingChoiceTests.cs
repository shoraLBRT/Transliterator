using Transliterator.Core.Repositories;
using Transliterator.Core.Services;
using Transliterator.Core.Services.Rules;
using Transliterator.Domain.Phonology;
using Xunit;

namespace Transliterator.Tests.RulesTests
{
    /// <summary>
    /// Выбор чтения на странице (I2): что хранится в браузере и что из этого
    /// получает конвейер. Хранилище переживает выпуски страницы, поэтому
    /// сохранённое сверяется с каталогом, а не берётся на веру.
    /// </summary>
    public class ReadingChoiceTests
    {
        private static readonly Transliterator.Domain.Entities.TajweedRuleCatalog Catalog =
            EmbeddedTajweedRuleCatalog.Catalog;

        [Fact]
        public void Default_IsFullTajweed() =>
            Assert.Same(ReadingOptions.FullTajweed, ReadingChoice.Default.ToOptions());

        [Fact]
        public void SavedChoice_ComesBack()
        {
            var choice = ReadingChoice.Default.WithMode(ReadingMode.Variants)
                                      .WithCount(TajweedRuleIds.MaddArid, 6)
                                      .WithCount(TajweedRuleIds.MaddMunfasil, 5);

            var restored = ReadingChoice.Parse(choice.Serialize(), Catalog);

            Assert.Equal(ReadingMode.Variants, restored.Mode);
            Assert.Equal(6, restored.Counts[TajweedRuleIds.MaddArid]);
            Assert.Equal(5, restored.Counts[TajweedRuleIds.MaddMunfasil]);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("не json")]
        [InlineData("null")]
        [InlineData("{\"mode\":\"loud\"}")]
        [InlineData("{\"mode\":\"7\"}")]
        public void BrokenOrUnknown_GivesFullTajweed(string? stored) =>
            Assert.Equal(ReadingMode.Full, ReadingChoice.Parse(stored, Catalog).Mode);

        [Fact]
        public void CountsOutsideTheCatalog_AreDropped()
        {
            // Счёт, которого правило не допускает, и правило, которого нет в каталоге,
            // отбрасываются: правило берёт свой счёт по умолчанию.
            var restored = ReadingChoice.Parse(
                "{\"mode\":\"variants\",\"counts\":{\"madd-arid\":3,\"madd-lin\":4,\"gone\":2}}", Catalog);

            Assert.Equal(new[] { TajweedRuleIds.MaddLin }, restored.Counts.Keys);
            Assert.Equal(2, restored.CountOf(Catalog.Rules.Single(r => r.Id == TajweedRuleIds.MaddArid)));
        }

        [Fact]
        public void Counts_ApplyOnlyInVariants()
        {
            // Полный таджвид — это счёты по умолчанию, обычное чтение своих вариантов
            // не имеет. Выбор при этом помнится: вернулся к вариантам — он на месте.
            var choice = ReadingChoice.Default.WithCount(TajweedRuleIds.MaddArid, 6);

            Assert.Same(ReadingOptions.FullTajweed, choice.ToOptions());
            Assert.False(choice.WithMode(ReadingMode.Plain).ToOptions().Tajweed);
            Assert.Equal(6, choice.WithMode(ReadingMode.Variants).ToOptions().Count(TajweedRuleIds.MaddArid, 2));
        }

        // ---- Правила, выключенные по одному (I4) ----

        [Fact]
        public void SwitchedOffRules_ComeBack_WithTheTier()
        {
            // Выбор хранится в браузере вместе со ступенью — одной записью.
            var choice = ReadingChoice.Default.WithMode(ReadingMode.Variants)
                                      .WithCount(TajweedRuleIds.MaddArid, 4)
                                      .WithRule(TajweedRuleIds.QalqalahSughra, on: false)
                                      .WithRule(TajweedRuleIds.Ikhfa, on: false);

            var restored = ReadingChoice.Parse(choice.Serialize(), Catalog);

            Assert.Equal(ReadingMode.Variants, restored.Mode);
            Assert.Equal(4, restored.Counts[TajweedRuleIds.MaddArid]);
            Assert.Equal(new[] { TajweedRuleIds.Ikhfa, TajweedRuleIds.QalqalahSughra }.Order(), restored.Off.Order());
            Assert.Equal(choice.Serialize(), restored.Serialize());
        }

        [Fact]
        public void SwitchedBackOn_IsRead() =>
            Assert.Empty(ReadingChoice.Default.WithRule(TajweedRuleIds.MaddSila, on: false)
                                      .WithRule(TajweedRuleIds.MaddSila, on: true).Off);

        [Fact]
        public void Serialize_DoesNotDependOnTheOrderOfSwitching()
        {
            // По строке страница узнаёт, изменился ли выбор: один выбор — одна строка.
            var one = ReadingChoice.Default.WithRule(TajweedRuleIds.Iqlab, false).WithRule(TajweedRuleIds.Ikhfa, false);
            var other = ReadingChoice.Default.WithRule(TajweedRuleIds.Ikhfa, false).WithRule(TajweedRuleIds.Iqlab, false);

            Assert.Equal(one.Serialize(), other.Serialize());
        }

        [Fact]
        public void SavedBeforeTheSwitches_ReadsEveryRule()
        {
            // Запись, сделанная до I4, переключателей не знает: все правила читаются.
            var restored = ReadingChoice.Parse("{\"mode\":\"variants\",\"counts\":{\"madd-arid\":4}}", Catalog);

            Assert.Equal(ReadingMode.Variants, restored.Mode);
            Assert.Empty(restored.Off);
        }

        [Fact]
        public void RulesThatCannotBeSwitchedOff_AreDropped()
        {
            // Вакф и идгам без гунны по одному не выключаются (решение владельца),
            // а правила, которого нет в каталоге, нет и на странице.
            var restored = ReadingChoice.Parse(
                "{\"mode\":\"variants\",\"off\":[\"waqf\",\"idgham-bila-ghunna\",\"gone\",\"iqlab\"]}", Catalog);

            Assert.Equal(new[] { TajweedRuleIds.Iqlab }, restored.Off);
        }

        [Fact]
        public void SwitchedOffRules_ApplyOnlyInVariants()
        {
            // Полный таджвид — все правила; обычное чтение их не читает и так.
            // Выбор при этом помнится: вернулся к вариантам — он на месте.
            var choice = ReadingChoice.Default.WithRule(TajweedRuleIds.MaddSila, on: false);

            Assert.Same(ReadingOptions.FullTajweed, choice.ToOptions());
            Assert.Same(ReadingOptions.Plain, choice.WithMode(ReadingMode.Plain).ToOptions());
            Assert.False(choice.WithMode(ReadingMode.Variants).ToOptions().Reads(TajweedRuleIds.MaddSila));
            Assert.Equal("иннаhу каан", TransliterationPipeline.Transliterate("إِنَّهُۥ كَانَ",
                reading: choice.WithMode(ReadingMode.Variants).ToOptions()));
        }

        [Fact]
        public void Variants_ChangeTheOutput() =>
            Assert.Equal("ар-рохIииииим", TransliterationPipeline.Transliterate("ٱلرَّحِيمِ", reading:
                ReadingChoice.Default.WithMode(ReadingMode.Variants).WithCount(TajweedRuleIds.MaddArid, 6).ToOptions()));
    }
}
