using Transliterator.Domain.Entities;

namespace Transliterator.Core.Services
{
    /// <summary>
    /// Разметка строк вывода по происхождению кусков: буквы с подсказкой (D41) —
    /// куски из букв, звука которых нет в русском, — и слова без огласовок (D45).
    /// Страница рисует вывод по строкам (D17), поэтому куски переводятся в позиции
    /// внутри строки. Буква ищется по происхождению (<see cref="LetterSpan"/> от
    /// отрисовщика), а не поиском графемы в тексте: в одном профиле разные буквы
    /// пишутся похоже, а иные — обычной русской буквой.
    /// </summary>
    public sealed class OutputLetters
    {
        private readonly IReadOnlyList<LetterSpan>[] _lines;
        private readonly IReadOnlyList<WordSpan>[] _unvowelled;

        private OutputLetters(string text, IReadOnlyList<LetterSpan>[] lines, IReadOnlyList<WordSpan>[] unvowelled)
        {
            Text = text;
            _lines = lines;
            _unvowelled = unvowelled;
        }

        /// <summary>Вывод, к которому относятся куски.</summary>
        public string Text { get; }

        /// <summary>Разметки нет: ни подсказок, ни слов без огласовок.</summary>
        public static OutputLetters None(string text) =>
            Build(text, Array.Empty<LetterSpan>(), _ => false);

        /// <summary>
        /// Куски вывода <paramref name="text"/>, чья буква проходит
        /// <paramref name="hinted"/>, и слова вывода с буквой слова без огласовок —
        /// по строкам, с позициями внутри строки. Кусок через перевод строки не
        /// переходит: перевод строки пишет только граница.
        /// </summary>
        public static OutputLetters Build(string text, IEnumerable<LetterSpan> letters, Func<string, bool> hinted)
        {
            var starts = new List<int> { 0 };
            for (var i = 0; i < text.Length; i++)
                if (text[i] == '\n')
                    starts.Add(i + 1);

            var lines = starts.Select(_ => new List<LetterSpan>()).ToArray();
            var unvowelled = starts.Select(_ => new SortedSet<int>()).ToArray();

            foreach (var span in letters)
            {
                if (span.Start < 0 || span.Start + span.Length > text.Length)
                    continue;

                var line = starts.BinarySearch(span.Start);
                if (line < 0)
                    line = ~line - 1;

                if (hinted(span.Letter))
                    lines[line].Add(span with { Start = span.Start - starts[line] });

                if (span.Unvowelled)
                    unvowelled[line].Add(span.Start - starts[line]);
            }

            var words = new IReadOnlyList<WordSpan>[starts.Count];
            for (var line = 0; line < starts.Count; line++)
            {
                var end = line + 1 < starts.Count ? starts[line + 1] - 1 : text.Length;
                words[line] = WordsAround(text[starts[line]..end], unvowelled[line]);
            }

            return new OutputLetters(text, lines.Select(l => (IReadOnlyList<LetterSpan>)l.OrderBy(s => s.Start).ToArray()).ToArray(), words);
        }

        /// <summary>
        /// Слова строки (куски без пробелов) вокруг позиций <paramref name="at"/>:
        /// подсвечивается слово вывода целиком, а не одна графема.
        /// </summary>
        private static IReadOnlyList<WordSpan> WordsAround(string line, IEnumerable<int> at)
        {
            var words = new List<WordSpan>();
            foreach (var position in at)
            {
                if (words.Count > 0 && position < words[^1].Start + words[^1].Length)
                    continue;

                var start = position;
                while (start > 0 && !char.IsWhiteSpace(line[start - 1]))
                    start--;
                var end = position;
                while (end < line.Length && !char.IsWhiteSpace(line[end]))
                    end++;

                words.Add(new WordSpan(start, end - start));
            }

            return words;
        }

        /// <summary>Куски строки <paramref name="line"/> с подсказкой, с позициями в строке.</summary>
        public IReadOnlyList<LetterSpan> Line(int line) =>
            line >= 0 && line < _lines.Length ? _lines[line] : Array.Empty<LetterSpan>();

        /// <summary>Слова без огласовок в строке <paramref name="line"/> (D45), с позициями в строке.</summary>
        public IReadOnlyList<WordSpan> Unvowelled(int line) =>
            line >= 0 && line < _unvowelled.Length ? _unvowelled[line] : Array.Empty<WordSpan>();

        /// <summary>
        /// Кусок строки, уже разбитый подсветкой изменённого (D19) на
        /// <paramref name="parts"/> подряд с позиции <paramref name="start"/>, — ещё
        /// и по буквам с подсказкой и словам без огласовок. Склеенные куски — ровно
        /// прежний текст.
        /// </summary>
        public IReadOnlyList<OutputPiece> Split(int line, int start, IReadOnlyList<DiffSegment> parts)
        {
            var letters = Line(line);
            var words = Unvowelled(line);
            var pieces = new List<OutputPiece>();
            var position = start;

            foreach (var part in parts)
            {
                var end = position + part.Text.Length;

                // Границы внутри куска: края букв и слов, что в него попадают.
                var cuts = new SortedSet<int> { position, end };
                foreach (var span in letters)
                {
                    cuts.Add(Math.Clamp(span.Start, position, end));
                    cuts.Add(Math.Clamp(span.Start + span.Length, position, end));
                }
                foreach (var word in words)
                {
                    cuts.Add(Math.Clamp(word.Start, position, end));
                    cuts.Add(Math.Clamp(word.Start + word.Length, position, end));
                }

                int? from = null;
                foreach (var cut in cuts)
                {
                    if (from is { } a && cut > a)
                    {
                        var letter = letters.FirstOrDefault(s => s.Start <= a && a < s.Start + s.Length)?.Letter;
                        var unvowelled = words.Any(w => w.Start <= a && a < w.Start + w.Length);
                        pieces.Add(new OutputPiece(part.Text[(a - position)..(cut - position)], part.Changed, letter, unvowelled));
                    }

                    from = cut;
                }

                position = end;
            }

            return pieces;
        }
    }

    /// <summary>
    /// Кусок строки вывода: изменился ли он (D19), буква с подсказкой (D41)
    /// или <c>null</c> и входит ли он в слово без огласовок (D45).
    /// </summary>
    public readonly record struct OutputPiece(string Text, bool Changed, string? Letter, bool Unvowelled = false);
}
