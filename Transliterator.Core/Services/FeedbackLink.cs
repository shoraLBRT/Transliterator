using static Transliterator.Core.Services.FeedbackMessage;

namespace Transliterator.Core.Services
{
    /// <summary>
    /// Ссылка на форму нового issue на GitHub с заполненными заголовком и текстом (H1).
    /// Токен GitHub в статике забрал бы любой, кто откроет код страницы, поэтому
    /// страница только собирает ссылку, а отправляет сам человек — своим аккаунтом.
    /// Это путь по умолчанию, пока не настроена функция обратной связи (H3), и запасной,
    /// когда отправка через неё не удалась.
    /// </summary>
    public static class FeedbackLink
    {
        public const string RepositoryUrl = "https://github.com/shoraLBRT/Transliterator";

        /// <summary>
        /// Потолок длины ссылки. Слишком длинную GitHub не открывает вовсе (414),
        /// а кириллица и арабица кодируются шестью знаками на букву: в ссылку
        /// помещается чуть больше тысячи букв на всё сообщение.
        /// </summary>
        public const int MaxUrlLength = 7500;

        /// <summary>
        /// Собирает ссылку. Вход и вывод транслитерации прикладываются, только если
        /// переданы оба: без пары баг не воспроизвести. Не помещается — обрезаются
        /// сначала они, и лишь затем само сообщение.
        /// </summary>
        public static FeedbackDraft Build(string message, string? arabic = null, string? transliteration = null)
        {
            message = message.Trim();
            var attach = !string.IsNullOrWhiteSpace(arabic) && !string.IsNullOrWhiteSpace(transliteration);
            var title = Title(message);

            if (!attach)
                return Fit(title, cap => Body(Clip(message, cap), null, null), message.Length);

            arabic = arabic!.Trim();
            transliteration = transliteration!.Trim();

            var full = Url(title, Body(message, arabic, transliteration));
            if (full.Length <= MaxUrlLength)
                return new FeedbackDraft(full, Truncated: false);

            // Приложение режется первым: сообщение написал человек, а вход у него
            // и так остался в поле.
            var longest = Math.Max(arabic.Length, transliteration.Length);
            var cap = Largest(longest, c => Url(title, Body(message, Clip(arabic, c), Clip(transliteration, c))).Length <= MaxUrlLength);
            if (cap > 0)
                return new FeedbackDraft(Url(title, Body(message, Clip(arabic, cap), Clip(transliteration, cap))), Truncated: true);

            return Fit(title, c => Body(Clip(message, c), null, null), message.Length);
        }

        private static FeedbackDraft Fit(string title, Func<int, string> body, int length)
        {
            var full = Url(title, body(length));
            if (full.Length <= MaxUrlLength)
                return new FeedbackDraft(full, Truncated: false);

            var cap = Largest(length, c => Url(title, body(c)).Length <= MaxUrlLength);
            return new FeedbackDraft(Url(title, body(cap)), Truncated: true);
        }

        /// <summary>Наибольший потолок от 0 до <paramref name="max"/>, при котором ссылка помещается.</summary>
        private static int Largest(int max, Func<int, bool> fits)
        {
            int low = 0, high = max;
            while (low < high)
            {
                var middle = (low + high + 1) / 2;
                if (fits(middle))
                    low = middle;
                else
                    high = middle - 1;
            }

            return low;
        }

        private static string Url(string title, string body) =>
            $"{RepositoryUrl}/issues/new?title={Uri.EscapeDataString(title)}&body={Uri.EscapeDataString(body)}";
    }

    /// <summary>Готовая ссылка и признак, что в неё вошло не всё.</summary>
    public sealed record FeedbackDraft(string Url, bool Truncated);
}
