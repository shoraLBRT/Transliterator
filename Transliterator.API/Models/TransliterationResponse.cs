namespace Transliterator.API.Models
{
    public class TransliterationResponse
    {
        public string OriginalText { get; set; } = string.Empty;
        public string TransliteratedText { get; set; } = string.Empty;
        public string ProfileUsed { get; set; } = string.Empty;

        public TransliterationResponse(Domain.Entities.TransliterationResult result)
        {
            OriginalText = result.OriginalText;
            TransliteratedText = result.TransliteratedText;
            ProfileUsed = result.ProfileName;
        }
    }
}
