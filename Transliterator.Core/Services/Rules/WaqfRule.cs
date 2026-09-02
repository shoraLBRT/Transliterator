using Transliterator.Core.Services.Phonology;
using Transliterator.Domain.Phonology;

namespace Transliterator.Core.Services.Rules
{
    /// <summary>
    /// Стадия 3 конвейера: вакф — паузы и паузальное произношение.
    /// <para>
    /// Стоит первой среди правил, потому что решает не про звук отдельной буквы,
    /// а про то, какие слова вообще окажутся рядом. Всё, что идёт дальше — васля,
    /// артикль, эмфаза, мадд — смотрит на соседей, и соседей им определяет вакф.
    /// </para>
    /// <para><b>Политика остановок.</b>
    /// Знак вакфа — совет, а не приказ, поэтому решение всё равно принимает конвейер.
    /// По умолчанию он читает настолько слитно, насколько текст это позволяет,
    /// и останавливается только там, где остановки требует или прямо советует мусхаф:
    /// на ۘ (обязательная) и на ۗ (остановка предпочтительнее соединения).
    /// На ۖ и ۙ соединение прямо предпочтено или предписано; ۚ и ۛ остановку лишь
    /// дозволяют, ничем её не выделяя, а ۜ — вообще не остановка, а мгновенная сакта
    /// без разрыва дыхания. Во всех этих случаях чтение продолжается.
    /// </para>
    /// <para>
    /// Кроме знаков, пауза возникает там, где текст кончается: перед номером аята
    /// и в конце разбираемого фрагмента. Эти две границы безусловны — номер аята
    /// разделяет высказывания, а после последнего слова читать уже нечего.
    /// </para>
    /// </summary>
    public class WaqfRule
    {
        /// <summary>Мадд ивад: танвин фатхи на паузе читается долгой ā в 2 хараката.</summary>
        private const int Iwad = 2;

        public void Apply(IList<Segment> segments)
        {
            MarkPauses(segments);

            // С конца: снятие танвина удаляет сегмент, и при обратном порядке
            // это не сдвигает границы, которые ещё предстоит обработать.
            for (int i = segments.Count - 1; i >= 0; i--)
                if (IsPauseBoundary(segments[i]))
                    ApplyPausalForm(segments, i);

            ApplyPausalForm(segments, segments.Count);
        }

        /// <summary>
        /// Превращает написанные знаки в решение конвейера. После этой стадии правила
        /// смотрят только на <see cref="Segment.IsPause"/> и знать про знаки не обязаны.
        /// </summary>
        private static void MarkPauses(IList<Segment> segments)
        {
            foreach (var segment in segments)
            {
                if (segment.Kind != SegmentKind.Break || segment.Waqf == WaqfMark.None)
                    continue;

                segment.IsPause = segment.Waqf is WaqfMark.Obligatory or WaqfMark.StopPreferred;
            }
        }

        /// <summary>Номер аята — такая же граница паузы, как знак вакфа, только не помеченная.</summary>
        private static bool IsPauseBoundary(Segment segment) =>
            segment.Kind == SegmentKind.Digit
            || (segment.Kind == SegmentKind.Break && segment.IsPause);

        /// <summary>
        /// Паузальное произношение последнего слова перед границей <paramref name="boundary"/>.
        /// Затрагивает ровно один согласный: остановка меняет только тот звук,
        /// на котором голос обрывается.
        /// </summary>
        private static void ApplyPausalForm(IList<Segment> segments, int boundary)
        {
            int last = LastPronouncedConsonant(segments, boundary);
            if (last < 0)
                return;

            // Танвин парсер развернул в «гласная + нун сакин», поэтому снимать
            // приходится оба сегмента: конечным согласным здесь оказывается нун,
            // а огласовка, которую решает пауза, стоит на букве перед ним.
            if (segments[last].FromTanwin)
            {
                int carrier = SegmentNavigator.PreviousConsonantInWord(segments, last);
                segments.RemoveAt(last);

                if (carrier >= 0)
                    ApplyPausalTanwin(segments[carrier]);

                return;
            }

            StripFinalVowel(segments[last]);
        }

        /// <summary>
        /// Танвин на паузе не звучит никогда — нун снят выше. Разница в огласовке:
        /// фатхатан переходит в долгую ā (мадд ивад), дамматан и касратан пропадают
        /// вместе с нуном, и слово обрывается на согласном.
        /// </summary>
        private static void ApplyPausalTanwin(Segment carrier)
        {
            if (carrier.Vowel == Harakah.Fatha)
            {
                carrier.VowelLength = Math.Max(carrier.VowelLength, Iwad);
                return;
            }

            StripFinalVowel(carrier);
        }

        /// <summary>
        /// Конечная краткая огласовка на паузе не произносится: ٱلرَّحِيمِ читается
        /// "ррохIиим", а не "ррохIиими". Долгая гласная остаётся — обрывается голос
        /// на согласном, а гласной обрываться не на чем: مَا на паузе всё та же "маа".
        /// <para>
        /// Шадда согласного при этом сохраняется (رَبِّ → "робб"): пауза снимает
        /// огласовку, а не удвоение.
        /// </para>
        /// </summary>
        private static void StripFinalVowel(Segment segment)
        {
            if (segment.Vowel is Harakah.None or Harakah.Sukun || segment.VowelLength > 1)
                return;

            // Огласовка нужна дальше правилу эмфазы, поэтому она не теряется,
            // а переезжает: звука у неё больше нет, качества согласного она не отменяет.
            segment.OriginalVowel = segment.Vowel;
            segment.Vowel = Harakah.Sukun;
        }

        /// <summary>Последний звучащий согласный перед границей. Через чужую паузу не смотрит.</summary>
        private static int LastPronouncedConsonant(IList<Segment> segments, int boundary)
        {
            for (int i = boundary - 1; i >= 0; i--)
            {
                var segment = segments[i];

                if (segment.Kind == SegmentKind.Digit)
                    return -1;
                if (segment.Kind == SegmentKind.Break && segment.IsPause)
                    return -1;
                if (segment.Kind == SegmentKind.Consonant && !segment.Silent)
                    return i;
            }

            return -1;
        }
    }
}
