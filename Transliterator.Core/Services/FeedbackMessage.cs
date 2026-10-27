using System.Text;

namespace Transliterator.Core.Services
{
    /// <summary>
    /// Заголовок и текст issue, которое заводит кнопка обратной связи. Один формат
    /// на оба пути отправки: ссылкой на форму GitHub (H1, <see cref="FeedbackLink"/>)
    /// и через функцию обратной связи (H3), — иначе issue одного и того же отзыва
    /// выглядели бы по-разному в зависимости от того, как он дошёл.
    /// </summary>
    public static class FeedbackMessage
    {
        public const string TitlePrefix = "Отзыв со страницы";

        /// <summary>Заголовок issue — начало первой строки сообщения, не длиннее этого.</summary>
        public const int TitleLength = 60;

        /// <summary>Потолок сообщения — то, что поле панели даёт написать.</summary>
        public const int MessageLength = 1000;

        /// <summary>
        /// Потолок каждой половины приложенной пары. Ссылке хватает и меньшего (её
        /// режет <see cref="FeedbackLink"/>), а функции нужна граница, чтобы чужой
        /// запрос не заводил issue по мегабайту.
        /// </summary>
        public const int AttachmentLength = 5000;

        /// <summary>
        /// Потолок текста, который принимает функция обратной связи
        /// (<c>MAX_BODY_LENGTH</c> в <c>feedback-worker/src/feedback.js</c>, сверяется
        /// тестом). С запасом над самым длинным, что даёт <see cref="Compose"/>.
        /// </summary>
        public const int MaxBodyLength = 12000;

        /// <summary>Потолок заголовка там же (<c>MAX_TITLE_LENGTH</c>).</summary>
        public const int MaxTitleLength = 120;

        private const string Ellipsis = "…";

        private const string Signature = "<sub>Отправлено кнопкой обратной связи на странице транслитерации.</sub>";

        /// <summary>
        /// Заголовок и текст целиком, без подгонки под длину ссылки. Вход и вывод
        /// транслитерации прикладываются, только если переданы оба: без пары баг
        /// не воспроизвести.
        /// </summary>
        public static (string Title, string Body) Compose(string message, string? arabic = null, string? transliteration = null)
        {
            message = Clip(message.Trim(), MessageLength);

            if (string.IsNullOrWhiteSpace(arabic) || string.IsNullOrWhiteSpace(transliteration))
                return (Title(message), Body(message, null, null));

            return (Title(message), Body(message,
                Clip(arabic.Trim(), AttachmentLength),
                Clip(transliteration.Trim(), AttachmentLength)));
        }

        public static string Title(string message)
        {
            var line = message.Trim().Split('\n', 2)[0].Trim();
            if (line.Length == 0)
                return TitlePrefix;

            return $"{TitlePrefix}: {Clip(line, TitleLength)}";
        }

        /// <summary>Текст issue; пара прикладывается, когда обе её половины не <c>null</c>.</summary>
        public static string Body(string message, string? arabic, string? transliteration)
        {
            var body = new StringBuilder(message);

            if (arabic is not null && transliteration is not null)
            {
                if (body.Length > 0)
                    body.Append("\n\n");

                body.Append("**Текст из поля**\n\n```text\n").Append(arabic).Append("\n```\n\n")
                    .Append("**Транслитерация**\n\n```text\n").Append(transliteration).Append("\n```");
            }

            body.Append("\n\n").Append(Signature);
            return body.ToString();
        }

        /// <summary>
        /// Обрезает до <paramref name="cap"/> знаков с многоточием. Суррогатную пару
        /// не рвёт: половинка эмодзи — неверный UTF-8, и кодирование ссылки падает.
        /// </summary>
        public static string Clip(string text, int cap)
        {
            if (text.Length <= cap)
                return text;

            if (cap > 0 && char.IsHighSurrogate(text[cap - 1]))
                cap--;

            return text[..cap].TrimEnd() + Ellipsis;
        }
    }
}
