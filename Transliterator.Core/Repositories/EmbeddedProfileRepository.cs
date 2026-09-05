using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text.Json;
using Transliterator.Domain.Entities;
using Transliterator.Domain.Exceptions;
using Transliterator.Domain.Interfaces;

namespace Transliterator.Core.Repositories
{
    /// <summary>
    /// Встроенные профили из ресурсов сборки. Файловой системы не касается вовсе
    /// и потому работает там, где её нет, — в браузере (Blazor WebAssembly).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Хранилище разнесено на две независимые вещи: <b>где лежат профили</b> и
    /// <b>что с ними делают</b>. Здесь профили лежат внутри сборки, и это делает
    /// хранилище <b>read-only</b>: ресурс сборки нельзя переписать в рантайме
    /// ни в браузере, ни на диске. Запись — дело другой реализации
    /// <see cref="IProfileRepository"/> (localStorage в браузере, файлы в CLI).
    /// </para>
    /// <para>
    /// Битый ресурс роняет чтение с внятным сообщением, а не пропускается:
    /// ресурсы фиксируются на сборке, руками их после этого никто не портит,
    /// и битый профиль здесь — сломанная сборка, а не сломанные данные.
    /// В <see cref="JsonProfileRepository"/> причина пропускать была
    /// (профиль правят руками рядом со сборкой), здесь её нет.
    /// </para>
    /// </remarks>
    public class EmbeddedProfileRepository : IProfileRepository
    {
        /// <summary>
        /// Имя ресурса собирается компилятором из корневого пространства имён
        /// и пути к файлу: <c>Resources\Profiles\Standard.json</c> превращается
        /// в <c>Transliterator.Core.Resources.Profiles.Standard.json</c>.
        /// </summary>
        public const string DefaultResourcePrefix = "Transliterator.Core.Resources.Profiles.";

        private const string ResourceSuffix = ".json";

        private readonly Assembly _assembly;
        private readonly string _resourcePrefix;
        private readonly Dictionary<string, TransliterationProfile> _profiles;

        public EmbeddedProfileRepository(ILogger<EmbeddedProfileRepository> logger)
            : this(typeof(EmbeddedProfileRepository).Assembly, DefaultResourcePrefix, logger)
        {
        }

        /// <summary>
        /// Профили можно вшить и в чужую сборку — это тот самый шов «где лежат»:
        /// класс знает, как профиль читается, и не знает, откуда он взялся.
        /// </summary>
        public EmbeddedProfileRepository(Assembly assembly, string resourcePrefix, ILogger<EmbeddedProfileRepository> logger)
        {
            _assembly = assembly;
            _resourcePrefix = resourcePrefix;
            _profiles = Load();

            logger.LogInformation("Loaded {Count} embedded profiles from {Assembly}",
                                  _profiles.Count, assembly.GetName().Name);
        }

        /// <summary>
        /// Ресурсы читаются один раз при создании: их набор фиксирован на сборке
        /// и меняться не может, а поштучное чтение по имени файла — привычка
        /// файлового хранилища, здесь бессмысленная.
        /// </summary>
        private Dictionary<string, TransliterationProfile> Load()
        {
            var profiles = new Dictionary<string, TransliterationProfile>();

            foreach (var resourceName in ResourceNames())
            {
                var profile = Read(resourceName);

                if (profiles.ContainsKey(profile.Name))
                    throw new TransliterationException(
                        $"Embedded profile '{profile.Name}' is declared twice (resource {resourceName})");

                profiles[profile.Name] = profile;
            }

            return profiles;
        }

        private IEnumerable<string> ResourceNames() =>
            _assembly.GetManifestResourceNames()
                     .Where(name => name.StartsWith(_resourcePrefix, StringComparison.Ordinal)
                                 && name.EndsWith(ResourceSuffix, StringComparison.Ordinal))
                     .OrderBy(name => name, StringComparer.Ordinal);

        private TransliterationProfile Read(string resourceName)
        {
            using var stream = _assembly.GetManifestResourceStream(resourceName)
                ?? throw new TransliterationException($"Embedded profile resource is unreadable: {resourceName}");

            TransliterationProfile? profile;

            try
            {
                profile = JsonSerializer.Deserialize<TransliterationProfile>(stream);
            }
            catch (JsonException ex)
            {
                throw new TransliterationException($"Embedded profile is not valid JSON: {resourceName}", ex);
            }

            if (profile is null)
                throw new TransliterationException($"Embedded profile is empty: {resourceName}");

            if (string.IsNullOrWhiteSpace(profile.Name))
                throw new TransliterationException($"Embedded profile has no name: {resourceName}");

            return profile;
        }

        public Task<TransliterationProfile?> GetProfileAsync(string profileName) =>
            Task.FromResult(_profiles.GetValueOrDefault(profileName));

        public Task<IEnumerable<TransliterationProfile>> GetAllProfilesAsync() =>
            Task.FromResult<IEnumerable<TransliterationProfile>>(_profiles.Values.ToList());

        public Task<bool> ProfileExistsAsync(string profileName) =>
            Task.FromResult(_profiles.ContainsKey(profileName));

        /// <summary>
        /// Всегда бросает: ресурс сборки не переписывается в рантайме.
        /// </summary>
        /// <remarks>
        /// Отказ выдаётся сразу и от своего имени, а не приходит откуда-то
        /// из глубины: вызывающему не приходится гадать, что именно упало.
        /// Правка встроенного профиля — это создание копии в пользовательском
        /// хранилище, а не запись сюда.
        /// </remarks>
        /// <exception cref="TransliterationException">Всегда.</exception>
        public Task SaveProfileAsync(TransliterationProfile profile) =>
            throw new TransliterationException(
                $"Profile '{profile.Name}' cannot be saved: embedded profiles are read-only. " +
                "Save it to a writable IProfileRepository instead.");

        /// <inheritdoc cref="SaveProfileAsync"/>
        /// <exception cref="TransliterationException">Всегда.</exception>
        public Task DeleteProfileAsync(string profileName) =>
            throw new TransliterationException(
                $"Profile '{profileName}' cannot be deleted: embedded profiles are read-only.");
    }
}
