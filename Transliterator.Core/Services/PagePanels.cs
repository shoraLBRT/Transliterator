namespace Transliterator.Core.Services
{
    /// <summary>
    /// Какая панель страницы открыта — сура, ступень чтения или профиль (D6).
    /// Открыта всегда одна: на широком экране панель стоит колонкой сбоку, без
    /// подложки, которая закрывала бы её щелчком мимо, и без общего учёта
    /// вторая кнопка открыла бы вторую колонку поверх первой. Страница по этому
    /// же состоянию отодвигает поле и вывод, освобождая место колонке.
    /// </summary>
    public sealed class PagePanels
    {
        /// <summary>Имя открытой панели или <c>null</c>, если все закрыты.</summary>
        public string? Current { get; private set; }

        /// <summary>Открылась или закрылась какая-то панель.</summary>
        public event Action? Changed;

        public bool IsOpen(string panel) => Current == panel;

        /// <summary>Открывает панель; открытая до неё закрывается.</summary>
        public void Open(string panel)
        {
            if (Current == panel)
                return;

            Current = panel;
            Changed?.Invoke();
        }

        /// <summary>Закрывает панель, если открыта именно она: чужую не трогает.</summary>
        public void Close(string panel)
        {
            if (Current != panel)
                return;

            Current = null;
            Changed?.Invoke();
        }

        public void Toggle(string panel)
        {
            if (IsOpen(panel))
                Close(panel);
            else
                Open(panel);
        }
    }
}
