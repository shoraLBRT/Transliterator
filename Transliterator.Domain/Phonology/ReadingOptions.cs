namespace Transliterator.Domain.Phonology
{
    /// <summary>
    /// Как читать текст: с таджвидом или без, и какие счёты выбраны там, где
    /// таджвид сам даёт выбор (I2). Ступени решены владельцем 2026-11-04:
    /// полный таджвид, варианты внутри таджвида, обычное чтение без таджвида.
    /// <para>
    /// Какое правило к какой ступени относится и какие счёты у него допустимы,
    /// записано в каталоге правил (<c>TajweedRules.json</c>). Здесь — только выбор
    /// человека: ступень, счёты и выключенные правила по идентификатору из каталога.
    /// </para>
    /// </summary>
    public sealed class ReadingOptions
    {
        /// <summary>Полный таджвид со счётами по умолчанию — чтение, которое конвейер давал всегда.</summary>
        public static ReadingOptions FullTajweed { get; } = new();

        /// <summary>Обычное арабское чтение: правил ступени «таджвид» нет.</summary>
        public static ReadingOptions Plain { get; } = new() { Tajweed = false };

        /// <summary>
        /// Читаются ли правила, которые работают только в чтении Корана: идгам
        /// разных букв, икляб, ихфа, удлинённые мадды, силя и кальканя. Без них
        /// остаётся то, что есть в любом чтении.
        /// </summary>
        public bool Tajweed { get; init; } = true;

        /// <summary>
        /// Выбранные счёты в харакатах по идентификатору правила каталога
        /// (<c>madd-arid</c> → 4). Правило, которого здесь нет, берёт свой счёт
        /// по умолчанию.
        /// </summary>
        public IReadOnlyDictionary<string, int> Counts { get; init; } = new Dictionary<string, int>();

        /// <summary>Выбранный счёт правила или <paramref name="fallback"/>, если выбора не было.</summary>
        public int Count(string ruleId, int fallback) =>
            Counts.TryGetValue(ruleId, out var count) ? count : fallback;

        /// <summary>
        /// Правила, выключенные по одному, по идентификатору каталога (I4). Выключить
        /// можно только те, что помечены в каталоге <c>Switchable</c>: их отключение
        /// не меняет того, что видят следующие стадии. Выключенное правило читается
        /// так, как в обычном чтении, а всё остальное — как при полном таджвиде.
        /// </summary>
        public IReadOnlySet<string> Off { get; init; } = new HashSet<string>();

        /// <summary>
        /// Не выключено ли правило по одному. Ступень проверяется отдельно
        /// (<see cref="Tajweed"/>): в обычном чтении правил таджвида нет и без этого.
        /// </summary>
        public bool Reads(string ruleId) => !Off.Contains(ruleId);
    }
}
