using Transliterator.Domain.Phonology;

namespace Transliterator.Core.Services.Phonology
{
    /// <summary>
    /// Слияние двух согласных в один долгий звук. Механизм у всех идгамов один
    /// и тот же, а поводов к нему три — лям артикля перед солнечной буквой,
    /// нун и мим сакина, однородные и близкие согласные, — и разные стадии
    /// обязаны делать это одинаково, иначе один и тот же шов будет рендериться
    /// то дефисом, то пробелом.
    /// </summary>
    public static class Idgham
    {
        /// <summary>
        /// Первый согласный не исчезает, а <b>становится</b> вторым: удвоение
        /// выражено двумя сегментами, а не шаддой. Шадда, которой мусхаф отметил
        /// слияние, на второй половине после этого лишняя — иначе стадия мадда
        /// приняла бы её за настоящее удвоение и растянула бы предыдущую гласную
        /// до мадда лязим.
        /// </summary>
        /// <param name="ghunna">Назализация на шве: у слияния в ن и م она есть, у прочих нет.</param>
        public static void Merge(IList<Segment> segments, int firstIndex, int secondIndex, bool ghunna)
        {
            var first = segments[firstIndex];
            var second = segments[secondIndex];

            first.Letter = second.Letter;
            first.Vowel = Harakah.Sukun;
            first.Shadda = false;
            first.IsGeminateFirstHalf = true;
            first.Ghunna = ghunna;

            second.Shadda = false;

            // Идгам в ن и م назален обеими половинами; в و и ي (идгам накыс)
            // назализация остаётся только на первой.
            if (ghunna && second.Letter is ArabicScript.NunStr or ArabicScript.MeemStr)
                second.Ghunna = true;

            Hyphenate(segments, firstIndex, secondIndex);
        }

        /// <summary>
        /// Слово кончилось посреди звука: граница между половинами удвоения
        /// рендерится дефисом, а не пробелом — "гъофуурур-рохIииим". Внутри слова
        /// границы нет, и менять нечего.
        /// </summary>
        private static void Hyphenate(IList<Segment> segments, int from, int to)
        {
            for (int i = from + 1; i < to; i++)
                if (segments[i].Kind == SegmentKind.Break)
                    segments[i].Literal = "-";
        }
    }
}
