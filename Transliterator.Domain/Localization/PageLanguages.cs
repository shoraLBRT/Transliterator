using System.Globalization;

namespace Transliterator.Domain.Localization
{
    /// <summary>
    /// Язык интерфейса страницы (D7): код культуры, имя на самом языке — так его
    /// показывает переключатель, — и профиль записи, которым страница пишет, пока
    /// человек не выбрал свой.
    /// </summary>
    public sealed record PageLanguage(string Code, string NativeName, string DefaultProfile);

    /// <summary>
    /// Языки, на которых есть интерфейс страницы и тексты данных (каталог правил,
    /// имена сур). Список один на всё: по нему сверяются ресурсы интерфейса,
    /// каталог отказывается загружаться без текста на каждом из них, а тесты
    /// падают, если у строки нет перевода. Третий язык — строка здесь, его файлы
    /// ресурсов и его тексты в данных; где чего не хватает, скажут тесты.
    /// </summary>
    public static class PageLanguages
    {
        public static PageLanguage Russian { get; } = new("ru", "Русский", "Standard");

        /// <summary>
        /// Английскому читателю кириллица не поможет: по умолчанию — латиница
        /// (решение владельца 2026-11-12, D7). Сохранённый выбор профиля важнее.
        /// </summary>
        public static PageLanguage English { get; } = new("en", "English", "Latin");

        /// <summary>Порядок — порядок в переключателе.</summary>
        public static IReadOnlyList<PageLanguage> All { get; } = new[] { Russian, English };

        /// <summary>
        /// Язык, когда ни сохранённый выбор, ни языки браузера не подошли. Русский,
        /// а не английский: страница пишет кириллицей, и у кого браузер на казахском,
        /// узбекском или татарском, тот скорее читает по-русски.
        /// </summary>
        public static PageLanguage Default => Russian;

        public static PageLanguage? Find(string? code) =>
            All.FirstOrDefault(language => string.Equals(language.Code, code, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Язык страницы: сохранённый выбор, иначе первый язык браузера, который
        /// есть у страницы (<c>en-GB</c> — это <c>en</c>), иначе <see cref="Default"/>.
        /// Из хранилища приходит что угодно — язык, которого больше нет, не мешает.
        /// </summary>
        public static PageLanguage Resolve(string? stored, IEnumerable<string>? browser)
        {
            if (Find(stored) is { } chosen)
                return chosen;

            foreach (var tag in browser ?? Enumerable.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(tag))
                    continue;

                var primary = tag.Split('-', '_')[0];
                if (Find(primary) is { } match)
                    return match;
            }

            return Default;
        }

        /// <summary>Язык культуры интерфейса: <c>en-US</c> — английский, неизвестная — <see cref="Default"/>.</summary>
        public static PageLanguage Of(CultureInfo culture)
        {
            for (var current = culture; !string.IsNullOrEmpty(current.Name); current = current.Parent)
            {
                if (Find(current.Name) is { } language)
                    return language;
            }

            return Default;
        }

        /// <summary>Язык, на котором сейчас интерфейс.</summary>
        public static PageLanguage Current => Of(CultureInfo.CurrentUICulture);
    }
}
