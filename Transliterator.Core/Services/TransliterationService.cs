using Transliterator.Core.Services.Rules;
using Transliterator.Domain.Entities;
using Transliterator.Domain.Interfaces;

public class TransliterationService : ITransliterationService
{
    private readonly IProfileRepository _profileRepository;
    private readonly LetterChangeService _letterChangeService;
    private readonly RulesService _rulesService;
    public TransliterationService(IProfileRepository profileRepository, LetterChangeService letterChangeService, RulesService rulesService)
    {
        _profileRepository = profileRepository;
        _letterChangeService = letterChangeService;
        _rulesService = rulesService;
    }

    public async Task<TransliterationResult> TransliterateAsync(string arabicText, string selectedProfile = "Standard")
    {
        var profile = await _profileRepository.GetProfileAsync(selectedProfile);

        if (profile == null)
            throw new Exception($"Profile '{selectedProfile}' not found");

        var resultText = _letterChangeService.TransliterateLetters(arabicText, profile);

        resultText = await _rulesService.ApplyTajweedRulesAsync(resultText);

        return new TransliterationResult(arabicText, resultText, selectedProfile);
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