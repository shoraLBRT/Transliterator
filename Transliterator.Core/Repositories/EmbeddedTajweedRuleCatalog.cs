using System.Reflection;
using System.Text.Json;
using Transliterator.Domain.Entities;
using Transliterator.Domain.Exceptions;

namespace Transliterator.Core.Repositories
{
    /// <summary>
    /// Каталог правил таджвида (I1) из ресурса сборки. Как профили и корпус, он
    /// лежит в ядре одним JSON-файлом и файловой системы не касается: каталог
    /// читают и страница в браузере, и CLI.
    /// </summary>
    /// <remarks>
    /// <para>
    /// JSON, а не код: тексты описаний вычитывает владелец, и править их удобнее
    /// в данных, чем в строковых литералах. Сверку с кодом это не ослабляет —
    /// каждый пример прогоняется через конвейер тестом.
    /// </para>
    /// <para>
    /// Битый ресурс роняет чтение, а не пропускается — по той же причине, что
    /// у <see cref="EmbeddedProfileRepository"/>: ресурс фиксируется на сборке,
    /// и битый каталог здесь — сломанная сборка, а не сломанные данные.
    /// Здесь же проверяется связность каталога: стадия, на которую ссылается
    /// правило, должна в нём быть. Сверка стадий с <c>RulesService</c> — дело
    /// тестов: ядро не перечисляет свои стадии нигде, кроме самого конвейера.
    /// </para>
    /// </remarks>
    public static class EmbeddedTajweedRuleCatalog
    {
        /// <summary>
        /// Имя ресурса собирается компилятором из корневого пространства имён
        /// и пути к файлу <c>Resources\TajweedRules.json</c>.
        /// </summary>
        public const string ResourceName = "Transliterator.Core.Resources.TajweedRules.json";

        private static readonly Lazy<TajweedRuleCatalog> _catalog =
            new(() => Load(typeof(EmbeddedTajweedRuleCatalog).Assembly, ResourceName));

        /// <summary>
        /// Каталог читается один раз: его содержимое фиксировано на сборке.
        /// Экземпляр общий — не меняйте его, читателям он отдаётся как есть.
        /// </summary>
        public static TajweedRuleCatalog Catalog => _catalog.Value;

        /// <summary>Каталог можно вшить и в чужую сборку — тесты так проверяют битые ресурсы.</summary>
        public static TajweedRuleCatalog Load(Assembly assembly, string resourceName)
        {
            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new TransliterationException($"Embedded tajweed rule catalog is missing: {resourceName}");

            TajweedRuleCatalog? catalog;

            try
            {
                catalog = JsonSerializer.Deserialize<TajweedRuleCatalog>(stream);
            }
            catch (JsonException ex)
            {
                throw new TransliterationException($"Embedded tajweed rule catalog is not valid JSON: {resourceName}", ex);
            }

            if (catalog is null)
                throw new TransliterationException($"Embedded tajweed rule catalog is empty: {resourceName}");

            Validate(catalog, resourceName);

            return catalog;
        }

        private static void Validate(TajweedRuleCatalog catalog, string resourceName)
        {
            void Require(bool condition, string message)
            {
                if (!condition)
                    throw new TransliterationException($"Tajweed rule catalog {resourceName}: {message}");
            }

            Require(catalog.Stages.Count > 0, "no stages");

            var stages = new HashSet<int>();
            foreach (var stage in catalog.Stages)
            {
                Require(stages.Add(stage.Number), $"stage {stage.Number} is declared twice");
                Require(!string.IsNullOrWhiteSpace(stage.Name) && !string.IsNullOrWhiteSpace(stage.Class),
                        $"stage {stage.Number} has no name or class");
            }

            var ids = new HashSet<string>();
            foreach (var rule in catalog.Rules)
            {
                Require(!string.IsNullOrWhiteSpace(rule.Id), "a rule has no id");
                Require(ids.Add(rule.Id), $"rule '{rule.Id}' is declared twice");
                Require(stages.Contains(rule.Stage), $"rule '{rule.Id}' refers to unknown stage {rule.Stage}");
                Require(!string.IsNullOrWhiteSpace(rule.Name)
                        && !string.IsNullOrWhiteSpace(rule.ArabicName)
                        && !string.IsNullOrWhiteSpace(rule.Description),
                        $"rule '{rule.Id}' has no name, Arabic name or description");

                // Перевод названия (I7) обязателен, как и само название: без него
                // «Мадд лязим» на странице — арабское слово русскими буквами.
                Require(!string.IsNullOrWhiteSpace(rule.Translation), $"rule '{rule.Id}' has no translation of its name");
                Require(rule.Examples.Count > 0, $"rule '{rule.Id}' has no examples");

                // Счёт по умолчанию — один из допустимых: его конвейер берёт, пока
                // человек не выбрал свой, и показан он будет выбранным.
                Require(rule.Counts.SequenceEqual(rule.Counts.Distinct().Order()),
                        $"rule '{rule.Id}' lists its counts out of order or twice");
                Require(rule.HasVariants ? rule.Counts.Contains(rule.DefaultCount) : rule.DefaultCount == 0,
                        rule.HasVariants
                            ? $"rule '{rule.Id}' has a default count outside its counts"
                            : $"rule '{rule.Id}' has a default count but no counts");

                // Выключить по одному можно только правило таджвида (I4): правило основы
                // есть в любом чтении, и выключенное читалось бы так же, как включённое.
                Require(!rule.Switchable || rule.Tier == TajweedTier.Tajweed,
                        $"rule '{rule.Id}' is switchable but not in the tajweed tier");
            }
        }
    }
}
