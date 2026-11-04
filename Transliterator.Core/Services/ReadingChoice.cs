using System.Text.Json;
using Transliterator.Domain.Entities;
using Transliterator.Domain.Phonology;

namespace Transliterator.Core.Services
{
    /// <summary>Ступень чтения, которую человек выбрал на странице (I2).</summary>
    public enum ReadingMode
    {
        /// <summary>Полный таджвид, счёты по умолчанию. Вид по умолчанию.</summary>
        Full,

        /// <summary>Таджвид со своими счётами там, где он сам даёт выбор.</summary>
        Variants,

        /// <summary>Обычное арабское чтение без таджвида.</summary>
        Plain,
    }

    /// <summary>
    /// Выбор человека на странице: ступень и счёты. Из него собирается
    /// <see cref="ReadingOptions"/> для конвейера, и он же хранится в браузере.
    /// <para>
    /// Счёты помнятся и тогда, когда ступень — не «варианты»: переключился
    /// на полный таджвид и обратно — выбор на месте. Действуют они только на
    /// ступени «варианты»: полный таджвид — это счёты по умолчанию, а обычное
    /// чтение своих вариантов не имеет.
    /// </para>
    /// </summary>
    public sealed record ReadingChoice(ReadingMode Mode, IReadOnlyDictionary<string, int> Counts)
    {
        public static ReadingChoice Default { get; } = new(ReadingMode.Full, new Dictionary<string, int>());

        /// <summary>Выбранный счёт правила, а без выбора — его счёт по умолчанию из каталога.</summary>
        public int CountOf(TajweedRule rule) =>
            Counts.TryGetValue(rule.Id, out var count) ? count : rule.DefaultCount;

        public ReadingChoice WithMode(ReadingMode mode) => this with { Mode = mode };

        public ReadingChoice WithCount(string ruleId, int count) =>
            this with { Counts = new Dictionary<string, int>(Counts) { [ruleId] = count } };

        public ReadingOptions ToOptions() => Mode switch
        {
            ReadingMode.Plain => ReadingOptions.Plain,
            ReadingMode.Variants => new ReadingOptions { Counts = Counts },
            _ => ReadingOptions.FullTajweed,
        };

        /// <summary>Для хранения в браузере: <c>{"mode":"variants","counts":{"madd-arid":4}}</c>.</summary>
        public string Serialize() =>
            JsonSerializer.Serialize(new Stored(Mode.ToString().ToLowerInvariant(),
                                                new Dictionary<string, int>(Counts)));

        /// <summary>
        /// Сохранённый выбор, сверенный с каталогом. Хранилище браузера переживает
        /// выпуски страницы: правило могло уйти из каталога, а счёт — из допустимых.
        /// Такое молча отбрасывается — неизвестная ступень даёт полный таджвид,
        /// недопустимый счёт — счёт по умолчанию. Битая запись страницу не ломает.
        /// </summary>
        public static ReadingChoice Parse(string? json, TajweedRuleCatalog catalog)
        {
            if (string.IsNullOrWhiteSpace(json))
                return Default;

            Stored? stored;
            try
            {
                stored = JsonSerializer.Deserialize<Stored>(json);
            }
            catch (JsonException)
            {
                return Default;
            }

            if (stored is null)
                return Default;

            var mode = Enum.TryParse<ReadingMode>(stored.mode, ignoreCase: true, out var parsed)
                       && Enum.IsDefined(parsed)
                ? parsed
                : ReadingMode.Full;

            var counts = new Dictionary<string, int>();
            foreach (var rule in catalog.Rules.Where(r => r.HasVariants))
                if (stored.counts?.TryGetValue(rule.Id, out var count) == true && rule.Counts.Contains(count))
                    counts[rule.Id] = count;

            return new ReadingChoice(mode, counts);
        }

        // Имена полей — как в хранилище, в нижнем регистре.
        private sealed record Stored(string? mode, Dictionary<string, int>? counts);
    }
}
