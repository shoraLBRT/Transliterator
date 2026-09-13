using Transliterator.Domain.Exceptions;

namespace Transliterator.Domain.Interfaces
{
    /// <summary>
    /// Строковое хранилище «ключ → значение» — localStorage браузера. Выделено
    /// в интерфейс, чтобы хранилище профилей пользователя можно было проверить
    /// без браузера: порчу данных и переполнение квоты руками в тестах
    /// воспроизвести проще, чем в настоящем localStorage.
    /// </summary>
    public interface IKeyValueStore
    {
        /// <summary>Значение по ключу; <c>null</c>, если ключа нет.</summary>
        Task<string?> GetAsync(string key);

        /// <summary>
        /// Записывает значение целиком. Запись атомарна: не записалось — прежнее
        /// значение осталось как было.
        /// </summary>
        /// <exception cref="StorageQuotaExceededException">Места в хранилище не хватило.</exception>
        Task SetAsync(string key, string value);

        Task RemoveAsync(string key);

        /// <summary>Все ключи хранилища, в том числе чужие.</summary>
        Task<IReadOnlyList<string>> GetKeysAsync();
    }
}
