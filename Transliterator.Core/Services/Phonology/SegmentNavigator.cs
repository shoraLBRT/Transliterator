using Transliterator.Domain.Phonology;

namespace Transliterator.Core.Services.Phonology
{
    /// <summary>
    /// Навигация по потоку сегментов. Все правила смотрят на соседей одинаково,
    /// и в частности одинаково понимают, где кончается слово.
    /// <para>
    /// Пауза — граница жёстче словесной: через неё не заглядывает никто, даже
    /// правила, которым разрешено пересекать границу слов. Между разорванными
    /// паузой словами нет соединения, а значит нет и повода их сопоставлять.
    /// </para>
    /// </summary>
    public static class SegmentNavigator
    {
        /// <summary>Индекс следующего согласного в пределах того же слова, иначе -1.</summary>
        public static int NextConsonantInWord(IList<Segment> segments, int index) =>
            NextConsonant(segments, index, crossWordBoundary: false);

        /// <summary>Индекс предыдущего согласного в пределах того же слова, иначе -1.</summary>
        public static int PreviousConsonantInWord(IList<Segment> segments, int index) =>
            PreviousConsonant(segments, index, crossWordBoundary: false);

        public static int NextConsonant(IList<Segment> segments, int index, bool crossWordBoundary)
        {
            for (int i = index + 1; i < segments.Count; i++)
            {
                switch (segments[i].Kind)
                {
                    case SegmentKind.Consonant:
                        return i;
                    case SegmentKind.Other:
                        continue;
                    case SegmentKind.Break when crossWordBoundary && !segments[i].IsPause:
                        continue;
                    default:
                        return -1;
                }
            }

            return -1;
        }

        public static int PreviousConsonant(IList<Segment> segments, int index, bool crossWordBoundary)
        {
            for (int i = index - 1; i >= 0; i--)
            {
                switch (segments[i].Kind)
                {
                    case SegmentKind.Consonant:
                        return i;
                    case SegmentKind.Other:
                        continue;
                    case SegmentKind.Break when crossWordBoundary && !segments[i].IsPause:
                        continue;
                    default:
                        return -1;
                }
            }

            return -1;
        }
    }
}
