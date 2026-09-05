using System.Text.Json;
using Transliterator.Domain.Entities;

namespace Transliterator.Tests
{
    /// <summary>
    /// Профили для тестов берутся из настоящего ресурса, а не из копии в коде:
    /// прежняя хардкодированная копия успела разойтись с Standard.json,
    /// и тесты проверяли поведение, которого в приложении уже не было.
    /// </summary>
    public static class TestProfiles
    {
        private static readonly Lazy<TransliterationProfile> _standard = new(() => Load("Standard"));
        private static readonly Lazy<TransliterationProfile> _latin = new(() => Load("Latin"));

        public static TransliterationProfile Standard => _standard.Value;

        public static TransliterationProfile Latin => _latin.Value;

        /// <summary>Все профили, которые приложение раздаёт из ресурсов.</summary>
        public static IEnumerable<TransliterationProfile> All => new[] { Standard, Latin };

        private static TransliterationProfile Load(string name)
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Profiles", $"{name}.json");

            if (!File.Exists(path))
                throw new FileNotFoundException($"Профиль для тестов не найден: {path}", path);

            var profile = JsonSerializer.Deserialize<TransliterationProfile>(File.ReadAllText(path));

            return profile ?? throw new InvalidOperationException($"Не удалось разобрать профиль {name}");
        }
    }
}
