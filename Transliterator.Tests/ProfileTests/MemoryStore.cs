using Transliterator.Domain.Exceptions;
using Transliterator.Domain.Interfaces;

namespace Transliterator.Tests.ProfileTests
{
    /// <summary>
    /// localStorage в памяти. Порчу записи, переполнение квоты и запрет хранения
    /// здесь можно устроить нарочно — в настоящем браузере их не дождаться.
    /// </summary>
    internal sealed class MemoryStore : IKeyValueStore
    {
        public readonly Dictionary<string, string> Items = new(StringComparer.Ordinal);
        public bool Full;
        public bool Unavailable;

        public Task<string?> GetAsync(string key)
        {
            ThrowIfUnavailable();
            return Task.FromResult<string?>(Items.GetValueOrDefault(key));
        }

        public Task SetAsync(string key, string value)
        {
            ThrowIfUnavailable();
            if (Full)
                throw new StorageQuotaExceededException("QuotaExceededError");
            Items[key] = value;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key)
        {
            ThrowIfUnavailable();
            Items.Remove(key);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<string>> GetKeysAsync()
        {
            ThrowIfUnavailable();
            return Task.FromResult<IReadOnlyList<string>>(Items.Keys.ToList());
        }

        private void ThrowIfUnavailable()
        {
            if (Unavailable)
                throw new InvalidOperationException("SecurityError: localStorage is disabled");
        }
    }
}
