namespace Transliterator.Domain.Entities
{
    public class TransliterationResult
    {
        public string OriginalText { get; set; } = string.Empty;
        public string TransliteratedText { get; set; } = string.Empty;
        public string ProfileName { get; set; } = string.Empty;

        /// <summary>
        /// Из какой арабской буквы получился каждый кусок вывода (D41), по порядку.
        /// Страница по ним подсвечивает буквы, звука которых нет в русском.
        /// </summary>
        public IReadOnlyList<LetterSpan> Letters { get; init; } = Array.Empty<LetterSpan>();

        public TransliterationResult(string original, string transliterated, string profile)
        {
            OriginalText = original;
            TransliteratedText = transliterated;
            ProfileName = profile;
        }
    }
}
