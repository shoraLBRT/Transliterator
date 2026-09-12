using Transliterator.Core.Services.Phonology;
using Transliterator.Domain.Phonology;

namespace Transliterator.Core.Services.Rules
{
    /// <summary>
    /// Стадия 6 конвейера: идгам мутаджанисайн и мутакарибайн — слияние безгласного
    /// согласного с однородным или близким ему следующим.
    /// <para>
    /// Признак берётся из письма, а не из списка пар: мусхаф сам отмечает такое
    /// слияние шаддой на следующей букве при безгласной предыдущей — عَبَدتُّمْ,
    /// قَالَت طَّآئِفَةٌ, يَلْهَث ذَّٰلِكَ. Списка пар для этого не нужно, и он был бы
    /// хуже: он решал бы за мусхаф там, где мусхаф уже решил, и расходился бы
    /// с ним на спорных парах.
    /// </para>
    /// <para>
    /// Стоит после артикля: к этому моменту солнечный лям уже слился, его шадда
    /// снята, и принимать её за отметку нового слияния больше не за что. И до
    /// стадии 7: нун и мим сакина решают свою судьбу сами — им, кроме слияния,
    /// доступны изхар, ихфа и икляб, и отдать их сюда значило бы потерять гунну.
    /// Поэтому носовые эта стадия не трогает вовсе.
    /// </para>
    /// <para>
    /// В отличие от нун сакины, идгама внутри слова здесь не только не запрещено,
    /// но и составляет главный случай: в عَبَدتُّمْ сливаются соседние буквы одного
    /// слова, и границы, через которую можно было бы не заглядывать, между ними нет.
    /// </para>
    /// </summary>
    public class AssimilationRule
    {
        public void Apply(IList<Segment> segments)
        {
            for (int i = 0; i < segments.Count; i++)
            {
                if (!IsSakin(segments[i]))
                    continue;

                int nextIndex = SegmentNavigator.NextPronouncedConsonant(segments, i);
                if (nextIndex < 0 || !segments[nextIndex].Shadda)
                    continue;

                var target = segments[nextIndex];

                // Слияние в носовой назализовано независимо от того, что слилось:
                // гунну даёт принимающая буква, а не источник.
                Idgham.Merge(segments, i, nextIndex,
                             ghunna: target.Letter is ArabicScript.NunStr or ArabicScript.MeemStr);
            }
        }

        /// <summary>
        /// Безгласный согласный, кроме носовых. Огласовки может не быть проставлено
        /// вовсе — в مسند сукун часто не пишут, и «огласовки нет» означает ровно
        /// безгласность.
        /// <para>
        /// Первая половина уже сделанного слияния и сама удвоенная буква сюда
        /// не попадают: это не безгласный согласный перед удвоением, а готовое
        /// удвоение, и сливать его второй раз не с чем.
        /// </para>
        /// </summary>
        private static bool IsSakin(Segment segment) =>
            segment.Kind == SegmentKind.Consonant
            && !segment.Silent
            && !segment.Shadda
            && !segment.IsGeminateFirstHalf
            && segment.Letter is not (ArabicScript.NunStr or ArabicScript.MeemStr)
            && segment.Vowel is Harakah.Sukun or Harakah.None;
    }
}
