using Transliterator.Core.Localization;
using Microsoft.Extensions.Logging;
using System.Text.Encodings.Web;
using System.Text.Json;
using Transliterator.Domain.Entities;
using Transliterator.Domain.Exceptions;
using Transliterator.Domain.Interfaces;

namespace Transliterator.Core.Repositories
{
    /// <summary>
    /// Профили пользователя поверх строкового хранилища (localStorage в браузере)
    /// вместе со встроенными — одним списком (E1).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Каждый профиль — отдельный ключ <c>transliterator.profiles.&lt;имя&gt;</c>,
    /// а не один общий. Так порча одной записи теряет один профиль, а не все,
    /// и запись, не поместившаяся в квоту, не трогает остальные.
    /// </para>
    /// <para>
    /// Встроенный профиль не перезаписывается никогда: запись под его именем
    /// сохраняет копию «Cyrillic (копия)», и сам встроенный профиль остаётся
    /// эталоном. Отдаются профили копиями — правка возвращённого словаря
    /// не должна молча менять встроенный профиль всей странице.
    /// </para>
    /// <para>
    /// Свой профиль всё же может оказаться под именем встроенного: так бывает,
    /// когда встроенный переименован (E8: <c>Standard</c> стал <c>Cyrillic</c>),
    /// а у человека уже был свой «Cyrillic». Такая запись не пропадает и не
    /// путается со встроенным: она переезжает под свободное имя копии,
    /// «Cyrillic (копия)».
    /// </para>
    /// <para>
    /// Битая запись — не сломанная сборка, как у ресурсов, а данные, которые мог
    /// испортить кто угодно: пользователь в инструментах разработчика, чужой скрипт
    /// на том же адресе. Поэтому она пропускается, а не роняет чтение, и причина
    /// попадает в <see cref="Problems"/> — показать её пользователю.
    /// </para>
    /// </remarks>
    public class UserProfileRepository : IProfileRepository
    {
        /// <summary>Префикс ключей профилей. Остальные ключи хранилища — чужие и не читаются.</summary>
        public const string KeyPrefix = "transliterator.profiles.";

        /// <summary>Арабица и кириллица пишутся как есть, как и в файлах профилей.</summary>
        private static readonly JsonSerializerOptions _json = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        private readonly IProfileRepository _builtIns;
        private readonly IKeyValueStore _store;
        private readonly ILogger<UserProfileRepository> _logger;
        private readonly List<string> _problems = new();
        private readonly Dictionary<string, string> _moved = new(StringComparer.Ordinal);

        public UserProfileRepository(IProfileRepository builtIns, IKeyValueStore store, ILogger<UserProfileRepository> logger)
        {
            _builtIns = builtIns;
            _store = store;
            _logger = logger;
        }

        /// <summary>
        /// Что пришлось пропустить при последнем чтении списка и после него:
        /// битые записи и недоступное хранилище. Сообщения — для пользователя.
        /// </summary>
        public IReadOnlyList<string> Problems => _problems;

        public static string KeyFor(string profileName) => KeyPrefix + profileName;

        public Task<bool> IsBuiltInAsync(string profileName) => _builtIns.ProfileExistsAsync(profileName);

        public async Task<TransliterationProfile?> GetProfileAsync(string profileName)
        {
            if (await _builtIns.GetProfileAsync(profileName) is { } builtIn)
                return Copy(builtIn);

            return await ReadAsync(KeyFor(profileName));
        }

        /// <summary>Сначала встроенные, за ними свои по имени.</summary>
        public async Task<IEnumerable<TransliterationProfile>> GetAllProfilesAsync()
        {
            _problems.Clear();

            var profiles = (await _builtIns.GetAllProfilesAsync()).Select(Copy).ToList();
            var builtInNames = profiles.Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
            var own = new List<TransliterationProfile>();

            foreach (var key in await UserKeysAsync())
            {
                if (await ReadAsync(key) is not { } profile)
                    continue;

                // Встроенный остаётся встроенным, а свой профиль с его именем
                // переезжает под свободное имя копии.
                if (builtInNames.Contains(profile.Name))
                {
                    if (await MoveAsync(key, profile) is not { } moved)
                        continue;

                    profile = moved;
                }

                own.Add(profile);
            }

            return profiles.Concat(own.OrderBy(p => p.Name, StringComparer.Ordinal));
        }

        /// <summary>
        /// Переносит свои профили, чьё имя занято встроенным, под свободное имя
        /// копии (E8) и возвращает все переезды, сделанные этим хранилищем: прежнее
        /// имя → новое. По ним страница поправляет сохранённый выбор профиля:
        /// кто выбрал свой «Cyrillic», тот остаётся на нём, а не на встроенном.
        /// </summary>
        public async Task<IReadOnlyDictionary<string, string>> MoveShadowedAsync()
        {
            foreach (var key in await UserKeysAsync())
            {
                if (!await _builtIns.ProfileExistsAsync(key[KeyPrefix.Length..]))
                    continue;

                if (await ReadAsync(key) is { } profile)
                    await MoveAsync(key, profile);
            }

            return _moved;
        }

        /// <summary>
        /// Свой профиль под именем встроенного — под свободное имя копии. Старая
        /// запись удаляется только после того, как записана новая; не вышло —
        /// запись остаётся как есть, а профиль пропускается.
        /// </summary>
        private async Task<TransliterationProfile?> MoveAsync(string key, TransliterationProfile profile)
        {
            var name = await FreeCopyNameAsync(profile.Name);
            var moved = new TransliterationProfile(name, profile.Description)
            {
                Rules = new Dictionary<string, string>(profile.Rules)
            };

            try
            {
                await _store.SetAsync(KeyFor(name), JsonSerializer.Serialize(moved, _json));
                await _store.RemoveAsync(key);
            }
            catch (Exception ex) when (ex is not TransliterationException)
            {
                _logger.LogWarning(ex, "Stored profile could not be moved: {Key}", key);
                Report(key, "имя занято встроенным профилем, а перенести его под другое не удалось");
                return null;
            }

            _moved[profile.Name] = name;
            _logger.LogInformation("Stored profile {Old} moved to {New}: the name is taken by a built-in profile",
                                   profile.Name, name);
            return moved;
        }

        public async Task<bool> ProfileExistsAsync(string profileName)
        {
            if (await _builtIns.ProfileExistsAsync(profileName))
                return true;

            try
            {
                return await _store.GetAsync(KeyFor(profileName)) is not null;
            }
            catch (Exception ex) when (ex is not TransliterationException)
            {
                _logger.LogWarning(ex, "Profile storage is unavailable");
                return false;
            }
        }

        public async Task SaveProfileAsync(TransliterationProfile profile) => await SaveAsync(profile);

        /// <summary>
        /// Сохраняет профиль и возвращает имя, под которым он сохранён. Оно отличается
        /// от имени профиля, только если это имя встроенного: тогда сохраняется копия.
        /// </summary>
        /// <exception cref="TransliterationException">
        /// Имя пустое, места в хранилище не хватило или хранилище недоступно.
        /// Сохранённая раньше версия профиля в любом из этих случаев остаётся.
        /// </exception>
        public async Task<string> SaveAsync(TransliterationProfile profile)
        {
            ArgumentNullException.ThrowIfNull(profile);

            if (string.IsNullOrWhiteSpace(profile.Name))
                throw new TransliterationException("Profile name must not be empty");

            var name = await _builtIns.ProfileExistsAsync(profile.Name)
                ? await FreeCopyNameAsync(profile.Name)
                : profile.Name;

            var stored = new TransliterationProfile(name, profile.Description)
            {
                Rules = new Dictionary<string, string>(profile.Rules ?? new Dictionary<string, string>())
            };

            try
            {
                await _store.SetAsync(KeyFor(name), JsonSerializer.Serialize(stored, _json));
            }
            catch (StorageQuotaExceededException ex)
            {
                throw new TransliterationException(CoreText.Format("Profile.NotSavedFull", name), ex);
            }
            catch (Exception ex) when (ex is not TransliterationException)
            {
                throw new TransliterationException(CoreText.Format("Profile.NotSavedUnavailable", name), ex);
            }

            _logger.LogInformation("Profile saved to browser storage: {Profile}", name);
            return name;
        }

        /// <exception cref="TransliterationException">Профиль встроенный или хранилище недоступно.</exception>
        public async Task DeleteProfileAsync(string profileName)
        {
            if (await _builtIns.ProfileExistsAsync(profileName))
                throw new TransliterationException($"Profile '{profileName}' is built in and cannot be deleted.");

            try
            {
                await _store.RemoveAsync(KeyFor(profileName));
            }
            catch (Exception ex) when (ex is not TransliterationException)
            {
                throw new TransliterationException(CoreText.Format("Profile.NotDeletedUnavailable", profileName), ex);
            }
        }

        /// <summary>«Cyrillic (копия)», затем «Cyrillic (копия 2)» — первое свободное имя.</summary>
        public async Task<string> FreeCopyNameAsync(string name)
        {
            var copySuffix = CoreText.Get("Profile.CopySuffix");
            var candidate = $"{name} ({copySuffix})";

            for (int n = 2; await ProfileExistsAsync(candidate); n++)
                candidate = $"{name} ({copySuffix} {n})";

            return candidate;
        }

        private async Task<IReadOnlyList<string>> UserKeysAsync()
        {
            try
            {
                return (await _store.GetKeysAsync())
                    .Where(key => key.StartsWith(KeyPrefix, StringComparison.Ordinal))
                    .OrderBy(key => key, StringComparer.Ordinal)
                    .ToList();
            }
            catch (Exception ex) when (ex is not TransliterationException)
            {
                _logger.LogWarning(ex, "Profile storage is unavailable");
                AddProblem("Хранилище браузера недоступно: свои профили не прочитаны, показаны только встроенные.");
                return Array.Empty<string>();
            }
        }

        /// <summary>Профиль по ключу; <c>null</c> — ключа нет или запись битая.</summary>
        private async Task<TransliterationProfile?> ReadAsync(string key)
        {
            string? json;

            try
            {
                json = await _store.GetAsync(key);
            }
            catch (Exception ex) when (ex is not TransliterationException)
            {
                _logger.LogWarning(ex, "Profile storage is unavailable");
                AddProblem("Хранилище браузера недоступно: свои профили не прочитаны, показаны только встроенные.");
                return null;
            }

            if (json is null)
                return null;

            TransliterationProfile? profile;

            try
            {
                profile = JsonSerializer.Deserialize<TransliterationProfile>(json);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Stored profile is not valid JSON: {Key}", key);
                Report(key, "запись не читается как JSON");
                return null;
            }

            if (profile is null)
            {
                Report(key, "запись пуста");
                return null;
            }

            // Имя в записи и имя в ключе должны совпадать: иначе профиль, выбранный
            // по одному имени, сохранялся бы под другим.
            if (profile.Name != key[KeyPrefix.Length..])
            {
                Report(key, $"имя в записи «{profile.Name}» не совпадает с ключом");
                return null;
            }

            if (profile.Rules is null)
            {
                Report(key, "в записи нет правил");
                return null;
            }

            return profile;
        }

        private void Report(string key, string reason) =>
            AddProblem($"Профиль «{key[KeyPrefix.Length..]}» из хранилища браузера пропущен: {reason}.");

        private void AddProblem(string message)
        {
            if (!_problems.Contains(message))
                _problems.Add(message);
        }

        private static TransliterationProfile Copy(TransliterationProfile profile) =>
            new(profile.Name, profile.Description)
            {
                Rules = new Dictionary<string, string>(profile.Rules)
            };
    }
}
