using System.Net;
using System.Net.Http.Json;

namespace Transliterator.Core.Services
{
    /// <summary>
    /// Отправка обратной связи через функцию обратной связи (H3,
    /// <c>feedback-worker/</c>): она проверяет Turnstile и заводит issue сама,
    /// аккаунт GitHub человеку не нужен. Ответ функции переводится в исход,
    /// который страница может объяснить словами.
    /// </summary>
    public sealed class FeedbackClient
    {
        private readonly HttpClient _http;

        public FeedbackClient(HttpClient http)
        {
            _http = http;
        }

        public async Task<FeedbackResult> SendAsync(string endpoint, string title, string body, string token,
                                                    CancellationToken cancellationToken = default)
        {
            HttpResponseMessage response;
            try
            {
                response = await _http.PostAsJsonAsync(endpoint, new FeedbackRequest(title, body, token), cancellationToken);
            }
            catch (HttpRequestException)
            {
                // Нет сети, функция недоступна или браузер не пустил ответ (CORS):
                // для человека это одно и то же — не дошло.
                return new FeedbackResult(FeedbackOutcome.Unreachable);
            }

            using (response)
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.Created:
                        var receipt = await response.Content.ReadFromJsonAsync<FeedbackReceipt>(cancellationToken);
                        return new FeedbackResult(FeedbackOutcome.Sent, receipt?.Url);

                    case HttpStatusCode.Forbidden:
                        return new FeedbackResult(FeedbackOutcome.NotVerified);

                    case HttpStatusCode.TooManyRequests:
                        return new FeedbackResult(FeedbackOutcome.TooMany);

                    default:
                        return new FeedbackResult(FeedbackOutcome.Failed);
                }
            }
        }

        private sealed record FeedbackRequest(string Title, string Body, string Token);

        private sealed record FeedbackReceipt(int Number, string Url);
    }

    public enum FeedbackOutcome
    {
        /// <summary>Issue заведено.</summary>
        Sent,

        /// <summary>Проверка Turnstile не прошла — или запрос пришёл не со своей страницы.</summary>
        NotVerified,

        /// <summary>Лимит сообщений с одного адреса.</summary>
        TooMany,

        /// <summary>До функции не достучаться.</summary>
        Unreachable,

        /// <summary>Функция ответила ошибкой: отвергла текст или не смогла завести issue.</summary>
        Failed,
    }

    /// <param name="IssueUrl">Адрес заведённого issue — только при <see cref="FeedbackOutcome.Sent"/>.</param>
    public sealed record FeedbackResult(FeedbackOutcome Outcome, string? IssueUrl = null);

    /// <summary>
    /// Настройки обратной связи страницы (<c>wwwroot/appsettings.json</c>, раздел
    /// <c>Feedback</c>). Оба значения публичные: адрес функции и ключ сайта Turnstile
    /// видны в коде любой страницы, которая ими пользуется. Пока хоть одно пусто,
    /// страница отправляет по-старому — ссылкой на форму GitHub (H1).
    /// </summary>
    public sealed record FeedbackSettings(string? Endpoint, string? TurnstileSiteKey)
    {
        public bool Direct => !string.IsNullOrWhiteSpace(Endpoint) && !string.IsNullOrWhiteSpace(TurnstileSiteKey);
    }
}
