using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Transliterator.Core.Services;

namespace Transliterator.Web.Components
{
    /// <summary>
    /// Кнопка с панелью — сура, ступень чтения, профиль (D6). Все три ведут себя
    /// одинаково: открыта одна панель на страницу (<see cref="PagePanels"/>), фокус
    /// при открытии переходит в неё, Escape и крестик в шапке закрывают её и
    /// возвращают фокус на кнопку. На широком экране панель — колонка сбоку без
    /// подложки: поле и вывод рядом остаются рабочими, и щелчок по ним её не
    /// закрывает. На телефоне — как прежде, поверх, и щелчок мимо закрывает.
    /// </summary>
    public abstract class PanelPicker : ComponentBase, IDisposable
    {
        [Inject]
        protected PagePanels Panels { get; set; } = default!;

        /// <summary>Имя панели в <see cref="PagePanels"/>.</summary>
        protected abstract string Panel { get; }

        protected bool IsOpen => Panels.IsOpen(Panel);

        protected ElementReference ToggleButton;
        protected ElementReference PanelElement;

        private bool _focusPanel;
        private bool _wasOpen;

        protected override void OnInitialized() => Panels.Changed += OnPanelsChanged;

        /// <summary>
        /// Панель могла закрыть и другая кнопка — открыв свою. Тогда эта узнаёт
        /// о закрытии отсюда и сбрасывает то, что было начато внутри.
        /// </summary>
        private void OnPanelsChanged()
        {
            if (_wasOpen && !IsOpen)
                OnClosed();

            _wasOpen = IsOpen;
            _ = InvokeAsync(StateHasChanged);
        }

        /// <summary>Панель закрылась — любым путём.</summary>
        protected virtual void OnClosed()
        {
        }

        /// <summary>Подготовка перед открытием: например, прочитать список.</summary>
        protected virtual Task BeforeOpenAsync() => Task.CompletedTask;

        protected async Task ToggleAsync()
        {
            if (IsOpen)
            {
                Close();
                return;
            }

            await BeforeOpenAsync();
            _focusPanel = true;
            Panels.Open(Panel);
        }

        protected void Close() => Panels.Close(Panel);

        protected async Task CloseToToggleAsync()
        {
            Close();
            await ToggleButton.FocusAsync();
        }

        protected async Task OnPanelKeyDownAsync(KeyboardEventArgs e)
        {
            if (e.Key == "Escape")
                await CloseToToggleAsync();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (_focusPanel && IsOpen)
            {
                _focusPanel = false;
                await PanelElement.FocusAsync();
            }
        }

        public void Dispose()
        {
            Panels.Changed -= OnPanelsChanged;
            GC.SuppressFinalize(this);
        }
    }
}
