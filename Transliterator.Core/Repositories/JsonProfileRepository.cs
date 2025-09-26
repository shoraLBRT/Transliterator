using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Transliterator.Core.Models;
using Transliterator.Domain.Entities;
using Transliterator.Domain.Interfaces;

namespace Transliterator.Core.Repositories
{
    public class JsonProfileRepository : IProfileRepository
    {
        private readonly string _storagePath;
        private readonly ILogger<JsonProfileRepository> _logger;
        private readonly Dictionary<string, TransliterationProfile> _cache = new();

        public JsonProfileRepository(IOptions<StorageSettings> options, ILogger<JsonProfileRepository> logger)
        {
            _logger = logger;
            _storagePath = options.Value.ProfilesPath;
            EnsureStorageDirectoryExists();
            LoadProfilesInCache();
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
                _logger.LogError($"Error loading profiles into cache");
            }
        }

        public async Task<TransliterationProfile?> GetProfileAsync(string profileName)
        {
            if (_cache.TryGetValue(profileName, out var profile))
                return profile;

            var filePath = Path.Combine(_storagePath, profileName);

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
                _logger.LogError($"Error loading profile {profileName}");
                return null;
            }
        }

        public async Task<IEnumerable<TransliterationProfile>> GetAllProfilesAsync()
        {
            var files = Directory.GetFiles(_storagePath, "*.json");
            var profiles = new List<TransliterationProfile>();

            foreach (var file in files)
            {
                var profileName = Path.GetFileName(file);
                var profile = await GetProfileAsync(profileName);
                if (profile != null)
                    profiles.Add(profile);
            }

            return profiles;
        }

        public async Task SaveProfileAsync(TransliterationProfile profile)
        {
            try
            {
                var filePath = Path.Combine(_storagePath, $"{profile.Name}.json");
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(profile, options);

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
