using Transliterator.Domain.Entities;

namespace Transliterator.Core.Services
{
    /// <summary>
    /// Буквы вывода с подсказкой (D41): куски вывода, которые получились из букв,
    /// звука которых нет в русском. Страница рисует вывод по строкам (D17), поэтому
    /// куски переводятся в позиции внутри строки. Буква ищется по происхождению
    /// (<see cref="LetterSpan"/> от отрисовщика), а не поиском графемы в тексте:
    /// в одном профиле разные буквы пишутся похоже, а иные — обычной русской буквой.
    /// </summary>
    public sealed class OutputLetters
    {
        private readonly IReadOnlyList<LetterSpan>[] _lines;

        private OutputLetters(string text, IReadOnlyList<LetterSpan>[] lines)
        {
            Text = text;
            _lines = lines;
        }

        /// <summary>Вывод, к которому относятся куски.</summary>
        public string Text { get; }

        /// <summary>Подсказок нет: пустой вывод или выключенный тумблер.</summary>
        public static OutputLetters None(string text) =>
            new(text, Enumerable.Range(0, text.Split('\n').Length)
                                .Select(_ => (IReadOnlyList<LetterSpan>)Array.Empty<LetterSpan>())
                                .ToArray());

        /// <summary>
        /// Куски вывода <paramref name="text"/>, чья буква проходит
        /// <paramref name="hinted"/>, — по строкам, с позициями внутри строки.
        /// Кусок через перевод строки не переходит: перевод строки пишет только граница.
        /// </summary>
        public static OutputLetters Build(string text, IEnumerable<LetterSpan> letters, Func<string, bool> hinted)
        {
            var starts = new List<int> { 0 };
            for (var i = 0; i < text.Length; i++)
                if (text[i] == '\n')
                    starts.Add(i + 1);

            var lines = starts.Select(_ => new List<LetterSpan>()).ToArray();
            foreach (var span in letters)
            {
                if (!hinted(span.Letter) || span.Start < 0 || span.Start + span.Length > text.Length)
                    continue;

                var line = starts.BinarySearch(span.Start);
                if (line < 0)
                    line = ~line - 1;

                lines[line].Add(span with { Start = span.Start - starts[line] });
            }

            return new OutputLetters(text, lines.Select(l => (IReadOnlyList<LetterSpan>)l.OrderBy(s => s.Start).ToArray()).ToArray());
        }

        /// <summary>Куски строки <paramref name="line"/> с позициями в строке.</summary>
        public IReadOnlyList<LetterSpan> Line(int line) =>
            line >= 0 && line < _lines.Length ? _lines[line] : Array.Empty<LetterSpan>();

        /// <summary>
        /// Кусок строки, уже разбитый подсветкой изменённого (D19) на
        /// <paramref name="parts"/> подряд с позиции <paramref name="start"/>, — ещё
        /// и по буквам с подсказкой. Склеенные куски — ровно прежний текст.
        /// </summary>
        public IReadOnlyList<OutputPiece> Split(int line, int start, IReadOnlyList<DiffSegment> parts)
        {
            var letters = Line(line);
            var pieces = new List<OutputPiece>();
            var position = start;

            foreach (var part in parts)
            {
                var end = position + part.Text.Length;
                var cursor = position;

                foreach (var span in letters)
                {
                    var from = Math.Max(span.Start, cursor);
                    var to = Math.Min(span.Start + span.Length, end);
                    if (from >= to)
                        continue;

                    if (from > cursor)
                        pieces.Add(new OutputPiece(part.Text[(cursor - position)..(from - position)], part.Changed, null));
                    pieces.Add(new OutputPiece(part.Text[(from - position)..(to - position)], part.Changed, span.Letter));
                    cursor = to;
                }

                if (end > cursor)
                    pieces.Add(new OutputPiece(part.Text[(cursor - position)..], part.Changed, null));

                position = end;
            }

            return pieces;
        }
    }

    /// <summary>
    /// Кусок строки вывода: изменился ли он (D19) и буква с подсказкой (D41)
    /// или <c>null</c>.
    /// </summary>
    public readonly record struct OutputPiece(string Text, bool Changed, string? Letter);
}
