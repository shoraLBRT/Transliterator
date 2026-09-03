using Transliterator.Core.Services.Phonology;
using Transliterator.Domain.Phonology;

namespace Transliterator.Core.Services.Rules
{
    /// <summary>
    /// Стадия 9 конвейера: кальканя.
    /// <para>
    /// Взрывной согласный ق ط ب ج د нельзя произнести безгласным «в тишину»:
    /// смычка полная, тянуть в ней нечего, и размыкание слышно коротким отзвуком.
    /// Правило не меняет ни буквы, ни огласовки — оно только помечает, что звук
    /// размыкается, и насколько громко.
    /// </para>
    /// <para>
    /// Стоит последней среди правил, и не по остаточному принципу: безгласность
    /// здесь — итог всех предыдущих стадий. Огласовку снимает вакф, первую половину
    /// удвоения создаёт идгам, а немой букву делает васля — до них неизвестно,
    /// какая буква окажется безгласной и окажется ли она перед остановкой.
    /// </para>
    /// </summary>
    public class QalqalahRule
    {
        public void Apply(IList<Segment> segments)
        {
            for (int i = 0; i < segments.Count; i++)
            {
                if (!IsQalqalahLetter(segments[i]))
                    continue;

                segments[i].Qalqalah = IsAtPause(segments, i) ? Qalqalah.Major : Qalqalah.Minor;
            }
        }

        /// <summary>
        /// Буква кальканя в безгласном положении. Огласовки может не быть проставлено
        /// вовсе — в написании усмани сукун часто не пишется, и «огласовки нет»
        /// на согласном означает ровно безгласность.
        /// <para>
        /// Первая половина удвоения отзвука не даёт: при идгаме буква не размыкается,
        /// а переходит в следующую (ٱلدِّينِ — лям артикля стал первой د) — размыкание
        /// будет одно, и придётся оно на вторую половину со своей огласовкой.
        /// </para>
        /// </summary>
        private static bool IsQalqalahLetter(Segment segment) =>
            segment.Kind == SegmentKind.Consonant
            && !segment.Silent
            && !segment.IsGeminateFirstHalf
            && segment.Letter.Length > 0
            && ArabicScript.QalqalahLetters.Contains(segment.Letter[0])
            && segment.Vowel is Harakah.Sukun or Harakah.None;

        /// <summary>
        /// Кальканя кубра: за буквой не звучит уже ничего — ни в этом слове, ни
        /// в следующем. Отзвук в такой позиции ничем не гасится и слышен отчётливо.
        /// <para>
        /// Спрашивать здесь надо именно про соседа, а не про снятую паузой огласовку
        /// (как это делает мадд арид): لَمْ يُولَدْ кончается написанным сукуном,
        /// снимать вакфу нечего — а кальканя на остановке всё равно усиленная.
        /// Удвоение на паузе тоже сюда попадает: وَتَبَّ размыкается один раз и громко.
        /// </para>
        /// </summary>
        private static bool IsAtPause(IList<Segment> segments, int index) =>
            SegmentNavigator.NextPronouncedConsonant(segments, index) < 0;
    }
}
