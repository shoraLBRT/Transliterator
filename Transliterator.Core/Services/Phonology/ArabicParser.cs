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

            /// <summary>Над буквой только знаки долготы — надстрочный алиф и маддах, без огласовки.</summary>
            public bool CarriesOnlyLength => Marks.Count > 0
                                             && Marks.All(m => m is ArabicScript.SuperscriptAlef or ArabicScript.Maddah);
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

            var clusters = BuildClusters(normalizedText);
            DetectImlaiWasl(clusters);

            foreach (var cluster in clusters)
            {
                if (ArabicScript.IsWaqfMark(cluster.Base))
                {
                    pendingWaqf = DecodeWaqfMark(cluster.Base);
                    continue;
                }

                if (char.IsWhiteSpace(cluster.Base))
                {
                    var boundary = AppendBreak(segments);

                    // Перевод строки нормализация сохранила; граница, на которую он
                    // пришёлся, остаётся одной и той же, но пишется переводом строки.
                    if (cluster.Base == '\n')
                        boundary.Literal = Segment.LineBreakLiteral;

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
                    if (!cluster.IsBare)
                        return false;
                    if (previous.Vowel == Harakah.Fatha)
                        Lengthen(previous, Harakah.Fatha, maddLength);
                    return true;

                // Алиф максура — тоже долгота предыдущей огласовки, но не одной
                // фатхи: в усмани ى пишут и на месте долгой ī (فِى, ٱلَّذِى, أَبِى),
                // и тогда предыдущая огласовка — касра. Даммы перед ней не бывает:
                // долгую ū пишут только و. Своей огласовки у голой максуры нет,
                // и всё, чем она может быть, — это долгота или немота.
                // Надстрочный алиф на ней (أَغْنَىٰ, عَلَىٰ) звука не добавляет: он лишь
                // явно пишет ту же ā, которую голая максура после фатхи даёт и так.
                // Своим сегментом он стал бы второй фатхой, и «а» + «аа» слиплись бы
                // в «ааа» — на письме мадд в четыре хараката, которого нет.
                case ArabicScript.AlefMaqsura:
                    if (cluster.IsBare)
                    {
                        if (previous.Vowel is Harakah.Fatha or Harakah.Kasra)
                            Lengthen(previous, previous.Vowel, maddLength);
                        return true;
                    }
                    if (!cluster.CarriesOnlyLength || previous.Vowel != Harakah.Fatha)
                        return false;
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
        /// чтобы правила нун сакины (стадия 7) работали с ним наравне с написанным нуном,
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
                return ArabicScript.AlefStr;

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
        /// В современной орфографии хамзат аль-васль пишется обычным алифом (الحمد,
        /// اهدنا), а не васлевым (ٱلحمد, ٱهدنا). Опознаём такой алиф и меняем его на ٱ,
        /// чтобы дальше по конвейеру доехало одно написание, а не два.
        /// <para>
        /// Решать приходится до разбора, на кластерах: голый алиф своего согласного
        /// не имеет, и за приросшей буквой (وَالْفَتْحُ) разбор сразу отдал бы его
        /// в долготу её фатхи — сегмента, который можно было бы переписать, не осталось бы.
        /// </para>
        /// <para>
        /// Признак васли — безгласный согласный сразу за ней: ради него она и пишется,
        /// начать слово с безгласного нельзя. Долгая ā перед безгласным внутри слова
        /// не встречается, кроме мадда лязим перед удвоением, и отсюда все границы:
        /// </para>
        /// <list type="bullet">
        ///   <item>до алифа в слове — только приставки (<see cref="ArabicScript.Proclitics"/>)
        ///         с краткой гласной. Иначе алиф стоит внутри слова и значит долготу;</item>
        ///   <item>за алифом — артикль (<see cref="IsArticleLam"/>) или согласный
        ///         с сукуном или шаддой, и после него слово продолжается;</item>
        ///   <item>шадда без ляма артикля засчитывается, только если перед алифом нет
        ///         приставки или приставка глагольная. Слияние в начале слова с васлей
        ///         бывает лишь у глагола (ٱتَّقُوا), а за ب и ك алиф перед шаддой —
        ///         мадд лязим имени: كَافَّةً;</item>
        ///   <item>огласовку на алифе допускает только артикль (اَلْحَمْدُ). Фатха
        ///         на голом алифе перед безгласным вне артикля — это хамза, у которой
        ///         не написали носитель (اَنْتُمْ), а не васля.</item>
        /// </list>
        /// <para>
        /// Носителем васли признаётся только голый алиф (ا). Носитель хамзы
        /// (أ, إ) — это хамзат аль-qотI, она произносится всегда, и васлей
        /// не бывает: أَلَمْ, إِلَّا, أَلْقَى, وَأَنْتُمْ. Та же граница проведена
        /// в <c>ArabicNormalizer</c>, где имя Аллаха опознаётся по ляму после ا или ٱ,
        /// но не после أ. Хамзу, у которой носитель не написан вовсе (انْتُمْ), от васли
        /// по письму не отличить, и это уже не орфография, а опечатка.
        /// </para>
        /// </summary>
        private static void DetectImlaiWasl(List<Cluster> clusters)
        {
            for (int i = 0; i < clusters.Count; i++)
            {
                var alef = clusters[i];
                if (alef.Base != ArabicScript.Alef) continue;
                if (!FollowsOnlyProclitics(clusters, i, out var proclitic)) continue;

                int second = NextLetterInWord(clusters, i);
                if (second < 0) continue;
                int third = NextLetterInWord(clusters, second);
                if (third < 0) continue;

                bool isWasl = IsArticleLam(clusters[second], clusters[third])
                    ? alef.Marks.All(m => m == ArabicScript.Fatha)
                    : alef.Marks.Count == 0 && IsSakinAfterWasl(clusters[second], proclitic);

                if (isWasl)
                    alef.Base = ArabicScript.AlefWasla;
            }
        }

        /// <summary>
        /// До алифа в его слове стоят только приставки с краткой гласной — или ничего.
        /// Через <paramref name="proclitic"/> возвращает ближайшую к алифу приставку.
        /// </summary>
        private static bool FollowsOnlyProclitics(List<Cluster> clusters, int index, out Cluster? proclitic)
        {
            proclitic = null;

            for (int i = index - 1; i >= 0 && ArabicScript.Consonants.Contains(clusters[i].Base); i--)
            {
                var letter = clusters[i];
                if (!ArabicScript.Proclitics.Contains(letter.Base)) return false;
                if (letter.Marks.Count != 1 || letter.Marks[0] is not (ArabicScript.Fatha or ArabicScript.Kasra))
                    return false;

                proclitic ??= letter;
            }

            return true;
        }

        /// <summary>
        /// Лям артикля перед телом слова. Способов написать его три, и все три —
        /// про одно и то же: лям артикля собственной огласовки не имеет.
        /// <list type="bullet">
        ///   <item>сукун — лунная буква, лям звучит: ٱلْحَمْدُ;</item>
        ///   <item>шадда на следующей букве — солнечная, лям в неё ушёл: ٱلنَّاسِ;</item>
        ///   <item>шадда на самом ляме — солнечная буква и есть лям, и удвоение
        ///         мусхаф пишет на нём: ٱلَّذِي, ٱلَّيْلِ. Сукуна на таком ляме нет,
        ///         а огласовка на нём — от следующего слога, не его собственная.</item>
        /// </list>
        /// </summary>
        private static bool IsArticleLam(Cluster lam, Cluster after) =>
            lam.Base == ArabicScript.Lam
            && (lam.Has(ArabicScript.Sukun) || lam.Has(ArabicScript.Shadda) || after.Has(ArabicScript.Shadda));

        /// <summary>
        /// Безгласный согласный за васлей вне артикля: сукун (ٱهْدِنَا, ٱسْتَغْفِرْ) или
        /// первая половина удвоения (ٱتَّقُوا) — последняя только у глагола.
        /// </summary>
        private static bool IsSakinAfterWasl(Cluster letter, Cluster? proclitic) =>
            letter.Has(ArabicScript.Sukun)
            || (letter.Has(ArabicScript.Shadda)
                && (proclitic is null || ArabicScript.VerbProclitics.Contains(proclitic.Base)));

        /// <summary>
        /// Следующая буква того же слова, иначе -1. Граница слова та же, что у разбора:
        /// пробел, знак вакфа, цифра; прочие знаки пропускаются, как их пропускает
        /// <c>SegmentNavigator.NextConsonantInWord</c>.
        /// </summary>
        private static int NextLetterInWord(List<Cluster> clusters, int index)
        {
            for (int i = index + 1; i < clusters.Count; i++)
            {
                var c = clusters[i].Base;
                if (ArabicScript.Consonants.Contains(c))
                    return i;
                if (char.IsWhiteSpace(c) || ArabicScript.IsWaqfMark(c) || char.IsDigit(c) || ArabicScript.IsArabicDigit(c))
                    return -1;
            }

            return -1;
        }

    }
}
