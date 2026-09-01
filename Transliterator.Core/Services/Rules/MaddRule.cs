using Transliterator.Core.Services.Phonology;
using Transliterator.Domain.Phonology;

namespace Transliterator.Core.Services.Rules
{
    /// <summary>
    /// Стадия 8 конвейера: длительность мадда.
    /// <para>
    /// Единственная стадия, которая назначает <i>количество</i> харакатов.
    /// Стоит после разметки пауз (мадд арид и мадд ивад существуют только на паузе)
    /// и после ассимиляций (мадд лязим срабатывает от шадды, которую может создать идгам).
    /// </para>
    /// <para>
    /// Мусхаф в написании усмани сам размечает обязательный мадд знаком ٓ,
    /// поэтому основной сигнал берётся из текста, а не восстанавливается эвристикой.
    /// </para>
    /// </summary>
    public class MaddRule
    {
        private const int Natural = 2;
        private const int Obligatory = 4;
        private const int Lazim = 6;

        public void Apply(IList<Segment> segments)
        {
            for (int i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];
                if (segment.Kind != SegmentKind.Consonant || segment.VowelLength < Natural)
                    continue;

                int nextIndex = SegmentNavigator.NextConsonant(segments, i, crossWordBoundary: true);
                if (nextIndex < 0)
                    continue;

                var next = segments[nextIndex];

                // Мадд лязим: за долгой гласной идёт удвоенный согласный — ٱلضَّآلِّينَ.
                if (next.Shadda && segment.VowelLength >= Obligatory)
                {
                    segment.VowelLength = Lazim;
                    continue;
                }

                // Мадд муттасиль и мунфасиль: за долгой гласной идёт хамза.
                if (next.Letter == ArabicScript.HamzaStr && !next.Silent
                                                         && segment.VowelLength < Obligatory)
                    segment.VowelLength = Obligatory;
            }
        }
    }
}
