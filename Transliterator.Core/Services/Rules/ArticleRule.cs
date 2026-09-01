using Transliterator.Core.Services.Phonology;
using Transliterator.Domain.Phonology;

namespace Transliterator.Core.Services.Rules
{
    /// <summary>
    /// Стадия 5 конвейера: лям определённого артикля — солнечный и лунный.
    /// <para>
    /// Перед солнечной буквой лям ассимилируется: он не исчезает, а <b>становится</b>
    /// этой буквой (идгам). Именно поэтому "ٱلرَّحْمَٰن" читается "ар-рохIмаан",
    /// а не "а-ррохIмаан" — дефис приходится между двумя копиями согласного.
    /// </para>
    /// <para>
    /// Стоит после васли (нужно знать, озвучен ли артикль) и до правил нун сакины,
    /// которым важно видеть окончательный следующий согласный.
    /// </para>
    /// </summary>
    public class ArticleRule
    {
        public void Apply(IList<Segment> segments)
        {
            for (int i = 0; i < segments.Count; i++)
            {
                var wasl = segments[i];
                if (wasl.Kind != SegmentKind.Consonant || !wasl.IsWaslHamza)
                    continue;

                int lamIndex = SegmentNavigator.NextConsonantInWord(segments, i);
                if (lamIndex < 0 || segments[lamIndex].Letter != ArabicScript.LamStr)
                    continue;

                int afterIndex = SegmentNavigator.NextConsonantInWord(segments, lamIndex);
                if (afterIndex < 0)
                    continue;

                var lam = segments[lamIndex];
                var after = segments[afterIndex];

                // Артикль опознаётся по написанию: лунный лям несёт сукун,
                // солнечный — шадду на следующей букве.
                if (lam.Vowel != Harakah.Sukun && !after.Shadda)
                    continue;

                bool isSun = ArabicScript.SunLetters.Contains(after.Letter[0]);

                if (isSun)
                    Assimilate(lam, after);
                else
                    lam.Vowel = Harakah.Sukun;

                // У солнечного артикля дефис приходится между двумя копиями согласного,
                // и при слиянии по васле его уже поставила стадия васли вместо пробела.
                lam.HyphenAfter = !isSun || !wasl.Silent;
            }
        }

        /// <summary>
        /// Идгам шамси: лям превращается в солнечную букву, а её шадда
        /// становится излишней — удвоение теперь выражено двумя сегментами.
        /// </summary>
        private static void Assimilate(Segment lam, Segment sunLetter)
        {
            lam.Letter = sunLetter.Letter;
            lam.Vowel = Harakah.Sukun;
            lam.Shadda = false;
            lam.IsGeminateFirstHalf = true;
            lam.Ghunna = sunLetter.Letter == ArabicScript.NunStr;

            sunLetter.Shadda = false;
        }
    }
}
