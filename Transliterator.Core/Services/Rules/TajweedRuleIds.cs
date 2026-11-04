namespace Transliterator.Core.Services.Rules
{
    /// <summary>
    /// Идентификаторы правил каталога (<c>TajweedRules.json</c>), по которым
    /// конвейер читает выбранные счёты из <see cref="Transliterator.Domain.Phonology.ReadingOptions"/>.
    /// Тест сверяет их с каталогом: правило с вариантами без константы здесь
    /// или константа без правила роняют тест, а не выбор на странице.
    /// </summary>
    public static class TajweedRuleIds
    {
        public const string MaddArid = "madd-arid";
        public const string MaddLin = "madd-lin";
        public const string MaddMuttasil = "madd-muttasil";
        public const string MaddMunfasil = "madd-munfasil";

        /// <summary>Все правила, где конвейер читает выбранный счёт.</summary>
        public static IReadOnlyList<string> WithCounts { get; } =
            new[] { MaddArid, MaddLin, MaddMuttasil, MaddMunfasil };
    }
}
