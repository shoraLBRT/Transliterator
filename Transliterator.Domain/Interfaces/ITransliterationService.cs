using Transliterator.Domain.Entities;

namespace Transliterator.Domain.Interfaces
{
    public interface ITransliterationService
    {
        Task<TransliterationResult> TransliterateAsync(string arabicText, string? profile = null);
        Task UpdateRuleAsync(string arabicLetter, string cyrillicMapping, string? profile = null);
        Task<Dictionary<string, string>> GetRulesAsync(string? profile = null);
        Task<IEnumerable<string>> GetAvailableProfilesAsync();
    }
}
