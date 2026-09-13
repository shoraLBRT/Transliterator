using Microsoft.JSInterop;
using Transliterator.Domain.Exceptions;
using Transliterator.Domain.Interfaces;

namespace Transliterator.Web.Storage
{
    /// <summary>
    /// localStorage браузера как <see cref="IKeyValueStore"/>. Не куки: куки уходили бы
    /// на сервер с каждым запросом и вмещают около 4 КБ, а профиль больше.
    /// </summary>
    /// <remarks>
    /// Запись и перечень ключей идут через <c>wwwroot/js/storage.js</c>: у свойства
    /// <c>localStorage.length</c> нет функции, которую можно вызвать из .NET,
    /// а переполнение квоты надо опознать по имени ошибки, пока оно ещё есть,
    /// а не по тексту сообщения, который у каждого браузера свой.
    /// </remarks>
    public sealed class LocalStorageStore : IKeyValueStore
    {
        private const string QuotaError = "quota";

        private readonly IJSRuntime _js;

        public LocalStorageStore(IJSRuntime js) => _js = js;

        public async Task<string?> GetAsync(string key) =>
            await _js.InvokeAsync<string?>("localStorage.getItem", key);

        public async Task SetAsync(string key, string value)
        {
            var error = await _js.InvokeAsync<string?>("transliteratorStorage.set", key, value);

            if (error is null)
                return;

            if (error == QuotaError)
                throw new StorageQuotaExceededException($"localStorage quota exceeded while writing '{key}'");

            throw new InvalidOperationException($"localStorage.setItem failed for '{key}': {error}");
        }

        public async Task RemoveAsync(string key) =>
            await _js.InvokeVoidAsync("localStorage.removeItem", key);

        public async Task<IReadOnlyList<string>> GetKeysAsync() =>
            await _js.InvokeAsync<string[]>("transliteratorStorage.keys");
    }
}
