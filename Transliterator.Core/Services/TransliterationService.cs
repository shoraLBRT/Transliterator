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
        var profileName = string.IsNullOrWhiteSpace(selectedProfile) ? "Standard" : selectedProfile;

        var profile = await _profileRepository.GetProfileAsync(profileName)
                      ?? throw new TransliterationException($"Profile '{profileName}' not found");

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

    // TODO
    public Task UpdateRuleAsync(string arabicLetter, string cyrillicMapping, string? profile = null)
    {
        throw new NotImplementedException();
    }

    // TODO
    public Task<IEnumerable<string>> GetAvailableProfilesAsync()
    {
        throw new NotImplementedException();
    }

    // TODO
    public Task<Dictionary<string, string>> GetRulesAsync(string? profile = null)
    {
        throw new NotImplementedException();
    }
}
