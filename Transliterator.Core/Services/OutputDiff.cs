namespace Transliterator.Core.Services
{
    /// <summary>
    /// Что изменилось в выводе после смены настройки (D19): страница коротко
    /// подсвечивает изменившиеся слова. Строки сопоставляются по номеру, слова
    /// внутри строки — наибольшей общей подпоследовательностью (LCS). Сравнение
    /// идёт по строке, а не по всему тексту: у длинной суры тысячи слов, но
    /// в аяте их десятки, и таблица LCS остаётся маленькой.
    /// </summary>
    public sealed class OutputDiff
    {
        private readonly string[] _lines;
        private readonly IReadOnlyList<WordSpan>[] _changed;

        private OutputDiff(string text, string[] lines, IReadOnlyList<WordSpan>[] changed)
        {
            Text = text;
            _lines = lines;
            _changed = changed;
        }

        /// <summary>Новый вывод, к которому относятся отметки.</summary>
        public string Text { get; }

        /// <summary>Есть ли хоть одно изменившееся слово.</summary>
        public bool HasChanges => _changed.Any(spans => spans.Count > 0);

        /// <summary>Изменившиеся слова строки <paramref name="line"/> нового вывода — позиции в строке.</summary>
        public IReadOnlyList<WordSpan> Changed(int line) =>
            line >= 0 && line < _changed.Length ? _changed[line] : Array.Empty<WordSpan>();

        /// <summary>
        /// Сравнивает прежний вывод с новым. Строки — по общему префиксу номеров:
        /// строка, которой прежде не было, изменилась вся.
        /// </summary>
        public static OutputDiff Compare(string previous, string current)
        {
            var before = previous.Split('\n');
            var after = current.Split('\n');
            var changed = new IReadOnlyList<WordSpan>[after.Length];

            for (var i = 0; i < after.Length; i++)
            {
                changed[i] = i < before.Length
                    ? ChangedWords(before[i], after[i])
                    : Words(after[i]).ToArray();
            }

            return new OutputDiff(current, after, changed);
        }

        /// <summary>
        /// Кусок строки <paramref name="line"/> от <paramref name="start"/> длиной
        /// <paramref name="length"/> — по отрезкам «изменилось / нет». Склеенные
        /// отрезки — ровно этот кусок: подсветка только обёртка над текстом.
        /// </summary>
        public IReadOnlyList<DiffSegment> Segments(int line, int start, int length)
        {
            var text = line >= 0 && line < _lines.Length ? _lines[line] : string.Empty;
            start = Math.Clamp(start, 0, text.Length);
            var end = Math.Clamp(start + length, start, text.Length);
            var segments = new List<DiffSegment>();
            var position = start;

            foreach (var span in Changed(line))
            {
                var from = Math.Max(span.Start, start);
                var to = Math.Min(span.Start + span.Length, end);
                if (from >= to)
                    continue;

                if (from > position)
                    segments.Add(new DiffSegment(text[position..from], false));
                segments.Add(new DiffSegment(text[from..to], true));
                position = to;
            }

            if (end > position)
                segments.Add(new DiffSegment(text[position..end], false));

            return segments;
        }

        /// <summary>Весь новый вывод по отрезкам; перевод строки — неизменный отрезок.</summary>
        public IReadOnlyList<DiffSegment> Segments()
        {
            var segments = new List<DiffSegment>();
            for (var i = 0; i < _lines.Length; i++)
            {
                if (i > 0)
                    segments.Add(new DiffSegment("\n", false));
                segments.AddRange(Segments(i, 0, _lines[i].Length));
            }

            return segments;
        }

        /// <summary>Слова строки — куски без пробелов, с позицией в строке.</summary>
        private static List<WordSpan> Words(string line)
        {
            var words = new List<WordSpan>();
            var i = 0;
            while (i < line.Length)
            {
                while (i < line.Length && char.IsWhiteSpace(line[i]))
                    i++;
                var start = i;
                while (i < line.Length && !char.IsWhiteSpace(line[i]))
                    i++;
                if (i > start)
                    words.Add(new WordSpan(start, i - start));
            }

            return words;
        }

        /// <summary>
        /// Слова новой строки, не вошедшие в LCS с прежней. Таблица — O(n·m)
        /// по словам одной строки.
        /// </summary>
        private static WordSpan[] ChangedWords(string beforeLine, string afterLine)
        {
            var old = Words(beforeLine).Select(w => beforeLine.Substring(w.Start, w.Length)).ToArray();
            var spans = Words(afterLine);
            var now = spans.Select(w => afterLine.Substring(w.Start, w.Length)).ToArray();

            // lcs[i, j] — длина LCS хвостов old[i..] и now[j..].
            var lcs = new int[old.Length + 1, now.Length + 1];
            for (var i = old.Length - 1; i >= 0; i--)
                for (var j = now.Length - 1; j >= 0; j--)
                    lcs[i, j] = old[i] == now[j]
                        ? lcs[i + 1, j + 1] + 1
                        : Math.Max(lcs[i + 1, j], lcs[i, j + 1]);

            var changed = new List<WordSpan>();
            int a = 0, b = 0;
            while (b < now.Length)
            {
                if (a < old.Length && old[a] == now[b])
                {
                    a++;
                    b++;
                }
                else if (a < old.Length && lcs[a + 1, b] >= lcs[a, b + 1])
                {
                    a++;
                }
                else
                {
                    changed.Add(spans[b]);
                    b++;
                }
            }

            return changed.ToArray();
        }
    }

    /// <summary>Слово в строке вывода: начало и длина.</summary>
    public readonly record struct WordSpan(int Start, int Length);

    /// <summary>Отрезок текста и изменился ли он.</summary>
    public readonly record struct DiffSegment(string Text, bool Changed);
}
