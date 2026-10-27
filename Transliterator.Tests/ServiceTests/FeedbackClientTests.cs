using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Transliterator.Core.Services;
using Transliterator.Tests.DocsTests;
using Xunit;

namespace Transliterator.Tests.ServiceTests
{
    /// <summary>
    /// Прямая отправка обратной связи (H3): страница шлёт заголовок, текст и токен
    /// Turnstile функции из <c>feedback-worker/</c>, а её ответ становится исходом,
    /// который панель объясняет словами.
    /// </summary>
    public class FeedbackClientTests
    {
        private const string Endpoint = "https://feedback.example/";

        [Fact]
        public async Task Message_IsPostedAsTheWorkerExpectsIt()
        {
            var handler = new FakeWorker(HttpStatusCode.Created, """{"number":7,"url":"https://github.com/shoraLBRT/Transliterator/issues/7"}""");

            var result = await new FeedbackClient(new HttpClient(handler)).SendAsync(Endpoint, "Заголовок", "Текст", "token");

            Assert.Equal(FeedbackOutcome.Sent, result.Outcome);
            Assert.Equal("https://github.com/shoraLBRT/Transliterator/issues/7", result.IssueUrl);

            Assert.Equal(HttpMethod.Post, handler.Request!.Method);
            Assert.Equal(Endpoint, handler.Request.RequestUri!.ToString());

            using var json = JsonDocument.Parse(handler.Body!);
            Assert.Equal("Заголовок", json.RootElement.GetProperty("title").GetString());
            Assert.Equal("Текст", json.RootElement.GetProperty("body").GetString());
            Assert.Equal("token", json.RootElement.GetProperty("token").GetString());
        }

        [Theory]
        [InlineData(HttpStatusCode.Forbidden, FeedbackOutcome.NotVerified)]
        [InlineData(HttpStatusCode.TooManyRequests, FeedbackOutcome.TooMany)]
        [InlineData(HttpStatusCode.BadRequest, FeedbackOutcome.Failed)]
        [InlineData(HttpStatusCode.BadGateway, FeedbackOutcome.Failed)]
        public async Task WorkerAnswer_BecomesAnOutcome(HttpStatusCode status, FeedbackOutcome outcome)
        {
            var handler = new FakeWorker(status, """{"error":"x"}""");

            var result = await new FeedbackClient(new HttpClient(handler)).SendAsync(Endpoint, "t", "b", "token");

            Assert.Equal(outcome, result.Outcome);
            Assert.Null(result.IssueUrl);
        }

        [Fact]
        public async Task NoConnection_IsUnreachable_NotACrash()
        {
            var handler = new FakeWorker(new HttpRequestException("TypeError: Failed to fetch"));

            var result = await new FeedbackClient(new HttpClient(handler)).SendAsync(Endpoint, "t", "b", "token");

            Assert.Equal(FeedbackOutcome.Unreachable, result.Outcome);
        }

        [Theory]
        [InlineData(null, null, false)]
        [InlineData("https://feedback.example/", "", false)]
        [InlineData("", "0x4AAA", false)]
        [InlineData("https://feedback.example/", "0x4AAA", true)]
        public void Page_SendsDirectly_OnlyWithBothSettings(string? endpoint, string? siteKey, bool direct)
        {
            Assert.Equal(direct, new FeedbackSettings(endpoint, siteKey).Direct);
        }

        /// <summary>
        /// Страница не должна собирать то, что функция отвергнет: самое длинное
        /// сообщение с самой длинной парой укладывается в её потолки.
        /// </summary>
        [Fact]
        public void LongestMessage_FitsTheWorkerLimits()
        {
            var (title, body) = FeedbackMessage.Compose(
                new string('ж', FeedbackMessage.MessageLength * 2),
                new string('ب', FeedbackMessage.AttachmentLength * 2),
                new string('б', FeedbackMessage.AttachmentLength * 2));

            Assert.InRange(title.Length, 1, FeedbackMessage.MaxTitleLength);
            Assert.InRange(body.Length, 1, FeedbackMessage.MaxBodyLength);
        }

        /// <summary>Потолки записаны дважды — в ядре и в функции — и расходиться не должны.</summary>
        [Theory]
        [InlineData("MAX_TITLE_LENGTH", FeedbackMessage.MaxTitleLength)]
        [InlineData("MAX_BODY_LENGTH", FeedbackMessage.MaxBodyLength)]
        public void WorkerLimit_MatchesTheCore(string name, int expected)
        {
            var worker = File.ReadAllText(Path.Combine(ReadmeTests.RepositoryRoot(), "feedback-worker", "src", "feedback.js"));
            var match = Regex.Match(worker, $@"export const {name} = (\d+);");

            Assert.True(match.Success, $"feedback.js has no {name}");
            Assert.Equal(expected, int.Parse(match.Groups[1].Value));
        }

        [Fact]
        public void Compose_KeepsTheLinkFormat()
        {
            var (title, body) = FeedbackMessage.Compose("Пропала хамза", "يَسْـَٔلُونَ", "йасалююн");

            Assert.Equal("Отзыв со страницы: Пропала хамза", title);
            Assert.Equal(FeedbackMessage.Body("Пропала хамза", "يَسْـَٔلُونَ", "йасалююн"), body);
            Assert.Contains("```text\nйасалююн\n```", body);
        }

        [Fact]
        public void Compose_WithoutAPair_AttachesNothing()
        {
            var (_, body) = FeedbackMessage.Compose("Пропала хамза", "يَسْـَٔلُونَ", " ");

            Assert.DoesNotContain("```", body);
        }

        private sealed class FakeWorker : HttpMessageHandler
        {
            private readonly HttpStatusCode _status;
            private readonly string _answer = string.Empty;
            private readonly Exception? _failure;

            public FakeWorker(HttpStatusCode status, string answer)
            {
                _status = status;
                _answer = answer;
            }

            public FakeWorker(Exception failure)
            {
                _failure = failure;
            }

            public HttpRequestMessage? Request { get; private set; }

            public string? Body { get; private set; }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                if (_failure is not null)
                    throw _failure;

                Request = request;
                Body = await request.Content!.ReadAsStringAsync(cancellationToken);

                return new HttpResponseMessage(_status)
                {
                    Content = new StringContent(_answer, Encoding.UTF8, "application/json"),
                };
            }
        }
    }
}
