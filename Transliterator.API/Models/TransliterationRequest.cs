namespace Transliterator.API.Models
{
    public class TransliterationRequest
    {
        public string ArabicText { get; set; } = string.Empty;
        public string? Profile { get; set; }
    }
}
