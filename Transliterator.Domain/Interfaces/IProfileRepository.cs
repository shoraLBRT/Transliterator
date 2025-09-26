using Transliterator.Domain.Entities;

namespace Transliterator.Domain.Interfaces
{
    public interface IProfileRepository
    {
        Task<TransliterationProfile?> GetProfileAsync(string profileName);
        Task<IEnumerable<TransliterationProfile>> GetAllProfilesAsync();
        Task SaveProfileAsync(TransliterationProfile profile);
        Task DeleteProfileAsync(string profileName);
        Task<bool> ProfileExistsAsync(string profileName);
    }
}
