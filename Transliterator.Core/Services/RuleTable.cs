using Transliterator.Core.Services.Phonology;

namespace Transliterator.Core.Services
{
    /// <summary>Вариант буквы: <c>"ر|heavy"</c> — имя <c>heavy</c>, ключ целиком и графема.</summary>
    public sealed record RuleVariant(string Name, string Key, string Value)
    {
        /// <summary>
        /// Что вариант значит, по-русски: <c>heavy</c> — «твёрдо, под эмфазой».
        /// <c>null</c> — имя рендереру неизвестно (опечатка в своём профиле).
        /// </summary>
        public string? Note => RuleTable.VariantNotes.GetValueOrDefault(Name);
    }

    /// <summary>
    /// Буква профиля со всеми своими вариантами.
    /// </summary>
    /// <param name="BaseValue">
    /// Графема базового ключа. <c>null</c> — ключа в профиле нет вовсе;
    /// пустая строка — ключ есть, и буква осознанно не пишется.
    /// </param>
    public sealed record RuleGroup(string BaseKey, string? BaseValue, IReadOnlyList<RuleVariant> Variants);

    /// <summary>
    /// Правила профиля таблицей: варианты собраны под своей буквой. В самом профиле
    /// это плоский словарь, где <c>"ر|heavy"</c> и <c>"ر"</c> — просто два ключа,
    /// и связь между ними знает только рендерер. Таблице просмотра (D4) и редактору
    /// (E2) нужна именно связь: вариант без своей буквы ничего не значит.
    /// </summary>
    public static class RuleTable
    {
        /// <summary>Разделитель буквы и варианта в ключе профиля.</summary>
        public const char VariantSeparator = '|';

        /// <summary>
        /// Пояснения к вариантам для того, кто не знает английского (E7). Имена
        /// вариантов — это то, что знает рендерер, и ключи профилей на них держатся;
        /// на странице рядом с именем показывается пояснение, и по нему же ищется.
        /// </summary>
        public static IReadOnlyDictionary<string, string> VariantNotes { get; } =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [CyrillicRenderer.HeavyVariant] = "твёрдо, под эмфазой",
                [CyrillicRenderer.SoftVariant] = "мягко, после мягкого ляма",
                [CyrillicRenderer.SukunVariant] = "без гласной, в закрытом слоге",
                [CyrillicRenderer.WaqfVariant] = "на паузе",
                [CyrillicRenderer.InitialVariant] = "в начале слова",
                [CyrillicRenderer.HiatusVariant] = "хамза между гласными",
                [CyrillicRenderer.FinalVariant] = "на конце слова",
                [CyrillicRenderer.GhunnaVariant] = "в нос, с гунной",
                [CyrillicRenderer.QalqalahVariant] = "с отзвуком, кальканя",
                [CyrillicRenderer.QalqalahStrongVariant] = "с сильным отзвуком на паузе",
            };

        /// <summary>
        /// Группы в порядке профиля: буква стоит там, где в профиле впервые встретился
        /// её ключ или ключ её варианта, варианты — в порядке профиля под ней.
        /// Порядок в профиле авторский (буквы, огласовки, цифры), и таблица его держит.
        /// </summary>
        public static IReadOnlyList<RuleGroup> Group(IReadOnlyDictionary<string, string> rules)
        {
            var order = new List<string>();
            var baseValues = new Dictionary<string, string>(StringComparer.Ordinal);
            var variants = new Dictionary<string, List<RuleVariant>>(StringComparer.Ordinal);

            foreach (var (key, value) in rules)
            {
                var separator = key.IndexOf(VariantSeparator);
                var baseKey = separator < 0 ? key : key[..separator];

                if (!variants.ContainsKey(baseKey))
                {
                    order.Add(baseKey);
                    variants[baseKey] = new List<RuleVariant>();
                }

                if (separator < 0)
                    baseValues[baseKey] = value;
                else
                    variants[baseKey].Add(new RuleVariant(key[(separator + 1)..], key, value));
            }

            return order.Select(baseKey => new RuleGroup(baseKey, baseValues.GetValueOrDefault(baseKey), variants[baseKey]))
                        .ToList();
        }

        /// <summary>
        /// Поиск. Группа находится целиком — по своей букве, по ключу или имени любого
        /// своего варианта (<c>heavy</c> показывает все буквы, у которых такой вариант
        /// есть, вместе с их базовыми графемами), по графеме — тому, как буква пишется
        /// («х»), — и по русскому пояснению варианта («пауз» находит <c>waqf</c>, E7).
        /// </summary>
        public static IReadOnlyList<RuleGroup> Filter(IReadOnlyList<RuleGroup> groups, string? query)
        {
            var needle = query?.Trim();

            if (string.IsNullOrEmpty(needle))
                return groups;

            return groups.Where(group => Matches(group.BaseKey, needle)
                                      || Matches(group.BaseValue, needle)
                                      || group.Variants.Any(variant => Matches(variant.Key, needle)
                                                                    || Matches(variant.Value, needle)
                                                                    || (needle.Length >= MinNoteQuery && Matches(variant.Note, needle))))
                         .ToList();
        }

        // Пояснение — слова, и одна-две буквы находят в них что угодно: «х» искал бы
        // графему «х», а нашёл бы и хамзу — по «хамза между гласными».
        private const int MinNoteQuery = 3;

        // Регистр не значим ни для имён вариантов, ни для графем: «Х» ищет и «х».
        // У арабских букв его нет.
        private static bool Matches(string? text, string needle) =>
            text is not null && text.Contains(needle, StringComparison.OrdinalIgnoreCase);
    }
}
