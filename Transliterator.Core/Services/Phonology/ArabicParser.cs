using Transliterator.Domain.Phonology;

namespace Transliterator.Core.Services.Phonology
{
    /// <summary>
    /// Стадия 2 конвейера: разбор нормализованного арабского текста в поток сегментов.
    /// <para>Здесь снимаются неоднозначности письма, а не звука:</para>
    /// <list type="bullet">
    ///   <item>носители хамзы (أ إ ؤ ئ آ) сводятся к одному согласному ء со своей огласовкой;</item>
    ///   <item>ا و ي опознаются как долгая гласная, дифтонг или согласный;</item>
    ///   <item>танвин разворачивается в «краткая гласная + нун сакин», чтобы правила
    ///         нун сакины позже не пришлось дублировать для танвина;</item>
    ///   <item>сукун становится явным состоянием, а не пустой строкой.</item>
    /// </list>
    /// </summary>
    public class ArabicParser
    {
        private sealed class Cluster
        {
            public char Base;
            public readonly List<char> Marks = new();

            public bool Has(char mark) => Marks.Contains(mark);
            public bool IsBare => Marks.Count == 0
                                  || (Marks.Count == 1 && Marks[0] == ArabicScript.Maddah);
        }

        public List<Segment> Parse(string normalizedText)
        {
            var segments = new List<Segment>();
            if (string.IsNullOrEmpty(normalizedText))
                return segments;

            // Знак вакфа стоит на границе слов, а не при букве, и в мусхафе может
            // оказаться как вплотную к слову, так и между пробелами. Поэтому он
            // не порождает свой сегмент, а откладывается и садится на ту границу,
            // которая отделит его от следующего слова.
            var pendingWaqf = WaqfMark.None;

            foreach (var cluster in BuildClusters(normalizedText))
            {
                if (ArabicScript.IsWaqfMark(cluster.Base))
                {
                    pendingWaqf = DecodeWaqfMark(cluster.Base);
                    continue;
                }

                if (char.IsWhiteSpace(cluster.Base))
                {
                    AppendBreak(segments);
                    continue;
                }

                if (pendingWaqf != WaqfMark.None)
                {
                    AppendBreak(segments).Waqf = pendingWaqf;
                    pendingWaqf = WaqfMark.None;
                }

                if (ArabicScript.IsArabicDigit(cluster.Base) || char.IsDigit(cluster.Base))
                {
                    segments.Add(new Segment
                    {
                        Kind = SegmentKind.Digit,
                        Literal = cluster.Base.ToString()
                    });
                    continue;
                }

                if (!ArabicScript.Consonants.Contains(cluster.Base))
                {
                    segments.Add(new Segment
                    {
                        Kind = SegmentKind.Other,
                        Literal = cluster.Base.ToString()
                    });
                    continue;
                }

                if (TryFoldLongVowel(cluster, segments))
                    continue;

                AppendConsonant(cluster, segments);
            }

            // Знак вакфа в самом конце текста разметки не добавляет:
            // конец текста и так пауза.

            MarkWordStarts(segments);
            DetectImlaiWasl(segments);
            return segments;
        }

        private static WaqfMark DecodeWaqfMark(char mark) => mark switch
        {
            ArabicScript.WaqfContinuePreferred => WaqfMark.ContinuePreferred,
            ArabicScript.WaqfStopPreferred => WaqfMark.StopPreferred,
            ArabicScript.WaqfObligatory => WaqfMark.Obligatory,
            ArabicScript.WaqfForbidden => WaqfMark.Forbidden,
            ArabicScript.WaqfPermissible => WaqfMark.Permissible,
            ArabicScript.WaqfEmbracing => WaqfMark.Embracing,
            ArabicScript.WaqfSaktah => WaqfMark.Saktah,
            _ => WaqfMark.None
        };

        /// <summary>
        /// Граница слов. Двух подряд не бывает: пробел вокруг знака вакфа —
        /// это одна и та же граница, и знак должен сесть именно на неё.
        /// </summary>
        private static Segment AppendBreak(List<Segment> segments)
        {
            if (segments.Count > 0 && segments[^1].Kind == SegmentKind.Break)
                return segments[^1];

            var boundary = Segment.Break();
            segments.Add(boundary);
            return boundary;
        }

        // ------------------------------------------------------------------
        // Разбиение на кластеры «носитель + его диакритика»
        // ------------------------------------------------------------------
        private static List<Cluster> BuildClusters(string text)
        {
            var clusters = new List<Cluster>();
            Cluster? current = null;

            foreach (var c in text)
            {
                if (ArabicScript.IsDiacritic(c))
                {
                    current?.Marks.Add(c);
                    continue;
                }

                current = new Cluster { Base = c };
                clusters.Add(current);
            }

            return clusters;
        }

        // ------------------------------------------------------------------
        // Долгие гласные: буква сливается с огласовкой предыдущего согласного
        // ------------------------------------------------------------------
        private static bool TryFoldLongVowel(Cluster cluster, List<Segment> segments)
        {
            var previous = LastConsonantInWord(segments);
            if (previous is null)
                return false;

            int maddLength = cluster.Has(ArabicScript.Maddah) ? 4 : 2;

            switch (cluster.Base)
            {
                // Голый алиф собственного звука не имеет. Он либо удлиняет фатху,
                // либо нем — алиф аль-фарика после глагольного "ـوا" (أُوتُوا, ٱدْخُلُوا).
                case ArabicScript.Alef:
                case ArabicScript.AlefMaqsura:
                    if (!cluster.IsBare)
                        return false;
                    if (previous.Vowel == Harakah.Fatha)
                        Lengthen(previous, Harakah.Fatha, maddLength);
                    return true;

                // آ в середине слова после фатхи — не хамза, а удлинённая ā
                // с обязательным маддом: ٱلضَّآلِّينَ, جَآءَ.
                case ArabicScript.AlefMadda:
                    if (previous.Vowel != Harakah.Fatha)
                        return false;
                    Lengthen(previous, Harakah.Fatha, 4);
                    return true;

                // Долгая ū: و без огласовки после даммы.
                // و с сукуном после фатхи — дифтонг "ау", он остаётся согласным.
                case ArabicScript.Waw:
                    if (!cluster.IsBare || previous.Vowel != Harakah.Damma)
                        return false;
                    Lengthen(previous, Harakah.Damma, maddLength);
                    return true;

                // Долгая ī: ي без огласовки после касры.
                // ي с сукуном после фатхи — дифтонг "ай", он остаётся согласным.
                case ArabicScript.Yeh:
                    if (!cluster.IsBare || previous.Vowel != Harakah.Kasra)
                        return false;
                    Lengthen(previous, Harakah.Kasra, maddLength);
                    return true;

                default:
                    return false;
            }
        }

        private static void Lengthen(Segment segment, Harakah vowel, int length)
        {
            segment.Vowel = vowel;
            segment.VowelLength = Math.Max(segment.VowelLength, length);
        }

        /// <summary>Последний согласный текущего слова — за границу слова заглядывать нельзя.</summary>
        private static Segment? LastConsonantInWord(List<Segment> segments)
        {
            for (int i = segments.Count - 1; i >= 0; i--)
            {
                if (segments[i].Kind == SegmentKind.Consonant)
                    return segments[i];
                if (segments[i].Kind != SegmentKind.Other)
                    return null;
            }

            return null;
        }

        // ------------------------------------------------------------------
        // Построение согласного сегмента
        // ------------------------------------------------------------------
        private static void AppendConsonant(Cluster cluster, List<Segment> segments)
        {
            var segment = new Segment
            {
                Letter = CanonicalLetter(cluster.Base),
                Shadda = cluster.Has(ArabicScript.Shadda),
                IsWaslHamza = cluster.Base == ArabicScript.AlefWasla,
                IsTaMarbuta = cluster.Base == ArabicScript.TaMarbuta,
                Silent = cluster.Has(ArabicScript.SmallHighRoundedZero)
                         || cluster.Has(ArabicScript.SmallHighUprightZero)
            };

            ApplyVowel(cluster, segment);

            // Знак икляба заменяет собой сукун: в مِنۢ огласовки не написано вовсе,
            // и без этой строки нун остался бы «без огласовки», а не безгласным.
            if (cluster.Has(ArabicScript.SmallHighMeemIsolated) && segment.Vowel == Harakah.None)
                segment.Vowel = Harakah.Sukun;

            // آ в начале слова — это хамза с долгой ā (آمَنَ).
            if (cluster.Base == ArabicScript.AlefMadda)
            {
                segment.Vowel = Harakah.Fatha;
                segment.VowelLength = Math.Max(segment.VowelLength, 2);
            }

            if (cluster.Has(ArabicScript.Maddah) && segment.VowelLength < 4)
                segment.VowelLength = 4;

            if (segment.Vowel == Harakah.None)
                ApplyCarrierDefaultVowel(cluster.Base, segment);

            // Шадда на نّ и مّ всегда даёт гунну.
            if (segment.Shadda && segment.Letter is ArabicScript.NunStr or ArabicScript.MeemStr)
                segment.Ghunna = true;

            segments.Add(segment);

            AppendTanwinNun(cluster, segments);
        }

        private static void ApplyVowel(Cluster cluster, Segment segment)
        {
            if (cluster.Has(ArabicScript.Fatha)) segment.Vowel = Harakah.Fatha;
            else if (cluster.Has(ArabicScript.Damma)) segment.Vowel = Harakah.Damma;
            else if (cluster.Has(ArabicScript.Kasra)) segment.Vowel = Harakah.Kasra;
            else if (cluster.Has(ArabicScript.Sukun)) segment.Vowel = Harakah.Sukun;
            else if (cluster.Has(ArabicScript.Fathatan)) segment.Vowel = Harakah.Fatha;
            else if (cluster.Has(ArabicScript.Dammatan)) segment.Vowel = Harakah.Damma;
            else if (cluster.Has(ArabicScript.Kasratan)) segment.Vowel = Harakah.Kasra;

            // Надстрочный алиф — долгая ā поверх фатхи: مَـٰلِكِ, ٱلرَّحْمَـٰنِ
            if (cluster.Has(ArabicScript.SuperscriptAlef))
            {
                segment.Vowel = Harakah.Fatha;
                segment.VowelLength = Math.Max(segment.VowelLength, 2);
            }
        }

        /// <summary>
        /// Огласовка по умолчанию для носителя хамзы в неогласованном тексте.
        /// В огласованном мусхафе не срабатывает — там огласовка проставлена явно,
        /// поэтому أُنزِلَ читается через дамму, а не через фатху носителя.
        /// </summary>
        private static void ApplyCarrierDefaultVowel(char baseChar, Segment segment)
        {
            segment.Vowel = baseChar switch
            {
                ArabicScript.AlefHamzaAbove or ArabicScript.AlefWavyHamzaAbove => Harakah.Fatha,
                ArabicScript.AlefHamzaBelow or ArabicScript.AlefWavyHamzaBelow => Harakah.Kasra,
                _ => segment.Vowel
            };
        }

        /// <summary>
        /// Танвин — это краткая гласная плюс нун сакин. Разворачиваем его здесь,
        /// чтобы правила нун сакины (стадия 6) работали с ним наравне с написанным нуном,
        /// а стадия вакфа могла его снять.
        /// </summary>
        private static void AppendTanwinNun(Cluster cluster, List<Segment> segments)
        {
            bool hasTanwin = cluster.Has(ArabicScript.Fathatan)
                             || cluster.Has(ArabicScript.Dammatan)
                             || cluster.Has(ArabicScript.Kasratan);

            if (!hasTanwin)
                return;

            segments.Add(new Segment
            {
                Letter = ArabicScript.NunStr,
                Vowel = Harakah.Sukun,
                FromTanwin = true,
                Ghunna = true
            });
        }

        private static string CanonicalLetter(char baseChar)
        {
            if (ArabicScript.HamzaCarriers.Contains(baseChar)
                || baseChar == ArabicScript.AlefMadda
                || baseChar == ArabicScript.AlefWasla)
                return ArabicScript.HamzaStr;

            if (baseChar == ArabicScript.AlefMaqsura)
                return ArabicScript.Alef.ToString();

            return baseChar.ToString();
        }

        // ------------------------------------------------------------------
        // Проходы по готовому потоку
        // ------------------------------------------------------------------
        private static void MarkWordStarts(List<Segment> segments)
        {
            bool atWordStart = true;

            foreach (var segment in segments)
            {
                if (segment.Kind != SegmentKind.Consonant)
                {
                    atWordStart = true;
                    continue;
                }

                segment.StartsWord = atWordStart;
                atWordStart = false;
            }
        }

        /// <summary>
        /// В современной орфографии артикль пишется обычным алифом (الحمد), а не
        /// васлевым (ٱلحمد). Опознаём такой алиф, чтобы правило васли работало
        /// не только на тексте в написании усмани.
        /// </summary>
        private static void DetectImlaiWasl(List<Segment> segments)
        {
            for (int i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];
                if (segment.Kind != SegmentKind.Consonant) continue;
                if (!segment.StartsWord || segment.IsWaslHamza) continue;
                if (segment.Letter != ArabicScript.HamzaStr) continue;
                if (segment.Vowel is not (Harakah.None or Harakah.Fatha)) continue;

                int lamIndex = SegmentNavigator.NextConsonantInWord(segments, i);
                if (lamIndex < 0 || segments[lamIndex].Letter != ArabicScript.LamStr) continue;

                int afterIndex = SegmentNavigator.NextConsonantInWord(segments, lamIndex);
                if (afterIndex < 0) continue;
                if (segments[lamIndex].Vowel != Harakah.Sukun && !segments[afterIndex].Shadda) continue;

                segment.IsWaslHamza = true;
            }
        }

    }
}
