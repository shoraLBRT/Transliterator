using Transliterator.Core.Services.Phonology;
using Transliterator.Domain.Phonology;

namespace Transliterator.Core.Services.Rules
{
    /// <summary>
    /// Стадия 7 конвейера: нун сакина, танвин и мим сакина.
    /// <para>
    /// Безгласный носовой не имеет одного звучания: он читается чисто (изхар),
    /// сливается со следующей буквой (идгам), переходит в мим (икляб) или прячется
    /// в назализацию (ихфа). Решает всё следующая буква, поэтому стадия стоит после
    /// артикля: до него неизвестно, какой согласный окажется следующим — у солнечного
    /// ляма это уже не ل.
    /// </para>
    /// <para>
    /// Носовые не отданы общей стадии ассимиляции (стадия 6) именно поэтому:
    /// слияние — только одна из четырёх их возможностей, а по одному лишь признаку
    /// «шадда на следующей букве» изхар, ихфа и икляб от него не отличить.
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
        /// <param name="tajweed">
        /// В обычном чтении (I2) нун и мим читаются как написаны — изхар перед
        /// всем, кроме такой же буквы: нун с нуном и мим с мимом сливаются в любом
        /// чтении. Шадда, которой мусхаф отметил идгам на стыке слов, снимается:
        /// <c>مِن رَّبِّهِمْ</c> → «мин роббиhим».
        /// </param>
        public void Apply(IList<Segment> segments, bool tajweed = true)
        {
            for (int i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];
                if (!IsSakin(segment))
                    continue;

                if (!tajweed)
                    ApplyPlain(segments, i);
                else if (segment.Letter == ArabicScript.NunStr)
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
            // Идгам, икляб и ихфа работают в том числе на стыке слов, поэтому
            // сосед ищется за границей слова, но не за паузой: после остановки
            // соединять нечего.
            int nextIndex = SegmentNavigator.NextPronouncedConsonant(segments, index);
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

                // Идгам: нун не исчезает, а становится следующей буквой — ровно как
                // лям артикля перед солнечной. Механизм слияния общий для всех стадий,
                // здесь решается только одно: остаётся ли на шве назализация.
                Idgham.Merge(segments, index, nextIndex,
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
            int nextIndex = SegmentNavigator.NextPronouncedConsonant(segments, index);
            if (nextIndex < 0)
                return;

            char next = segments[nextIndex].Letter[0];

            // Идгам мисляйн: мим сливается с мимом в один долгий носовой.
            if (next == ArabicScript.Meem)
            {
                Idgham.Merge(segments, index, nextIndex, ghunna: true);
                return;
            }

            // Ихфа шафави: перед ب губы смыкаются не до конца, и мим назализуется.
            // Перед всем остальным — изхар шафави, чистый мим без призвука.
            segments[index].Ghunna = next == ArabicScript.Ba;
        }

        // ------------------------------------------------------------------
        // Обычное чтение
        // ------------------------------------------------------------------

        /// <summary>
        /// Без таджвида у носового два исхода: слиться с такой же буквой или
        /// прозвучать целиком. Ни икляба, ни ихфы, ни назализации.
        /// </summary>
        private static void ApplyPlain(IList<Segment> segments, int index)
        {
            var nasal = segments[index];
            nasal.Ghunna = false;

            int nextIndex = SegmentNavigator.NextPronouncedConsonant(segments, index);
            if (nextIndex < 0)
                return;

            var next = segments[nextIndex];

            if (next.Letter == nasal.Letter && (next.Shadda || SeparatedByBreak(segments, index, nextIndex)))
            {
                Idgham.Merge(segments, index, nextIndex, ghunna: false);
                return;
            }

            // Шадда после безгласного носового — помета слияния, а не удвоение:
            // после закрытого слога удвоенному согласному места нет.
            if (next.Shadda)
                next.Shadda = false;
        }

        // ------------------------------------------------------------------
        // Общее
        // ------------------------------------------------------------------
        private static bool SeparatedByBreak(IList<Segment> segments, int from, int to)
        {
            for (int i = from + 1; i < to; i++)
                if (segments[i].Kind == SegmentKind.Break)
                    return true;

            return false;
        }
    }
}
