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

        /// <summary>Слов из двух букв и больше во входном тексте (D45).</summary>
        public int Words { get; init; }

        /// <summary>Из них написаны без огласовок (D45): их чтение — догадка конвейера.</summary>
        public int UnvowelledWords { get; init; }

        /// <summary>
        /// Без огласовок половина слов или больше (D45): такой текст не читается,
        /// и страница вместо вывода объясняет, какой текст нужен.
        /// </summary>
        public bool MostlyUnvowelled => Words > 0 && UnvowelledWords * 2 >= Words;

        public TransliterationResult(string original, string transliterated, string profile)
        {
            OriginalText = original;
            TransliteratedText = transliterated;
            ProfileName = profile;
        }
    }
}
