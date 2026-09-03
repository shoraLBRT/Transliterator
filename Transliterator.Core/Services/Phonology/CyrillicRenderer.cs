using System.Text;
using Transliterator.Domain.Entities;
using Transliterator.Domain.Phonology;

namespace Transliterator.Core.Services.Phonology
{
    /// <summary>
    /// Стадия 10 конвейера: перевод потока сегментов в кириллицу по профилю.
    /// <para>
    /// Стадия сознательно «глупая»: она ничего не решает про звук, только ищет графему.
    /// Ни одно правило таджвида не должно стоять после неё — именно в этом состояла
    /// исходная ошибка архитектуры.
    /// </para>
    /// <para>Порядок поиска ключа в профиле: вариант → базовая буква → пусто.</para>
    /// </summary>
    public class CyrillicRenderer
    {
        public const string HeavyVariant = "heavy";
        public const string SoftVariant = "soft";
        public const string SukunVariant = "sukun";
        public const string WaqfVariant = "waqf";
        public const string InitialVariant = "initial";
        public const string GhunnaVariant = "ghunna";

        /// <summary>
        /// Сколько раз повторить графему гласной для заданной длительности в харакатах.
        /// Три длительности мадда остаются различимы на письме.
        /// </summary>
        private static int GraphemeCount(int harakat) => harakat switch
        {
            <= 1 => 1,
            2 or 3 => 2,
            4 or 5 => 3,
            _ => 4
        };

        public string Render(IReadOnlyList<Segment> segments, TransliterationProfile profile)
        {
            var result = new StringBuilder();

            foreach (var segment in segments)
            {
                switch (segment.Kind)
                {
                    case SegmentKind.Break:
                        AppendBreak(result, segment.Literal);
                        continue;

                    case SegmentKind.Digit:
                        result.Append(Lookup(profile, segment.Literal) ?? segment.Literal);
                        continue;

                    case SegmentKind.Other:
                        result.Append(segment.Literal);
                        continue;
                }

                if (!segment.Silent)
                {
                    var consonant = RenderConsonant(segment, profile);
                    result.Append(Repeat(consonant, GlideCount(segment)));
                    if (segment.Shadda)
                        result.Append(consonant);

                    result.Append(RenderVowel(segment, profile));
                }

                if (segment.HyphenAfter)
                    AppendHyphen(result);
            }

            return result.ToString();
        }

        private static void AppendBreak(StringBuilder result, string literal)
        {
            if (literal == "-")
            {
                AppendHyphen(result);
                return;
            }

            if (result.Length > 0 && result[^1] != ' ')
                result.Append(' ');
        }

        private static void AppendHyphen(StringBuilder result)
        {
            // Слияние по васле и дефис артикля могут прийтись на одну границу.
            // Дефис в такой позиции нужен ровно один: "бисми-лляhи", а не "бисми-л-ляhи".
            if (result.Length == 0 || result[^1] == '-')
                return;

            result.Append('-');
        }

        private string RenderConsonant(Segment segment, TransliterationProfile profile)
        {
            var letter = segment.Letter;

            // Гортанный приступ в начале слова смыслоразличительным не является
            // и по традиции не пишется: "аль-хIамду", а не "ъаль-хIамду".
            // Профиль может вернуть его, задав непустой "ء|initial".
            if (segment.StartsWord && letter == ArabicScript.HamzaStr
                                   && segment.Vowel is not (Harakah.None or Harakah.Sukun))
            {
                var initial = Lookup(profile, Variant(letter, InitialVariant));
                if (initial is not null)
                    return initial;
            }

            // Та-марбута звучит как /t/ в соединении и как /h/ на паузе.
            // Огласовку с неё снимает стадия вакфа.
            if (segment.IsTaMarbuta && segment.Vowel is Harakah.None or Harakah.Sukun)
            {
                var pausal = Lookup(profile, Variant(letter, WaqfVariant));
                if (pausal is not null)
                    return pausal;
            }

            // Назализованный носовой: ихфа, икляб, идгам с гунной и удвоенные نّ مّ.
            // Отличать ли гунну на письме от обычных н и м — дело профиля;
            // стадии довольно того, что звук помечен.
            if (segment.Ghunna)
            {
                var nasal = Lookup(profile, Variant(letter, GhunnaVariant));
                if (nasal is not null)
                    return nasal;
            }

            // Закрытый слог: ل в "аль-" даёт "ль". Первая половина удвоения при идгаме —
            // не закрытый слог, поэтому этот вариант к ней не применяется.
            if (segment.Vowel == Harakah.Sukun && !segment.IsGeminateFirstHalf)
            {
                var closed = Lookup(profile, Variant(letter, SukunVariant));
                if (closed is not null)
                    return closed;
            }

            if (segment.Emphasis == Emphasis.Heavy)
            {
                var heavy = Lookup(profile, Variant(letter, HeavyVariant));
                if (heavy is not null)
                    return heavy;
            }

            return Lookup(profile, letter) ?? string.Empty;
        }

        /// <summary>
        /// Сколько раз повторить графему самого согласного. Больше одного — только
        /// у безгласного глайда с маддом лин: там долгота лежит на و и ي, а не на
        /// гласной при них, и повторять приходится их же графему.
        /// </summary>
        private static int GlideCount(Segment segment) =>
            segment.Vowel == Harakah.Sukun ? GraphemeCount(segment.VowelLength) : 1;

        private string RenderVowel(Segment segment, TransliterationProfile profile)
        {
            if (segment.Vowel is Harakah.None or Harakah.Sukun)
                return string.Empty;

            var key = VowelKey(segment.Vowel);
            var grapheme = segment.VowelVariant switch
            {
                VowelVariant.Heavy => Lookup(profile, Variant(key, HeavyVariant)),
                VowelVariant.Soft => Lookup(profile, Variant(key, SoftVariant)),
                _ => null
            } ?? Lookup(profile, key);

            if (string.IsNullOrEmpty(grapheme))
                return string.Empty;

            return Repeat(grapheme, GraphemeCount(segment.VowelLength));
        }

        private static string Repeat(string grapheme, int count) =>
            count == 1 ? grapheme : string.Concat(Enumerable.Repeat(grapheme, count));

        private static string VowelKey(Harakah vowel) => vowel switch
        {
            Harakah.Fatha => ArabicScript.Fatha.ToString(),
            Harakah.Damma => ArabicScript.Damma.ToString(),
            Harakah.Kasra => ArabicScript.Kasra.ToString(),
            _ => string.Empty
        };

        private static string Variant(string key, string variant) => $"{key}|{variant}";

        private static string? Lookup(TransliterationProfile profile, string key)
        {
            if (string.IsNullOrEmpty(key))
                return null;

            return profile.Rules.TryGetValue(key, out var value) ? value : null;
        }
    }
}
