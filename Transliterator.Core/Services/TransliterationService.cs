using Transliterator.Core.Services.Phonology;
using Transliterator.Core.Services.Rules;
using Transliterator.Domain.Entities;
using Transliterator.Domain.Exceptions;
using Transliterator.Domain.Interfaces;

/// <summary>
/// Конвейер транслитерации: орфография → фонология → правила таджвида → письмо.
/// <para>
/// Правила работают с потоком сегментов, а не с готовой кириллицей. Обратный порядок
/// невозможен: к моменту побуквенной замены сукун, шадда, тип хамзы и границы слов
/// уже потеряны, а именно они и нужны таджвиду.
/// </para>
/// </summary>
public class TransliterationService : ITransliterationService
{
    private const string DefaultProfileName = "Standard";

    private readonly IProfileRepository _profileRepository;
    private readonly ArabicNormalizer _normalizer;
    private readonly ArabicParser _parser;
    private readonly RulesService _rulesService;
    private readonly CyrillicRenderer _renderer;

    public TransliterationService(
        IProfileRepository profileRepository,
        ArabicNormalizer normalizer,
        ArabicParser parser,
        RulesService rulesService,
        CyrillicRenderer renderer)
    {
        _profileRepository = profileRepository;
        _normalizer = normalizer;
        _parser = parser;
        _rulesService = rulesService;
        _renderer = renderer;
    }

    public async Task<TransliterationResult> TransliterateAsync(string arabicText, string? selectedProfile = null)
    {
        var profileName = ResolveProfileName(selectedProfile);
        var profile = await LoadProfileAsync(profileName);

        ValidateProfile(profile);

        return new TransliterationResult(arabicText, Run(arabicText, profile), profileName);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Проверка та же, что у профиля из хранилища: откуда профиль взялся, для
    /// конвейера разницы нет. Разница только в том, что здесь его никто до этого
    /// не читал, — пользователь мог стереть в редакторе что угодно.
    /// </remarks>
    public TransliterationResult Transliterate(string arabicText, TransliterationProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        ValidateProfile(profile);

        return new TransliterationResult(arabicText ?? string.Empty, Run(arabicText, profile), profile.Name);
    }

    private string Run(string? arabicText, TransliterationProfile profile)
    {
        if (string.IsNullOrWhiteSpace(arabicText))
            return string.Empty;

        var normalized = _normalizer.Normalize(arabicText);
        var segments = _parser.Parse(normalized);

        _rulesService.ApplyTajweedRules(segments);

        var missingKeys = new SortedSet<string>(StringComparer.Ordinal);
        var text = _renderer.Render(segments, profile, missingKeys);

        if (missingKeys.Count > 0)
            throw new TransliterationException(
                $"Profile '{profile.Name}' has no rule for {string.Join(", ", missingKeys.Select(DescribeKey))}");

        return text;
    }

    /// <summary>
    /// Структура профиля, без которой считать нечего. Полнота правил здесь
    /// не проверяется: какие ключи нужны, зависит от текста, и знает это рендерер.
    /// </summary>
    private static void ValidateProfile(TransliterationProfile profile)
    {
        if (string.IsNullOrWhiteSpace(profile.Name))
            throw new TransliterationException("Profile name must not be empty");

        if (profile.Rules is null || profile.Rules.Count == 0)
            throw new TransliterationException($"Profile '{profile.Name}' has no rules");

        foreach (var (key, value) in profile.Rules)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new TransliterationException($"Profile '{profile.Name}' has a rule with an empty key");

            // null приходит из JSON ("ب": null) и отличается от пустой строки:
            // пустая — осознанно немая графема, null — незаполненное поле.
            if (value is null)
                throw new TransliterationException(
                    $"Profile '{profile.Name}': rule {DescribeKey(key)} has no value; " +
                    "use an empty string for a grapheme that is deliberately not written");
        }
    }

    /// <summary>
    /// Ключ с кодами символов: огласовка без буквы в сообщении не видна вовсе,
    /// а арабская буква в консоли с другой кодировкой превращается в «?».
    /// </summary>
    private static string DescribeKey(string key) =>
        $"'{key}' ({string.Join(" ", key.Select(c => $"U+{(int)c:X4}"))})";

    /// <summary>
    /// Правка одной строки профиля.
    /// <para>
    /// Ключ — тот же, что в самом профиле: «буква» либо «буква|вариант»
    /// (<c>"ر|heavy"</c>, <c>"ة|waqf"</c>). Проверять ключ по алфавиту нельзя:
    /// вариантов у правил больше, чем букв, и их набор задаёт профиль, а не код.
    /// Пустое значение — законная запись: так в <c>Standard</c> заданы отзвук
    /// кальканя и начальная хамза.
    /// </para>
    /// </summary>
    public async Task UpdateRuleAsync(string arabicLetter, string cyrillicMapping, string? profile = null)
    {
        if (string.IsNullOrWhiteSpace(arabicLetter))
            throw new TransliterationException("Rule key must not be empty");

        var profileName = ResolveProfileName(profile);
        var target = await LoadProfileAsync(profileName);

        target.Rules[arabicLetter] = cyrillicMapping ?? string.Empty;

        await _profileRepository.SaveProfileAsync(target);
    }

    /// <summary>Имена профилей, доступных хранилищу, по алфавиту.</summary>
    public async Task<IEnumerable<string>> GetAvailableProfilesAsync()
    {
        var profiles = await _profileRepository.GetAllProfilesAsync();

        return profiles.Select(p => p.Name)
                       .Where(name => !string.IsNullOrWhiteSpace(name))
                       .Distinct(StringComparer.Ordinal)
                       .OrderBy(name => name, StringComparer.Ordinal)
                       .ToList();
    }

    /// <summary>Правила профиля — копией.</summary>
    /// <remarks>
    /// Именно копией: репозиторий отдаёт профили из кеша, и правка возвращённого
    /// словаря молча меняла бы профиль для всех, кто его уже держит.
    /// </remarks>
    public async Task<Dictionary<string, string>> GetRulesAsync(string? profile = null)
    {
        var target = await LoadProfileAsync(ResolveProfileName(profile));

        return new Dictionary<string, string>(target.Rules);
    }

    private static string ResolveProfileName(string? profile) =>
        string.IsNullOrWhiteSpace(profile) ? DefaultProfileName : profile;

    private async Task<TransliterationProfile> LoadProfileAsync(string profileName) =>
        await _profileRepository.GetProfileAsync(profileName)
        ?? throw new TransliterationException($"Profile '{profileName}' not found");
}
