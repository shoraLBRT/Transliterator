namespace Transliterator.Domain.Entities
{
    /// <summary>
    /// Имена встроенных профилей и их прежние имена (E8).
    /// </summary>
    /// <remarks>
    /// Имя встроенного профиля — его ключ: по нему профиль выбирают в CLI
    /// (<c>--profile</c>) и помнят в браузере. Поэтому переименованный профиль
    /// продолжает находиться и по старому имени: старое имя — запасной путь,
    /// когда профиля с таким именем нет, а не второе имя рядом с новым.
    /// </remarks>
    public static class ProfileNames
    {
        /// <summary>Расширенная кириллица; до E8 называлась <c>Standard</c>.</summary>
        public const string Cyrillic = "Cyrillic";

        /// <summary>Латиница с диакритикой.</summary>
        public const string Latin = "Latin";

        /// <summary>
        /// Прежние имена встроенных профилей → нынешние. Регистр не важен:
        /// CLI и раньше понимал <c>--profile standard</c>.
        /// </summary>
        public static IReadOnlyDictionary<string, string> Former { get; } =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Standard"] = Cyrillic
            };

        /// <summary>
        /// Нынешнее имя профиля, если <paramref name="name"/> — прежнее имя
        /// встроенного; иначе само <paramref name="name"/>.
        /// </summary>
        public static string Current(string name) =>
            Former.TryGetValue(name, out var current) ? current : name;
    }
}
