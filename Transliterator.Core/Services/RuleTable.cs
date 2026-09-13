namespace Transliterator.Core.Services
{
    /// <summary>Вариант буквы: <c>"ر|heavy"</c> — имя <c>heavy</c>, ключ целиком и графема.</summary>
    public sealed record RuleVariant(string Name, string Key, string Value);

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
        /// Поиск по ключу. Группа находится целиком — и по своей букве, и по ключу
        /// или имени любого своего варианта: <c>heavy</c> показывает все буквы,
        /// у которых такой вариант есть, вместе с их базовыми графемами.
        /// </summary>
        public static IReadOnlyList<RuleGroup> Filter(IReadOnlyList<RuleGroup> groups, string? query)
        {
            var needle = query?.Trim();

            if (string.IsNullOrEmpty(needle))
                return groups;

            return groups.Where(group => Matches(group.BaseKey, needle)
                                      || group.Variants.Any(variant => Matches(variant.Key, needle)))
                         .ToList();
        }

        // Регистр значим только для имён вариантов: у арабских букв его нет.
        private static bool Matches(string key, string needle) =>
            key.Contains(needle, StringComparison.OrdinalIgnoreCase);
    }
}
