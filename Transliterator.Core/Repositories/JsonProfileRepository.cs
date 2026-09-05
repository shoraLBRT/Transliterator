using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Runtime.Versioning;
using System.Text.Encodings.Web;
using System.Text.Json;
using Transliterator.Core.Models;
using Transliterator.Domain.Entities;
using Transliterator.Domain.Interfaces;

namespace Transliterator.Core.Repositories
{
    /// <summary>
    /// Профили из каталога рядом со сборкой: файлы, которые правят руками.
    /// </summary>
    /// <remarks>
    /// В браузере файловой системы нет, и эта реализация там неприменима —
    /// отсюда <see cref="UnsupportedOSPlatformAttribute"/>. Атрибут не запрет,
    /// а проверка: ядро объявлено совместимым с browser, и обращение к этому
    /// классу из кода, который в браузере работает, ломает сборку (CA1416)
    /// вместо того, чтобы упасть на старте страницы. Встроенные профили
    /// в браузере читает <see cref="EmbeddedProfileRepository"/>.
    /// </remarks>
    [UnsupportedOSPlatform("browser")]
    public class JsonProfileRepository : IProfileRepository
    {
        private readonly string _storagePath;
        private readonly ILogger<JsonProfileRepository> _logger;
        private readonly Dictionary<string, TransliterationProfile> _cache = new();

        public JsonProfileRepository(IOptions<StorageSettings> options, ILogger<JsonProfileRepository> logger)
        {
            _logger = logger;
            _storagePath = ResolveStoragePath(options.Value.ProfilesPath);
            EnsureStorageDirectoryExists();
            LoadProfilesInCache();
        }

        /// <summary>
        /// Профили копируются рядом со сборкой, а рабочая папка при запуске через
        /// "dotnet run" — папка проекта. Относительный путь считаем от сборки.
        /// </summary>
        private static string ResolveStoragePath(string configuredPath)
        {
            if (Path.IsPathRooted(configuredPath))
                return configuredPath;

            return Path.Combine(AppContext.BaseDirectory, configuredPath);
        }

        private void EnsureStorageDirectoryExists()
        {
            if (!Directory.Exists(_storagePath))
            {
                Directory.CreateDirectory(_storagePath);
                _logger.LogInformation($"Created profiles directory: {_storagePath}");
            }
        }

        private void LoadProfilesInCache()
        {
            try
            {
                var files = Directory.GetFiles(_storagePath, "*.json");
                foreach (var file in files)
                {
                    var profileName = Path.GetFileNameWithoutExtension(file);
                    var json = File.ReadAllText(file);
                    var profile = JsonSerializer.Deserialize<TransliterationProfile>(json);

                    if (profile != null)
                    {
                        _cache[profile.Name] = profile;
                    }
                }
                _logger.LogInformation($"Loaded {_cache.Count} profiles into cache");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading profiles into cache from {Path}", _storagePath);
            }
        }

        public async Task<TransliterationProfile?> GetProfileAsync(string profileName)
        {
            if (_cache.TryGetValue(profileName, out var profile))
                return profile;

            var filePath = Path.Combine(_storagePath, profileName + ".json");

            if (!File.Exists(filePath))
                return null;

            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                var profileDesd = JsonSerializer.Deserialize<TransliterationProfile>(json);
                return profileDesd;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading profile {Profile}", profileName);
                return null;
            }
        }

        /// <summary>
        /// Профиль ищется по имени, а имя — это имя файла без расширения:
        /// <see cref="GetProfileAsync"/> сам дописывает ".json", и полное имя
        /// файла превращалось бы в "Standard.json.json".
        /// </summary>
        public async Task<IEnumerable<TransliterationProfile>> GetAllProfilesAsync()
        {
            var files = Directory.GetFiles(_storagePath, "*.json");
            var profiles = new List<TransliterationProfile>();

            foreach (var file in files)
            {
                var profileName = Path.GetFileNameWithoutExtension(file);
                var profile = await GetProfileAsync(profileName);
                if (profile != null)
                    profiles.Add(profile);
            }

            return profiles;
        }

        /// <summary>
        /// Профиль — файл, который читают и правят руками, поэтому арабица
        /// и кириллица пишутся как есть. Экранирование по умолчанию превратило бы
        /// весь профиль в "\u0631" при первой же записи.
        /// </summary>
        private static readonly JsonSerializerOptions _writeOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public async Task SaveProfileAsync(TransliterationProfile profile)
        {
            try
            {
                var filePath = Path.Combine(_storagePath, $"{profile.Name}.json");
                var json = JsonSerializer.Serialize(profile, _writeOptions);

                await File.WriteAllTextAsync(filePath, json);
                _cache[profile.Name] = profile;

                _logger.LogInformation("Profile saved: {Profile}", profile.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving profile {Profile}", profile.Name);
                throw;
            }
        }

        public async Task DeleteProfileAsync(string profileName)
        {
            var filePath = Path.Combine(_storagePath, $"{profileName}.json");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _cache.Remove(profileName);
                _logger.LogInformation("Profile deleted: {Profile}", profileName);
            }
        }

        public Task<bool> ProfileExistsAsync(string profileName)
        {
            var filePath = Path.Combine(_storagePath, $"{profileName}.json");
            return Task.FromResult(File.Exists(filePath) || _cache.ContainsKey(profileName));
        }
    }
}
