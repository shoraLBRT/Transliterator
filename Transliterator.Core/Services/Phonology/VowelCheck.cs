using Transliterator.Domain.Phonology;

namespace Transliterator.Core.Services.Phonology
{
    /// <summary>
    /// Слова без огласовок (D45). Конвейер читает огласовки: без них гласные не
    /// известны, и вывод неверен, хотя выглядит правдоподобно. Знаки считаются по
    /// написанному тексту, а не по разбору: разбор сам подставляет гласную, где её
    /// можно угадать (фатха у «أ»), а стадия вакфа ставит сукун на паузе — по
    /// сегментам неогласованное слово выглядело бы огласованным.
    /// </summary>
    public static class VowelCheck
    {
        /// <summary>
        /// Считает слова текста и слова без огласовок и помечает буквы последних
        /// (<see cref="Segment.Unvowelled"/>). Слово — буквы со знаками подряд.
        /// Однобуквенное слово (و, ب) не считается: частицу без огласовки читают
        /// и так, а ложная тревога на каждом «и» мешала бы. Слова текста и слова
        /// разбора (от <see cref="Segment.StartsWord"/> до следующего) идут в одном
        /// порядке; если их число разошлось, буквы не помечаются — счёт остаётся.
        /// </summary>
        /// <param name="text">Текст после нормализации — тот, что разобран.</param>
        /// <param name="segments">Разбор этого текста до правил.</param>
        public static VowelCoverage Mark(string text, IReadOnlyList<Segment> segments)
        {
            var written = Words(text);
            var parsed = ParsedWords(segments);
            var aligned = written.Count == parsed.Count;

            var words = 0;
            var unvowelled = 0;
            for (var i = 0; i < written.Count; i++)
            {
                var (letters, marked) = written[i];
                if (letters < 2)
                    continue;

                words++;
                if (marked)
                    continue;

                unvowelled++;
                if (aligned)
                    foreach (var segment in parsed[i])
                        segment.Unvowelled = true;
            }

            return new VowelCoverage(words, unvowelled);
        }

        /// <summary>Слова текста: сколько в слове букв и есть ли у него хоть один знак чтения.</summary>
        private static List<(int Letters, bool Marked)> Words(string text)
        {
            var words = new List<(int, bool)>();
            var letters = 0;
            var marked = false;

            void Close()
            {
                if (letters > 0)
                    words.Add((letters, marked));
                letters = 0;
                marked = false;
            }

            foreach (var c in text)
            {
                if (IsLetter(c))
                    letters++;
                else if (IsReadingMark(c))
                    marked = true;
                else if (!IsOtherMark(c))
                    Close();
            }

            Close();
            return words;
        }

        /// <summary>Слова разбора: согласные от начала слова до следующего.</summary>
        private static List<List<Segment>> ParsedWords(IReadOnlyList<Segment> segments)
        {
            var words = new List<List<Segment>>();
            List<Segment>? word = null;

            foreach (var segment in segments)
            {
                if (segment.Kind != SegmentKind.Consonant)
                {
                    word = null;
                    continue;
                }

                if (word is null || segment.StartsWord)
                {
                    word = new List<Segment>();
                    words.Add(word);
                }

                word.Add(segment);
            }

            return words;
        }

        /// <summary>Арабская буква: основной блок и буквы Корана (алиф-васля ٱ и др.).</summary>
        private static bool IsLetter(char c) => c is >= 'ء' and <= 'ي' or >= 'ٱ' and <= 'ۓ';

        /// <summary>
        /// Знак чтения: огласовки, танвин, сукун, шадда, мадда, малый алиф и малые
        /// знаки мусхафа. Хамза над и под буквой (U+0654, U+0655) — часть буквы, а не
        /// огласовка: «أحد» без огласовок остаётся без огласовок.
        /// </summary>
        private static bool IsReadingMark(char c) =>
            c is >= 'ً' and <= 'ٓ' or >= 'ٖ' and <= 'ٟ' or 'ٰ' or >= 'ۖ' and <= 'ۭ';

        /// <summary>Знак внутри слова, который не огласовка: хамза над и под буквой, татвиль.</summary>
        private static bool IsOtherMark(char c) => c is 'ٔ' or 'ٕ' or 'ـ';
    }

    /// <summary>Сколько в тексте слов (из двух букв и больше) и сколько из них без огласовок (D45).</summary>
    public readonly record struct VowelCoverage(int Words, int Unvowelled);
}
