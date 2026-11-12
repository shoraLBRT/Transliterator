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

        /// <summary>
        /// Таджвид со своими счётами там, где он сам даёт выбор, и без правил,
        /// выключенных по одному (I4). На странице — «Неполный таджвид» (I5);
        /// имя в коде и в браузере (<c>"variants"</c>) прежнее, чтобы сохранённый
        /// выбор не потерялся.
        /// </summary>
        Variants,

        /// <summary>Обычное арабское чтение без таджвида.</summary>
        Plain,
    }

    /// <summary>
    /// Выбор человека на странице: ступень, счёты и выключенные правила. Из него
    /// собирается <see cref="ReadingOptions"/> для конвейера, и он же хранится в браузере.
    /// <para>
    /// Счёты и выключенные правила помнятся и тогда, когда ступень — не неполный таджвид:
    /// переключился на полный таджвид и обратно — выбор на месте. Действуют они только
    /// в неполном таджвиде: полный таджвид — это все правила и счёты по умолчанию,
    /// а обычное чтение своих вариантов не имеет.
    /// </para>
    /// </summary>
    public sealed record ReadingChoice(ReadingMode Mode, IReadOnlyDictionary<string, int> Counts)
    {
        public static ReadingChoice Default { get; } = new(ReadingMode.Full, new Dictionary<string, int>());

        /// <summary>
        /// Правила, выключенные по одному (I4), по идентификатору каталога. Пусто —
        /// читаются все. Хранится отдельно от счётов: у правила либо счёт, либо
        /// переключатель, и выключенное правило счёта не теряет.
        /// </summary>
        public IReadOnlySet<string> Off { get; init; } = new HashSet<string>();

        /// <summary>Выбранный счёт правила, а без выбора — его счёт по умолчанию из каталога.</summary>
        public int CountOf(TajweedRule rule) =>
            Counts.TryGetValue(rule.Id, out var count) ? count : rule.DefaultCount;

        public ReadingChoice WithMode(ReadingMode mode) => this with { Mode = mode };

        public ReadingChoice WithCount(string ruleId, int count) =>
            this with { Counts = new Dictionary<string, int>(Counts) { [ruleId] = count } };

        /// <summary>Читается ли правило: не выключено ли оно по одному.</summary>
        public bool Reads(TajweedRule rule) => !Off.Contains(rule.Id);

        /// <summary>Правило включено или выключено по одному (I4).</summary>
        public ReadingChoice WithRule(string ruleId, bool on)
        {
            var off = new HashSet<string>(Off);
            if (on)
                off.Remove(ruleId);
            else
                off.Add(ruleId);

            return this with { Off = off };
        }

        public ReadingOptions ToOptions() => Mode switch
        {
            ReadingMode.Plain => ReadingOptions.Plain,
            ReadingMode.Variants => new ReadingOptions { Counts = Counts, Off = Off },
            _ => ReadingOptions.FullTajweed,
        };

        /// <summary>
        /// Для хранения в браузере:
        /// <c>{"mode":"variants","counts":{"madd-arid":4},"off":["qalqalah-sughra"]}</c>.
        /// Выключенные правила — по порядку, чтобы один и тот же выбор всегда давал
        /// одну и ту же строку: по ней страница узнаёт, изменилось ли что-нибудь.
        /// </summary>
        public string Serialize() =>
            JsonSerializer.Serialize(new Stored(Mode.ToString().ToLowerInvariant(),
                                                new Dictionary<string, int>(Counts),
                                                Off.Order(StringComparer.Ordinal).ToList()));

        /// <summary>
        /// Сохранённый выбор, сверенный с каталогом. Хранилище браузера переживает
        /// выпуски страницы: правило могло уйти из каталога, а счёт — из допустимых.
        /// Такое молча отбрасывается — неизвестная ступень даёт полный таджвид,
        /// недопустимый счёт — счёт по умолчанию, а правило, которое больше нельзя
        /// выключить, — читается. Битая запись страницу не ломает. Запись, сделанная
        /// до I4, выключенных правил не знает и читается как «все включены».
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

            var off = new HashSet<string>();
            foreach (var rule in catalog.Rules.Where(r => r.Switchable))
                if (stored.off?.Contains(rule.Id) == true)
                    off.Add(rule.Id);

            return new ReadingChoice(mode, counts) { Off = off };
        }

        // Имена полей — как в хранилище, в нижнем регистре.
        private sealed record Stored(string? mode, Dictionary<string, int>? counts, List<string>? off = null);
    }
}
