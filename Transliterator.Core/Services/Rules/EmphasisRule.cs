using Transliterator.Core.Services.Phonology;
using Transliterator.Domain.Phonology;

namespace Transliterator.Core.Services.Rules
{
    /// <summary>
    /// Стадия 7 конвейера: тафхим и таркик.
    /// <para>
    /// Стоит после всех ассимиляций, потому что идгам создаёт и разрушает условия
    /// эмфазы: в "مِن رَّبِّهِمْ" твёрдость ر определяется только после слияния нуна.
    /// И после разметки пауз: у "ٱلْفَجْرِ" на паузе ر оказывается безгласной,
    /// но остаётся мягкой — по исходной касре.
    /// </para>
    /// </summary>
    public class EmphasisRule
    {
        public void Apply(IList<Segment> segments)
        {
            for (int i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];
                if (segment.Kind != SegmentKind.Consonant)
                    continue;

                segment.Emphasis = ResolveEmphasis(segments, i);
            }

            // Удвоение — один звук, разложенный на два сегмента, и решение у них
            // общее. Принимает его половина с огласовкой: безгласная первая иначе
            // рассуждала бы по соседям и в "مِن رَّبِّهِمْ" осталась бы мягкой,
            // хотя произносится та же твёрдая ر, что и во второй половине.
            for (int i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];
                if (segment.Kind != SegmentKind.Consonant || !segment.IsGeminateFirstHalf)
                    continue;

                int second = SegmentNavigator.NextConsonant(segments, i, crossWordBoundary: true);
                if (second >= 0)
                    segment.Emphasis = segments[second].Emphasis;
            }

            for (int i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];
                if (segment.Kind != SegmentKind.Consonant)
                    continue;

                segment.VowelVariant = ResolveVowelVariant(segments, i);
            }
        }

        private static Emphasis ResolveEmphasis(IList<Segment> segments, int index)
        {
            var segment = segments[index];
            if (segment.Letter.Length == 0)
                return Emphasis.Light;

            char letter = segment.Letter[0];

            if (ArabicScript.AlwaysHeavy.Contains(letter))
                return Emphasis.Heavy;

            if (letter == ArabicScript.Ra)
                return ResolveRa(segments, index);

            if (letter == ArabicScript.Lam)
                return ResolveLam(segments, index);

            return Emphasis.Light;
        }

        /// <summary>
        /// ر твёрдая при фатхе и дамме, мягкая при касре — по своей огласовке,
        /// в том числе по той, которую с неё уже сняла пауза. Настоящая безгласная ر
        /// берёт твёрдость у предыдущей огласовки, но твёрдеет, если дальше в слове
        /// стоит буква истиля: قِرْطَاس, مِرْصَاد.
        /// </summary>
        private static Emphasis ResolveRa(IList<Segment> segments, int index)
        {
            var segment = segments[index];

            switch (segment.Vowel)
            {
                case Harakah.Fatha:
                case Harakah.Damma:
                    return Emphasis.Heavy;

                case Harakah.Kasra:
                    return Emphasis.Light;
            }

            // Огласовку могло снять правило вакфа. Звучать ей больше нечем,
            // но качество согласного она задала ещё до паузы: ٱلْفَجْرِ на остановке
            // читается с мягкой ر — по своей касре, а не по соседям, у которых
            // на этом месте стоит сукун предыдущей буквы.
            switch (segment.OriginalVowel)
            {
                case Harakah.Fatha:
                case Harakah.Damma:
                    return Emphasis.Heavy;

                case Harakah.Kasra:
                    return Emphasis.Light;
            }

            int previous = SegmentNavigator.PreviousConsonantInWord(segments, index);
            if (previous < 0 || segments[previous].Vowel != Harakah.Kasra)
                return Emphasis.Heavy;

            int next = SegmentNavigator.NextConsonantInWord(segments, index);
            if (next >= 0 && segments[next].Letter.Length > 0
                          && ArabicScript.AlwaysHeavy.Contains(segments[next].Letter[0]))
                return Emphasis.Heavy;

            return Emphasis.Light;
        }

        /// <summary>
        /// ل твёрдая только в имени Аллаха и только после фатхи или даммы:
        /// "Аллаh", но "лилляhи". Этим заменяется прежний строковый хак,
        /// который искал в кириллице подстроку "Аллах" и не срабатывал никогда.
        /// </summary>
        private static Emphasis ResolveLam(IList<Segment> segments, int index)
        {
            if (!IsLamOfAllah(segments, index))
                return Emphasis.Light;

            int previous = PrecedingVowelCarrier(segments, index);
            if (previous < 0)
                return Emphasis.Heavy;

            return segments[previous].Vowel is Harakah.Fatha or Harakah.Damma
                ? Emphasis.Heavy
                : Emphasis.Light;
        }

        /// <summary>
        /// Имя Аллаха: удвоенный лям с долгой ā, за которым следует ه.
        /// Покрывает и ٱللَّه, и لِلَّه, и بِٱللَّه.
        /// <para>
        /// Смотрим на ту половину удвоения, что несёт огласовку: удвоение выражено
        /// то шаддой (لِلَّه), то двумя сегментами — если лям артикля уже слился
        /// со вторым лямом (ٱللَّه). Решение потом получат обе половины.
        /// </para>
        /// </summary>
        private static bool IsLamOfAllah(IList<Segment> segments, int index)
        {
            var lam = segments[index];
            if (lam.Vowel != Harakah.Fatha || lam.VowelLength < 2)
                return false;

            int previous = SegmentNavigator.PreviousConsonant(segments, index, crossWordBoundary: true);
            bool doubled = lam.Shadda
                           || (previous >= 0
                               && segments[previous].IsGeminateFirstHalf
                               && segments[previous].Letter == ArabicScript.LamStr);
            if (!doubled)
                return false;

            int next = SegmentNavigator.NextConsonantInWord(segments, index);
            return next >= 0 && segments[next].Letter == "ه";
        }

        /// <summary>
        /// Согласный, чья огласовка решает судьбу ляма имени Аллаха. Немая васля
        /// и первая половина удвоения пропускаются: своей огласовки у них нет,
        /// а решает именно она — "qооля-ллаааh", но "бисми-лляяяh".
        /// </summary>
        private static int PrecedingVowelCarrier(IList<Segment> segments, int index)
        {
            int previous = SegmentNavigator.PreviousConsonant(segments, index, crossWordBoundary: true);

            while (previous >= 0
                   && (segments[previous].Silent || segments[previous].IsGeminateFirstHalf))
                previous = SegmentNavigator.PreviousConsonant(segments, previous, crossWordBoundary: true);

            return previous;
        }

        /// <summary>
        /// Твёрдость окрашивает гласную: и свою, и предыдущую, если сам согласный безгласен
        /// (بَرْ → "бор"). Мягкий лям, наоборот, смягчает свою гласную: "ля", а не "ла".
        /// Конкретные графемы задаёт профиль, а не это правило.
        /// </summary>
        private static VowelVariant ResolveVowelVariant(IList<Segment> segments, int index)
        {
            var segment = segments[index];

            if (segment.Emphasis == Emphasis.Heavy)
                return segment.Letter[0] == ArabicScript.Lam ? VowelVariant.Plain : VowelVariant.Heavy;

            int next = SegmentNavigator.NextConsonantInWord(segments, index);
            if (next >= 0)
            {
                var following = segments[next];
                bool followingIsClosing = following.Vowel is Harakah.Sukun or Harakah.None;
                if (followingIsClosing && !following.IsGeminateFirstHalf
                                       && following.Emphasis == Emphasis.Heavy
                                       && following.Letter[0] != ArabicScript.Lam)
                    return VowelVariant.Heavy;
            }

            if (segment.Letter[0] == ArabicScript.Lam)
                return VowelVariant.Soft;

            return VowelVariant.Plain;
        }
    }
}
