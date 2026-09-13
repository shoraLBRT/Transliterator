using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Transliterator.Core.Services.Phonology;
using Transliterator.Domain.Entities;

namespace Transliterator.Core.Services
{
    /// <summary>
    /// Разбор профиля из файла.
    /// </summary>
    /// <param name="Profile">Профиль; <c>null</c>, если в файле что-то не так.</param>
    /// <param name="Problems">Всё, что не так, для пользователя; пусто, если профиль разобран.</param>
    /// <param name="RenamedFrom">
    /// Имя из файла, если профиль сохранён под другим: имя было занято.
    /// </param>
    public sealed record ProfileImport(
        TransliterationProfile? Profile,
        IReadOnlyList<string> Problems,
        string? RenamedFrom = null);

    /// <summary>
    /// Профиль в JSON и обратно (E3). Формат — тот же, что у <c>Standard.json</c>:
    /// выгруженный из браузера профиль кладётся в <c>Resources/Profiles</c>
    /// и читается <c>JsonProfileRepository</c> без правок.
    /// </summary>
    public static class ProfileJson
    {
        /// <summary>Больше профиль быть не может: в <c>Standard</c> пара килобайт.</summary>
        public const int MaxLength = 1024 * 1024;

        private const string NameField = nameof(TransliterationProfile.Name);
        private const string DescriptionField = nameof(TransliterationProfile.Description);
        private const string RulesField = nameof(TransliterationProfile.Rules);

        private static readonly string[] Fields = { NameField, DescriptionField, RulesField };

        /// <summary>Те же настройки, что у записи файла профиля: отступы, арабица и кириллица как есть.</summary>
        private static readonly JsonSerializerOptions _writeOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public static string Export(TransliterationProfile profile)
        {
            ArgumentNullException.ThrowIfNull(profile);

            var copy = new TransliterationProfile(profile.Name, profile.Description)
            {
                Rules = new Dictionary<string, string>(profile.Rules)
            };

            return JsonSerializer.Serialize(copy, _writeOptions);
        }

        /// <summary>
        /// Имя файла для профиля. Файловое хранилище ищет профиль по имени файла,
        /// поэтому оно совпадает с именем профиля, пока в нём нет знаков, которые
        /// файловая система не примет: они заменяются подчёркиванием.
        /// </summary>
        public static string FileNameFor(string profileName)
        {
            var name = new StringBuilder(profileName.Length);

            foreach (var c in profileName)
                name.Append(char.IsControl(c) || "<>:\"/\\|?*".Contains(c) ? '_' : c);

            var trimmed = name.ToString().Trim().TrimEnd('.');

            return (trimmed.Length == 0 ? "profile" : trimmed) + ".json";
        }

        /// <summary>
        /// Разбирает профиль и сообщает обо всём, что не так, разом, а не о первом
        /// попавшемся: чинить файл по одной ошибке за попытку никто не станет.
        /// Строгость — та же, что у чтения файла профиля: без комментариев и лишних
        /// запятых, имена полей с учётом регистра. Принятое здесь хранилище
        /// прочитает, а отвергнутое — прочитало бы не так, как задумано.
        /// </summary>
        public static ProfileImport Import(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return Fail("Файл пуст.");

            if (json.Length > MaxLength)
                return Fail("Файл слишком большой для профиля.");

            JsonDocument document;

            try
            {
                document = JsonDocument.Parse(json);
            }
            catch (JsonException ex)
            {
                return Fail($"Файл не читается как JSON: строка {(ex.LineNumber ?? 0) + 1}, " +
                            $"позиция {(ex.BytePositionInLine ?? 0) + 1}.");
            }

            using (document)
            {
                var root = document.RootElement;

                if (root.ValueKind != JsonValueKind.Object)
                    return Fail($"Профиль должен быть объектом JSON с полями {NameField}, {DescriptionField} и {RulesField}.");

                var problems = new List<string>();

                CheckFields(root, problems);

                var name = ReadString(root, NameField, required: true, problems);
                var description = ReadString(root, DescriptionField, required: false, problems);
                var rules = ReadRules(root, problems);

                if (name is not null && string.IsNullOrWhiteSpace(name))
                    problems.Add($"Имя профиля ({NameField}) пустое.");

                if (problems.Count > 0)
                    return new ProfileImport(null, problems);

                var profile = new TransliterationProfile(name!.Trim(), description ?? string.Empty)
                {
                    Rules = rules!
                };

                return new ProfileImport(profile, Array.Empty<string>());
            }
        }

        private static ProfileImport Fail(string problem) => new(null, new[] { problem });

        private static void CheckFields(JsonElement root, List<string> problems)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (var property in root.EnumerateObject())
            {
                if (!seen.Add(property.Name))
                {
                    problems.Add($"Поле «{property.Name}» задано дважды.");
                    continue;
                }

                if (Fields.Contains(property.Name))
                    continue;

                var meant = Fields.FirstOrDefault(f => string.Equals(f, property.Name, StringComparison.OrdinalIgnoreCase));

                problems.Add(meant is not null
                    ? $"Поле «{property.Name}» надо писать как «{meant}»: регистр в именах полей важен."
                    : $"Неизвестное поле «{property.Name}».");
            }
        }

        private static string? ReadString(JsonElement root, string field, bool required, List<string> problems)
        {
            if (!root.TryGetProperty(field, out var value))
            {
                if (required)
                    problems.Add($"Нет поля «{field}».");
                return null;
            }

            if (value.ValueKind == JsonValueKind.String)
                return value.GetString();

            problems.Add($"Поле «{field}» должно быть строкой.");
            return null;
        }

        private static Dictionary<string, string>? ReadRules(JsonElement root, List<string> problems)
        {
            if (!root.TryGetProperty(RulesField, out var element))
            {
                problems.Add($"Нет поля «{RulesField}».");
                return null;
            }

            if (element.ValueKind != JsonValueKind.Object)
            {
                problems.Add($"Поле «{RulesField}» должно быть объектом «ключ: графема».");
                return null;
            }

            var rules = new Dictionary<string, string>(StringComparer.Ordinal);
            var before = problems.Count;

            foreach (var rule in element.EnumerateObject())
            {
                var key = rule.Name;

                if (string.IsNullOrWhiteSpace(key))
                {
                    problems.Add($"Пустой ключ в «{RulesField}».");
                    continue;
                }

                if (rules.ContainsKey(key))
                {
                    problems.Add($"Ключ {Describe(key)} задан дважды.");
                    continue;
                }

                switch (rule.Value.ValueKind)
                {
                    case JsonValueKind.String:
                        rules[key] = rule.Value.GetString()!;
                        break;

                    case JsonValueKind.Null:
                        problems.Add($"У ключа {Describe(key)} нет графемы (null). Графему, которая не пишется, задают пустой строкой.");
                        break;

                    default:
                        problems.Add($"Графема ключа {Describe(key)} должна быть строкой.");
                        break;
                }
            }

            if (element.EnumerateObject().Any() == false)
                problems.Add("В профиле нет ни одного правила.");

            foreach (var key in rules.Keys.Where(k => k.Contains(RuleTable.VariantSeparator)))
            {
                var separator = key.IndexOf(RuleTable.VariantSeparator);
                var baseKey = key[..separator];
                var variant = key[(separator + 1)..];

                if (!CyrillicRenderer.KnownVariants.Contains(variant))
                    problems.Add($"Неизвестный вариант «{variant}» у ключа {Describe(key)}. " +
                                 $"Известны: {string.Join(", ", CyrillicRenderer.KnownVariants.Order(StringComparer.Ordinal))}.");

                if (!rules.ContainsKey(baseKey))
                    problems.Add($"У варианта {Describe(key)} нет базового ключа {Describe(baseKey)}: откатываться ему некуда.");
            }

            return problems.Count == before ? rules : null;
        }

        /// <summary>Ключ с кодами символов: огласовка без буквы в сообщении иначе не видна.</summary>
        private static string Describe(string key) =>
            $"«{key}» ({string.Join(" ", key.Select(c => $"U+{(int)c:X4}"))})";
    }
}
