using Transliterator.Core.Services.Phonology;
using Transliterator.Domain.Phonology;

namespace Transliterator.Core.Services.Rules
{
    /// <summary>
    /// Стадия 4 конвейера: хамзат аль-васль (ٱ).
    /// <para>
    /// В начале высказывания васля озвучивается, в соединении — исчезает,
    /// а граница слова перед ней рендерится дефисом: "бисми-лляhи".
    /// </para>
    /// <para>
    /// Стоит после разметки пауз (там решается, есть ли вообще соединение)
    /// и до правила артикля, которому нужно знать, озвучена ли васля.
    /// </para>
    /// </summary>
    public class WaslRule
    {
        /// <summary>
        /// Семь имён, у которых васля всегда берёт касру, хотя формально это не глаголы.
        /// Ключ — скелет из согласных после хамзы.
        /// </summary>
        private static readonly HashSet<string> SevenNouns = new()
        {
            "سم",     // ٱسْم
            "بن",     // ٱبْن
            "بنة",    // ٱبْنَة
            "مرء",    // ٱمْرُؤ
            "مرءة",   // ٱمْرَأَة
            "ثنين",   // ٱثْنَان / ٱثْنَيْن
            "ثنتين"   // ٱثْنَتَان / ٱثْنَتَيْن
        };

        /// <summary>
        /// Глаголы, где дамма на третьей букве — «привнесённая» (ضمة عارضة) от
        /// местоименного вава, а не коренная. Васля в них остаётся с касрой.
        /// </summary>
        private static readonly HashSet<string> TransientDammaVerbs = new()
        {
            "مشوا",   // ٱمْشُوا
            "قضوا",   // ٱقْضُوا
            "بنوا",   // ٱبْنُوا
            "مضوا",   // ٱمْضُوا
            "ءتوا"    // ٱئْتُوا
        };

        public void Apply(IList<Segment> segments)
        {
            for (int i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];
                if (segment.Kind != SegmentKind.Consonant || !segment.IsWaslHamza)
                    continue;

                if (StartsUtterance(segments, i))
                    segment.Vowel = InitialVowel(segments, i);
                else
                    Connect(segments, i);
            }
        }

        /// <summary>
        /// Васля стоит в начале высказывания, если до неё в текущем высказывании
        /// ещё не было ни одного произносимого согласного. Номер аята считается
        /// границей высказывания — после него чтение начинается заново.
        /// </summary>
        private static bool StartsUtterance(IList<Segment> segments, int index)
        {
            for (int i = index - 1; i >= 0; i--)
            {
                var segment = segments[i];

                if (segment.Kind == SegmentKind.Digit)
                    return true;

                if (segment.Kind == SegmentKind.Consonant && !segment.Silent)
                    return false;
            }

            return true;
        }

        private static void Connect(IList<Segment> segments, int index)
        {
            segments[index].Silent = true;

            // Пробел перед слитой васлей рендерится дефисом, а не пробелом.
            for (int i = index - 1; i >= 0; i--)
            {
                if (segments[i].Kind == SegmentKind.Consonant)
                    break;

                if (segments[i].Kind == SegmentKind.Break)
                {
                    segments[i].Literal = "-";
                    break;
                }
            }

            // Если предыдущее слово кончается сукуном, произнести стык нечем —
            // вставляется вспомогательная касра: قُلِ ٱللَّهُمَّ.
            int previous = PreviousPronouncedConsonant(segments, index);
            if (previous >= 0 && segments[previous].Vowel == Harakah.Sukun)
                segments[previous].Vowel = Harakah.Kasra;
        }

        /// <summary>
        /// Огласовка васли в начале чтения:
        /// артикль — фатха; семь имён — касра; глагол — дамма, если третья буква
        /// (считая саму хамзу первой) несёт дамму, иначе касра.
        /// </summary>
        private Harakah InitialVowel(IList<Segment> segments, int index)
        {
            int second = SegmentNavigator.NextConsonantInWord(segments, index);
            if (second < 0)
                return Harakah.Kasra;

            if (segments[second].Letter == ArabicScript.LamStr && IsArticle(segments, second))
                return Harakah.Fatha;

            var skeleton = WordSkeleton(segments, index);

            if (SevenNouns.Contains(skeleton))
                return Harakah.Kasra;

            if (TransientDammaVerbs.Contains(skeleton))
                return Harakah.Kasra;

            int third = SegmentNavigator.NextConsonantInWord(segments, second);
            if (third >= 0 && segments[third].Vowel == Harakah.Damma)
                return Harakah.Damma;

            return Harakah.Kasra;
        }

        private static bool IsArticle(IList<Segment> segments, int lamIndex)
        {
            int after = SegmentNavigator.NextConsonantInWord(segments, lamIndex);
            if (after < 0)
                return false;

            // Лунный артикль: лям с сукуном. Солнечный: шадда на следующей букве —
            // либо, если следующая буква сама лям, шадда прямо на нём (ٱلَّذِينَ).
            return segments[lamIndex].Vowel == Harakah.Sukun
                   || segments[lamIndex].Shadda
                   || segments[after].Shadda;
        }

        /// <summary>Согласные слова после хамзы — по ним опознаются исключения.</summary>
        private static string WordSkeleton(IList<Segment> segments, int waslIndex)
        {
            var letters = new List<string>();

            for (int i = waslIndex + 1; i < segments.Count; i++)
            {
                if (segments[i].Kind == SegmentKind.Break || segments[i].Kind == SegmentKind.Digit)
                    break;
                if (segments[i].Kind != SegmentKind.Consonant)
                    continue;
                if (segments[i].FromTanwin)
                    continue;

                letters.Add(segments[i].Letter);
            }

            return string.Concat(letters);
        }

        private static int PreviousPronouncedConsonant(IList<Segment> segments, int index)
        {
            for (int i = index - 1; i >= 0; i--)
            {
                if (segments[i].Kind == SegmentKind.Consonant && !segments[i].Silent)
                    return i;
                if (segments[i].Kind == SegmentKind.Digit)
                    return -1;
            }

            return -1;
        }
    }
}
