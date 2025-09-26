namespace Transliterator.Domain.Entities
{
    public class TransliterationResult
    {
        public string OriginalText { get; set; } = string.Empty;
        public string TransliteratedText { get; set; } = string.Empty;
        public string ProfileName { get; set; } = string.Empty;

        public TransliterationResult(string original, string transliterated, string profile)
        {
            OriginalText = original;
            TransliteratedText = transliterated;
            ProfileName = profile;
        }
    }
}
