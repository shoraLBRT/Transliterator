using System.Reflection;
using System.Text.Json;
using Transliterator.Domain.Entities;
using Transliterator.Domain.Exceptions;
using Transliterator.Domain.Localization;

namespace Transliterator.Core.Repositories
{
    /// <summary>
    /// Таблица звуков (D11) из ресурса сборки — так же, как каталог правил
    /// (<see cref="EmbeddedTajweedRuleCatalog"/>): один JSON-файл в ядре, без
    /// файловой системы, тексты вычитывает владелец.
    /// </summary>
    /// <remarks>
    /// Битый ресурс роняет чтение: ресурс фиксируется на сборке, и битая таблица —
    /// сломанная сборка. Здесь же — что у каждого звука есть буква, название и
    /// текст произношения на каждом языке страницы. Что буква есть в профилях —
    /// дело тестов: профиль может быть и свой, и в нём буквы может не быть.
    /// </remarks>
    public static class EmbeddedSoundTable
    {
        /// <summary>Имя ресурса из корневого пространства имён и пути <c>Resources\Sounds.json</c>.</summary>
        public const string ResourceName = "Transliterator.Core.Resources.Sounds.json";

        private static readonly Lazy<SoundTable> _table =
            new(() => Load(typeof(EmbeddedSoundTable).Assembly, ResourceName));

        /// <summary>Таблица читается один раз. Экземпляр общий — не меняйте его.</summary>
        public static SoundTable Table => _table.Value;

        /// <summary>Таблицу можно вшить и в чужую сборку — тесты так проверяют битые ресурсы.</summary>
        public static SoundTable Load(Assembly assembly, string resourceName)
        {
            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new TransliterationException($"Embedded sound table is missing: {resourceName}");

            SoundTable? table;

            try
            {
                table = JsonSerializer.Deserialize<SoundTable>(stream);
            }
            catch (JsonException ex)
            {
                throw new TransliterationException($"Embedded sound table is not valid JSON: {resourceName}", ex);
            }

            if (table is null)
                throw new TransliterationException($"Embedded sound table is empty: {resourceName}");

            Validate(table, resourceName);

            return table;
        }

        private static void Validate(SoundTable table, string resourceName)
        {
            void Require(bool condition, string message)
            {
                if (!condition)
                    throw new TransliterationException($"Sound table {resourceName}: {message}");
            }

            // Каждый текст — на каждом языке страницы (D7), как у каталога правил.
            void RequireTexts(LocalizedText? text, string message)
            {
                var missing = text?.MissingLanguages() ?? PageLanguages.All.Select(l => l.Code).ToList();
                Require(missing.Count == 0, $"{message} in {string.Join(", ", missing)}");
            }

            Require(table.Sounds.Count > 0, "no sounds");

            var letters = new HashSet<string>();
            foreach (var sound in table.Sounds)
            {
                Require(!string.IsNullOrWhiteSpace(sound.Letter), "a sound has no letter");
                Require(letters.Add(sound.Letter), $"letter '{sound.Letter}' is declared twice");
                RequireTexts(sound.Name, $"letter '{sound.Letter}' has no name");
                RequireTexts(sound.Pronunciation, $"letter '{sound.Letter}' has no pronunciation");
            }
        }
    }
}
