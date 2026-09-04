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

        var resultText = Transliterate(arabicText, profile);

        return new TransliterationResult(arabicText, resultText, profileName);
    }

    /// <summary>Синхронный конвейер без обращения к хранилищу — удобен для тестов.</summary>
    public string Transliterate(string arabicText, TransliterationProfile profile)
    {
        if (string.IsNullOrWhiteSpace(arabicText))
            return string.Empty;

        var normalized = _normalizer.Normalize(arabicText);
        var segments = _parser.Parse(normalized);

        _rulesService.ApplyTajweedRules(segments);

        return _renderer.Render(segments, profile);
    }

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
