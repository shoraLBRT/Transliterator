using Transliterator.Core.Services.Phonology;
using Transliterator.Domain.Phonology;

namespace Transliterator.Core.Services.Rules
{
    /// <summary>
    /// Стадия 6 конвейера: нун сакина, танвин и мим сакина.
    /// <para>
    /// Безгласный носовой не имеет одного звучания: он читается чисто (изхар),
    /// сливается со следующей буквой (идгам), переходит в мим (икляб) или прячется
    /// в назализацию (ихфа). Решает всё следующая буква, поэтому стадия стоит после
    /// артикля: до него неизвестно, какой согласный окажется следующим — у солнечного
    /// ляма это уже не ل.
    /// </para>
    /// <para>
    /// И до эмфазы: идгам создаёт и разрушает её условия. В "مِن رَّبِّهِمْ" твёрдость ر
    /// определяется только после слияния нуна — до него на этом месте стоит نْ.
    /// </para>
    /// <para>
    /// Танвин парсер развернул в «гласная + нун сакин», поэтому отдельных правил
    /// для него здесь нет: это тот же нун сакина.
    /// </para>
    /// </summary>
    public class NasalRule
    {
        public void Apply(IList<Segment> segments)
        {
            for (int i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];
                if (!IsSakin(segment))
                    continue;

                if (segment.Letter == ArabicScript.NunStr)
                    ApplyNunSakina(segments, i);
                else if (segment.Letter == ArabicScript.MeemStr)
                    ApplyMeemSakina(segments, i);
            }
        }

        /// <summary>
        /// Безгласный носовой. Огласовки может не быть проставлено вовсе: написание
        /// усмани сукун часто не пишет (أُنزِلَ), и на нуне «огласовки нет» означает
        /// ровно безгласность — гласную там бы написали.
        /// <para>
        /// Удвоенный нун и первая половина уже сделанного идгама сюда не попадают:
        /// это не нун сакина, а готовая гунна, и сливать её второй раз не с чем.
        /// </para>
        /// </summary>
        private static bool IsSakin(Segment segment) =>
            segment.Kind == SegmentKind.Consonant
            && !segment.Silent
            && !segment.Shadda
            && !segment.IsGeminateFirstHalf
            && segment.Vowel is Harakah.Sukun or Harakah.None;

        // ------------------------------------------------------------------
        // Нун сакина и танвин
        // ------------------------------------------------------------------
        private static void ApplyNunSakina(IList<Segment> segments, int index)
        {
            int nextIndex = NextPronounced(segments, index);
            if (nextIndex < 0)
                return;

            var nun = segments[index];
            char next = segments[nextIndex].Letter[0];

            // Изхар халькы: гортанным буквам носовой призвук передать нечем,
            // и нун перед ними дочитывается целиком.
            if (ArabicScript.ThroatLetters.Contains(next))
            {
                nun.Ghunna = false;
                return;
            }

            if (next == ArabicScript.Ba)
            {
                Iqlab(nun);
                return;
            }

            bool idgham = ArabicScript.IdghamWithGhunna.Contains(next)
                          || ArabicScript.IdghamWithoutGhunna.Contains(next);

            if (idgham)
            {
                // Изхар мутлак: внутри слова идгама не бывает. Иначе دُنْيَا и صِنْوَان
                // читались бы с удвоением, а корень слова стал бы неузнаваем.
                if (!SeparatedByBreak(segments, index, nextIndex))
                {
                    nun.Ghunna = false;
                    return;
                }

                Merge(segments, index, nextIndex,
                      ghunna: ArabicScript.IdghamWithGhunna.Contains(next));
                return;
            }

            // Ихфа: нун не звучит целиком и не исчезает — остаётся назализация,
            // окрашенная следующей буквой. Букву при этом не меняем: прячется звук,
            // а не написание.
            nun.Ghunna = true;
        }

        /// <summary>
        /// Икляб: перед ب нун переходит в мим — губы смыкаются заранее, и место
        /// образования у носового становится губным. Слова при этом не сливаются:
        /// مِنۢ بَعْدِ — это «мим ба'д», а не удвоение.
        /// </summary>
        private static void Iqlab(Segment nun)
        {
            nun.Letter = ArabicScript.MeemStr;
            nun.Vowel = Harakah.Sukun;
            nun.Ghunna = true;
        }

        // ------------------------------------------------------------------
        // Мим сакина
        // ------------------------------------------------------------------
        private static void ApplyMeemSakina(IList<Segment> segments, int index)
        {
            int nextIndex = NextPronounced(segments, index);
            if (nextIndex < 0)
                return;

            char next = segments[nextIndex].Letter[0];

            // Идгам мисляйн: мим сливается с мимом в один долгий носовой.
            if (next == ArabicScript.Meem)
            {
                Merge(segments, index, nextIndex, ghunna: true);
                return;
            }

            // Ихфа шафави: перед ب губы смыкаются не до конца, и мим назализуется.
            // Перед всем остальным — изхар шафави, чистый мим без призвука.
            segments[index].Ghunna = next == ArabicScript.Ba;
        }

        // ------------------------------------------------------------------
        // Общее
        // ------------------------------------------------------------------
        /// <summary>
        /// Идгам: носовой не исчезает, а <b>становится</b> следующей буквой — ровно
        /// как лям артикля перед солнечной. Удвоение выражено двумя сегментами,
        /// поэтому шадда, которой мусхаф отметил слияние, на второй половине лишняя:
        /// иначе стадия мадда приняла бы её за настоящее удвоение и растянула бы
        /// предыдущую гласную до мадда лязим.
        /// </summary>
        private static void Merge(IList<Segment> segments, int nasalIndex, int targetIndex, bool ghunna)
        {
            var nasal = segments[nasalIndex];
            var target = segments[targetIndex];

            nasal.Letter = target.Letter;
            nasal.Vowel = Harakah.Sukun;
            nasal.Shadda = false;
            nasal.IsGeminateFirstHalf = true;
            nasal.Ghunna = ghunna;

            target.Shadda = false;

            // Идгам в ن и م назален обеими половинами; в و и ي (идгам накыс)
            // назализация остаётся только на первой.
            if (ghunna && target.Letter is ArabicScript.NunStr or ArabicScript.MeemStr)
                target.Ghunna = true;

            Hyphenate(segments, nasalIndex, targetIndex);
        }

        /// <summary>
        /// Слово кончилось посреди звука: граница между половинами удвоения
        /// рендерится дефисом, а не пробелом — "гъофуурур-рохIииим".
        /// </summary>
        private static void Hyphenate(IList<Segment> segments, int from, int to)
        {
            for (int i = from + 1; i < to; i++)
                if (segments[i].Kind == SegmentKind.Break)
                    segments[i].Literal = "-";
        }

        /// <summary>
        /// Следующий звучащий согласный, в том числе в соседнем слове: идгам, икляб
        /// и ихфа работают как раз на стыке слов. Немую васлю пропускает — сливаться
        /// носовой будет с тем, что действительно звучит. Через паузу не смотрит:
        /// после остановки соединять нечего.
        /// </summary>
        private static int NextPronounced(IList<Segment> segments, int index)
        {
            int next = SegmentNavigator.NextConsonant(segments, index, crossWordBoundary: true);

            while (next >= 0 && segments[next].Silent)
                next = SegmentNavigator.NextConsonant(segments, next, crossWordBoundary: true);

            return next;
        }

        private static bool SeparatedByBreak(IList<Segment> segments, int from, int to)
        {
            for (int i = from + 1; i < to; i++)
                if (segments[i].Kind == SegmentKind.Break)
                    return true;

            return false;
        }
    }
}
