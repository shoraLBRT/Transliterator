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
    /// <para>
    /// Мадд бадаль (долгая гласная сразу после хамзы: ءَامَنَ, إِيمَان) своей ветки
    /// не имеет и не должен её иметь: он тянется ровно два хараката — столько,
    /// сколько долгой гласной уже дал разбор. Всё правило в том, чтобы такую гласную
    /// не удлинить сверх двух, а для этого довольно порядка букв: хамза стоит
    /// <i>перед</i> гласной, тогда как муттасиль и мунфасиль требуют хамзы <i>после</i>.
    /// </para>
    /// </summary>
    public class MaddRule
    {
        private const int Natural = 2;
        private const int Obligatory = 4;
        private const int Lazim = 6;

        /// <summary>
        /// Мадд арид тянут на 2, 4 или 6 харакатов — дозволены все три чтения.
        /// Берём среднее: оно чаще всего и звучит в размеренном чтении, и при нём
        /// мадд арид остаётся отличим от естественного мадда в 2 хараката.
        /// <para>
        /// Тем же счётом читают и мадд лин: длиннее арида он быть не может,
        /// а разнобой между ними в одном чтении слышен как ошибка.
        /// </para>
        /// </summary>
        private const int Arid = 4;

        /// <summary>
        /// Слова, где местоименная ه сили не получает, хотя все условия налицо:
        /// يَرْضَهُ لَكُمْ Хафс читает короткой даммой.
        /// <para>Ключ — скелет слова из согласных, как в <c>WaslRule</c>.</para>
        /// </summary>
        private static readonly HashSet<string> NoSilaWords = new()
        {
            "يرضه"   // يَرْضَهُ
        };

        /// <summary>
        /// فِيهِ مُهَانًا — единственное место, где силя читается вопреки условию:
        /// перед ه там стоит долгая ī, а не огласованная буква.
        /// <para>
        /// Скелет здесь короче написанного: долгие гласные своего сегмента не имеют
        /// и живут в длительности предыдущего согласного — оттого فِيهِ это «فه»,
        /// а مُهَانًا — «مهن».
        /// </para>
        /// </summary>
        private const string SilaAfterMaddWord = "فه";

        private const string SilaAfterMaddNextWord = "مهن";

        public void Apply(IList<Segment> segments)
        {
            for (int i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];
                if (segment.Kind != SegmentKind.Consonant)
                    continue;

                // Мадд лин: тянется сам безгласный глайд, а не фатха при нём,
                // поэтому длительность садится на глайд и ветки ниже к нему
                // уже не относятся — своей гласной у него нет.
                if (IsLinGlide(segments, i))
                {
                    segment.VowelLength = Arid;
                    continue;
                }

                // Мадд силя сугра. Силя кубра (4 хараката перед хамзой) отдельной
                // ветки не требует: силя делает ه долгой, а дальше её удлиняет
                // общее правило мунфасиля — оно и так стоит ниже.
                if (IsSila(segments, i))
                    segment.VowelLength = Math.Max(segment.VowelLength, Natural);

                if (segment.VowelLength < Natural)
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

                // Мадд арид лис-сукун: пауза обеззвучила конечный согласный, и долгота
                // перед ним растягивается. Признаком служит снятая паузой огласовка,
                // а не сам сукун: написанный сукун (عَلَيْهِمْ) удлинения не даёт — он
                // не «случайный», слог закрыт им и в слитном чтении.
                if (next.Vowel == Harakah.Sukun && next.OriginalVowel != Harakah.None
                                                && segment.VowelLength < Arid)
                    segment.VowelLength = Arid;
            }
        }

        /// <summary>
        /// Буква лин: و или ي с сукуном после краткой фатхи. Сама по себе это просто
        /// дифтонг, и удлинять в нём нечего. Маддом он становится только на паузе:
        /// остановка обеззвучивает следующий согласный, слог оказывается закрыт
        /// внезапно — и голос отыгрывается на глайде.
        /// <para>
        /// Признак тот же, что у мадда арид: сукун, <i>наведённый</i> паузой.
        /// Написанный сукун удлинения не даёт — с ним слог закрыт и в слитном
        /// чтении: عَلَيْهِمْ читается без мадда, а خَوْفٌ на паузе — с маддом.
        /// </para>
        /// </summary>
        private static bool IsLinGlide(IList<Segment> segments, int index)
        {
            var segment = segments[index];
            if (segment.Vowel != Harakah.Sukun || segment.Letter.Length == 0)
                return false;

            if (segment.Letter[0] is not (ArabicScript.Waw or ArabicScript.Yeh))
                return false;

            int previous = SegmentNavigator.PreviousConsonantInWord(segments, index);
            if (previous < 0 || segments[previous].Vowel != Harakah.Fatha
                             || segments[previous].VowelLength > 1)
                return false;

            int next = SegmentNavigator.NextConsonantInWord(segments, index);
            return next >= 0 && segments[next].Vowel == Harakah.Sukun
                             && segments[next].OriginalVowel != Harakah.None;
        }

        /// <summary>
        /// Местоименная ه («его», «него») в конце слова, зажатая между двумя
        /// огласованными буквами. В мусхафе её долгота размечена малыми ۥ и ۦ,
        /// и тогда разбор даёт её сам; в современной орфографии этих знаков нет,
        /// поэтому условие проверяется по самим сегментам.
        /// <para>
        /// Долгая гласная перед ه силю отменяет (فِيهِ, ٱللَّٰهُ — там ه вообще коренная),
        /// сукун перед ней — тоже (مِنْهُ, عَلَيْهِ), и безгласная буква после неё
        /// (لَهُ ٱلْمُلْكُ): силя живёт только между двумя движениями голоса.
        /// </para>
        /// </summary>
        private static bool IsSila(IList<Segment> segments, int index)
        {
            var segment = segments[index];
            if (segment.Letter != ArabicScript.HaStr || segment.Shadda)
                return false;

            // Местоимение — это هُ и هِ. Фатхи у него не бывает.
            if (segment.Vowel is not (Harakah.Damma or Harakah.Kasra))
                return false;

            // И это последняя буква слова.
            if (SegmentNavigator.NextConsonantInWord(segments, index) >= 0)
                return false;

            // Дальше должно звучать слово: перед паузой конечную огласовку и так
            // снимает вакф, а перед безгласной буквой второго движения голоса нет.
            int next = NextPronouncedConsonant(segments, index);
            if (next < 0 || segments[next].Vowel is Harakah.None or Harakah.Sukun)
                return false;

            var word = WordSkeleton(segments, index);
            if (NoSilaWords.Contains(word))
                return false;

            int previous = SegmentNavigator.PreviousConsonantInWord(segments, index);
            if (previous < 0)
                return false;

            var before = segments[previous];
            if (before.Vowel is Harakah.Fatha or Harakah.Damma or Harakah.Kasra
                && before.VowelLength == 1)
                return true;

            return word == SilaAfterMaddWord
                   && WordSkeleton(segments, next) == SilaAfterMaddNextWord;
        }

        /// <summary>
        /// Следующий звучащий согласный, хоть бы и в соседнем слове. Немую васлю
        /// пропускает: в لَهُ ٱلْمُلْكُ за ه звучит безгласный лям, а не она.
        /// Через паузу, как и все, не смотрит.
        /// </summary>
        private static int NextPronouncedConsonant(IList<Segment> segments, int index)
        {
            int next = SegmentNavigator.NextConsonant(segments, index, crossWordBoundary: true);

            while (next >= 0 && segments[next].Silent)
                next = SegmentNavigator.NextConsonant(segments, next, crossWordBoundary: true);

            return next;
        }

        /// <summary>
        /// Согласные слова, которому принадлежит сегмент. Нун от танвина в скелет
        /// не входит — он часть огласовки, а не написания.
        /// </summary>
        private static string WordSkeleton(IList<Segment> segments, int index)
        {
            int start = index;
            while (start > 0 && segments[start - 1].Kind == SegmentKind.Consonant)
                start--;

            var letters = new List<string>();

            for (int i = start; i < segments.Count && segments[i].Kind == SegmentKind.Consonant; i++)
                if (!segments[i].FromTanwin)
                    letters.Add(segments[i].Letter);

            return string.Concat(letters);
        }
    }
}
