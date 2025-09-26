using Transliterator.Core.Services;
using Transliterator.Domain.Entities;
using Transliterator.Domain.Interfaces;

public class TransliterationService : ITransliterationService
{
    private readonly IProfileRepository _profileRepository;
    private readonly LetterChangeService _letterChangeService;

    public TransliterationService(IProfileRepository profileRepository, LetterChangeService letterChangeService)
    {
        _profileRepository = profileRepository;
        _letterChangeService = letterChangeService;
    }

    public async Task<TransliterationResult> TransliterateAsync(string arabicText, string selectedProfile = "Standard")
    {
        var profile = await _profileRepository.GetProfileAsync(selectedProfile);

        if (profile == null)
            throw new Exception($"Profile '{selectedProfile}' not found");

        var resultText = _letterChangeService.TransliterateLetters(arabicText, profile);




        return new TransliterationResult(arabicText, resultText, selectedProfile);
    }

    public Task UpdateRuleAsync(string arabicLetter, string cyrillicMapping, string? profile = null)
    {
        throw new NotImplementedException();
    }
    public Task<IEnumerable<string>> GetAvailableProfilesAsync()
    {
        throw new NotImplementedException();
    }

    public Task<Dictionary<string, string>> GetRulesAsync(string? profile = null)
    {
        throw new NotImplementedException();
    }
}