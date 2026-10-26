using System.Text;

namespace Transliterator.Core.Services
{
    /// <summary>
    /// Ссылка на форму нового issue на GitHub с заполненными заголовком и текстом —
    /// так страница отправляет обратную связь (H1). Сервера у страницы нет, а токен
    /// GitHub в статике забрал бы любой, кто откроет её код. Поэтому страница
    /// только собирает ссылку, а отправляет сам человек — на GitHub, своим аккаунтом.
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

        /// <summary>Заголовок issue — начало первой строки сообщения, не длиннее этого.</summary>
        public const int TitleLength = 60;

        public const string TitlePrefix = "Отзыв со страницы";

        private const string Ellipsis = "…";

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

        private static string Title(string message)
        {
            var line = message.Split('\n', 2)[0].Trim();
            if (line.Length == 0)
                return TitlePrefix;

            return $"{TitlePrefix}: {Clip(line, TitleLength)}";
        }

        private static string Body(string message, string? arabic, string? transliteration)
        {
            var body = new StringBuilder(message);

            if (arabic is not null && transliteration is not null)
            {
                if (body.Length > 0)
                    body.Append("\n\n");

                body.Append("**Текст из поля**\n\n```text\n").Append(arabic).Append("\n```\n\n")
                    .Append("**Транслитерация**\n\n```text\n").Append(transliteration).Append("\n```");
            }

            body.Append("\n\n<sub>Отправлено кнопкой обратной связи на странице транслитерации.</sub>");
            return body.ToString();
        }

        private static string Url(string title, string body) =>
            $"{RepositoryUrl}/issues/new?title={Uri.EscapeDataString(title)}&body={Uri.EscapeDataString(body)}";

        /// <summary>
        /// Обрезает до <paramref name="cap"/> знаков с многоточием. Суррогатную пару
        /// не рвёт: половинка эмодзи в ссылке — неверный UTF-8, и кодирование падает.
        /// </summary>
        private static string Clip(string text, int cap)
        {
            if (text.Length <= cap)
                return text;

            if (cap > 0 && char.IsHighSurrogate(text[cap - 1]))
                cap--;

            return text[..cap].TrimEnd() + Ellipsis;
        }
    }

    /// <summary>Готовая ссылка и признак, что в неё вошло не всё.</summary>
    public sealed record FeedbackDraft(string Url, bool Truncated);
}
