using Transliterator.Core.Repositories;
using Transliterator.Domain.Entities;
using Transliterator.Domain.Exceptions;

namespace Transliterator.Core.Services
{
    /// <summary>
    /// Правка профилей пользователя (E2): копия, переименование, удаление и правка
    /// графемы. Встроенные профили здесь только источник копии — менять, переименовывать
    /// и удалять их нельзя.
    /// </summary>
    /// <remarks>
    /// Сообщения о неподходящем имени написаны для пользователя, а не для журнала:
    /// редактор показывает их прямо под полем имени.
    /// </remarks>
    public class ProfileEditor
    {
        private readonly UserProfileRepository _profiles;

        public ProfileEditor(UserProfileRepository profiles) => _profiles = profiles;

        /// <summary>Свободное имя для копии: «Standard (копия)», «Standard (копия 2)».</summary>
        public Task<string> SuggestCopyNameAsync(string sourceName) => _profiles.FreeCopyNameAsync(sourceName);

        /// <summary>
        /// <c>null</c> — имя годится; иначе — почему нет. Имя сравнивается без пробелов
        /// по краям и без учёта регистра: «мой» рядом с «Мой» в списке неотличимы
        /// на глаз, и выбрать из них нужный было бы нечем.
        /// </summary>
        /// <param name="renaming">Имя профиля, который переименовывают: с самим собой он не совпадает.</param>
        public async Task<string?> CheckNameAsync(string? name, string? renaming = null)
        {
            var trimmed = name?.Trim();

            if (string.IsNullOrEmpty(trimmed))
                return "Имя профиля не может быть пустым.";

            if (renaming is not null && trimmed == renaming)
                return "Имя не изменилось.";

            var taken = (await _profiles.GetAllProfilesAsync())
                .FirstOrDefault(p => p.Name != renaming && string.Equals(p.Name, trimmed, StringComparison.OrdinalIgnoreCase));

            return taken is null ? null : $"Профиль «{taken.Name}» уже есть.";
        }

        /// <summary>Новый профиль — копия существующего, встроенного или своего.</summary>
        /// <exception cref="TransliterationException">Имя не годится или источника нет.</exception>
        public async Task<TransliterationProfile> CreateCopyAsync(string sourceName, string newName)
        {
            var name = await RequireNameAsync(newName);
            var source = await _profiles.GetProfileAsync(sourceName)
                ?? throw new TransliterationException($"Профиль «{sourceName}» не найден.");

            var copy = new TransliterationProfile(name, source.Description)
            {
                Rules = new Dictionary<string, string>(source.Rules)
            };

            await _profiles.SaveAsync(copy);
            return copy;
        }

        /// <summary>
        /// Переименование — запись под новым именем и удаление старой. Сначала запись:
        /// не удалось сохранить — старый профиль остался на месте.
        /// </summary>
        /// <exception cref="TransliterationException">Профиль встроенный, имя не годится или запись не удалась.</exception>
        public async Task<TransliterationProfile> RenameAsync(string oldName, string newName)
        {
            var profile = await RequireUserProfileAsync(oldName);
            var name = await RequireNameAsync(newName, renaming: oldName);

            profile.Name = name;
            await _profiles.SaveAsync(profile);
            await _profiles.DeleteProfileAsync(oldName);

            return profile;
        }

        /// <exception cref="TransliterationException">Профиль встроенный или его нет.</exception>
        public async Task DeleteAsync(string name)
        {
            await RequireUserProfileAsync(name);
            await _profiles.DeleteProfileAsync(name);
        }

        /// <summary>
        /// Меняет графему в профиле и сохраняет его. Словарь меняется до записи:
        /// правка видна на тексте сразу, даже если записать её не удалось, — и тогда
        /// об этом говорит исключение, а сохранённая версия остаётся прежней.
        /// </summary>
        /// <exception cref="TransliterationException">Профиль встроенный или запись не удалась.</exception>
        public async Task SetRuleAsync(TransliterationProfile profile, string key, string? value)
        {
            ArgumentNullException.ThrowIfNull(profile);

            if (await _profiles.IsBuiltInAsync(profile.Name))
                throw new TransliterationException(
                    $"Встроенный профиль «{profile.Name}» не редактируется: создайте копию.");

            profile.Rules[key] = value ?? string.Empty;
            await _profiles.SaveAsync(profile);
        }

        private async Task<string> RequireNameAsync(string? name, string? renaming = null)
        {
            if (await CheckNameAsync(name, renaming) is { } problem)
                throw new TransliterationException(problem);

            return name!.Trim();
        }

        private async Task<TransliterationProfile> RequireUserProfileAsync(string name)
        {
            if (await _profiles.IsBuiltInAsync(name))
                throw new TransliterationException($"Встроенный профиль «{name}» нельзя изменить или удалить.");

            return await _profiles.GetProfileAsync(name)
                ?? throw new TransliterationException($"Профиль «{name}» не найден.");
        }
    }
}
