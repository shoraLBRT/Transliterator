using System.Globalization;
using Microsoft.JSInterop;
using Transliterator.Domain.Localization;

namespace Transliterator.Web.Localization
{
    /// <summary>
    /// Язык страницы (D7) ставится один раз, до запуска приложения: так в Blazor
    /// WebAssembly принято — спутниковые сборки с переводами загружаются под культуру,
    /// которая стоит к старту. Смена языка поэтому — запись выбора и перезагрузка
    /// страницы (<see cref="SwitchAsync"/>); текст в поле при этом не теряется.
    /// </summary>
    public static class PageCulture
    {
        /// <summary>Ключ хранилища браузера с выбранным языком. Автоматический выбор не пишется.</summary>
        public const string LanguageKey = "transliterator.language";

        /// <summary>
        /// Ключ хранилища вкладки, в котором текст поля переживает перезагрузку при
        /// смене языка. Вкладки, а не браузера: в новой вкладке прежний текст не нужен.
        /// </summary>
        public const string DraftKey = "transliterator.draft";

        /// <summary>
        /// Сохранённый выбор, иначе язык браузера, иначе русский. Скрипт страницы
        /// недоступен — страница остаётся на языке по умолчанию, но работает.
        /// </summary>
        public static async Task ApplyAsync(IJSRuntime js)
        {
            string? stored = null;
            string[]? browser = null;

            try
            {
                stored = await js.InvokeAsync<string?>("page.load", LanguageKey);
                browser = await js.InvokeAsync<string[]?>("page.browserLanguages");
            }
            catch (JSException)
            {
            }

            var culture = CultureInfo.GetCultureInfo(PageLanguages.Resolve(stored, browser).Code);

            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }

        /// <summary>
        /// Выбор запоминается, текст поля откладывается до перезагрузки, и страница
        /// загружается заново уже на другом языке.
        /// </summary>
        public static async Task SwitchAsync(IJSRuntime js, PageLanguage language, string draft)
        {
            await js.InvokeVoidAsync("page.save", LanguageKey, language.Code);
            await js.InvokeVoidAsync("page.sessionSave", DraftKey, draft);
            await js.InvokeVoidAsync("page.reload");
        }

        /// <summary>Текст, отложенный сменой языка; читается один раз.</summary>
        public static async Task<string?> TakeDraftAsync(IJSRuntime js) =>
            await js.InvokeAsync<string?>("page.sessionTake", DraftKey);
    }
}
