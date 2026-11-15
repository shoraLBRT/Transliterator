namespace Transliterator.Web.Components
{
    /// <summary>
    /// Закрытие панели с анимацией (D10). Закрытая панель ещё <see cref="Duration"/>
    /// остаётся в разметке — с классом <c>is-closing</c>, <c>inert</c> и
    /// <c>aria-hidden</c>: её уже не нажать и диктор её не читает, но она успевает
    /// плавно уйти. Потом исчезает, как прежде. Открыли снова, пока уходила, —
    /// уход отменяется. Длительность — та же, что <c>--motion-duration</c> в app.css;
    /// с <c>prefers-reduced-motion</c> уходящая панель скрыта сразу.
    /// </summary>
    public sealed class PanelClosing
    {
        /// <summary>Как <c>--motion-duration</c> в app.css.</summary>
        public static readonly TimeSpan Duration = TimeSpan.FromMilliseconds(200);

        private int _run;

        /// <summary>Панель закрыта, но ещё в разметке — уходит.</summary>
        public bool IsClosing { get; private set; }

        /// <summary>Класс уходящей панели.</summary>
        public string? CssClass => IsClosing ? "is-closing" : null;

        /// <summary>Для <c>aria-hidden</c>: уходящую панель диктор не читает.</summary>
        public string? AriaHidden => IsClosing ? "true" : null;

        /// <summary>
        /// Панель закрылась: держит её уходящей <see cref="Duration"/>, затем
        /// просит перерисовать уже без неё.
        /// </summary>
        public async Task RunAsync(Func<Task> render)
        {
            var run = ++_run;
            IsClosing = true;
            await render();
            await Task.Delay(Duration);

            if (run != _run)
                return;

            IsClosing = false;
            await render();
        }

        /// <summary>Панель открылась снова — уход отменяется.</summary>
        public void Cancel()
        {
            _run++;
            IsClosing = false;
        }
    }
}
