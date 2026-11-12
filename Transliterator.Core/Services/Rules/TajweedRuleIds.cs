namespace Transliterator.Core.Services.Rules
{
    /// <summary>
    /// Идентификаторы правил каталога (<c>TajweedRules.json</c>), по которым
    /// конвейер читает выбранные счёты и выключенные правила из
    /// <see cref="Transliterator.Domain.Phonology.ReadingOptions"/>.
    /// Тест сверяет их с каталогом: правило с вариантами без константы здесь
    /// или константа без правила роняют тест, а не выбор на странице.
    /// </summary>
    public static class TajweedRuleIds
    {
        public const string MaddArid = "madd-arid";
        public const string MaddLin = "madd-lin";
        public const string MaddMuttasil = "madd-muttasil";
        public const string MaddMunfasil = "madd-munfasil";

        public const string IdghamBiGhunna = "idgham-bi-ghunna";
        public const string Iqlab = "iqlab";
        public const string Ikhfa = "ikhfa";
        public const string MaddSila = "madd-sila";
        public const string QalqalahSughra = "qalqalah-sughra";
        public const string QalqalahKubra = "qalqalah-kubra";

        /// <summary>Все правила, где конвейер читает выбранный счёт.</summary>
        public static IReadOnlyList<string> WithCounts { get; } =
            new[] { MaddArid, MaddLin, MaddMuttasil, MaddMunfasil };

        /// <summary>
        /// Все правила, которые конвейер умеет выключить по одному (I4). Сверяются
        /// с правилами каталога, помеченными <c>Switchable</c>.
        /// </summary>
        public static IReadOnlyList<string> Switchable { get; } =
            new[] { IdghamBiGhunna, Iqlab, Ikhfa, MaddSila, QalqalahSughra, QalqalahKubra };
    }
}
