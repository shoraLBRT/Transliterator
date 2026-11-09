using System.Globalization;
using Transliterator.Core.Services.Phonology;

namespace Transliterator.Core.Services
{
    /// <summary>
    /// Спорная графема (E5): по выводу больше не отличить одну букву от другой
    /// или от сочетания двух.
    /// </summary>
    /// <param name="Letter">Буква, графема которой спорит.</param>
    /// <param name="Grapheme">Её графема.</param>
    /// <param name="Parts">
    /// С чем спорит: одна буква — пишется так же; две — графема складывается
    /// из их графем по порядку («къ» у <c>ق</c> = «к» у <c>ك</c> + «ъ» у <c>ء</c>).
    /// </param>
    public sealed record GraphemeConflict(string Letter, string Grapheme, IReadOnlyList<string> Parts)
    {
        public bool IsComposed => Parts.Count == 2;

        /// <summary>Касается ли спор этой буквы — как спорящей или как части.</summary>
        public bool Involves(string letter) => Letter == letter || Parts.Contains(letter);

        /// <summary>Все буквы спора: спорящая и то, с чем она спорит.</summary>
        public IEnumerable<string> Letters => Parts.Prepend(Letter).Distinct();
    }

    /// <summary>
    /// Поиск спорных графем профиля (E5). Запрещать их не нужно — решает
    /// пользователь, — но видеть, к чему это приведёт, он должен. Так было
    /// с <c>ذ</c> «зъ» при хамзе «ъ» в <c>Standard</c>: «з + хамза» неотличимо
    /// от <c>ذ</c>, и с этого начался G2.
    /// <para>
    /// Сравниваются только графемы согласных букв. Варианты (<c>"ر|heavy"</c>)
    /// пишут ту же букву в другом положении, огласовки и цифры буквой не спутать.
    /// Пустая графема ни с чем не спорит: буква, которая не пишется, не пишется
    /// осознанно.
    /// </para>
    /// </summary>
    public static class GraphemeConflicts
    {
        /// <summary>
        /// Пары, которые пишутся одинаково по праву: та-марбута в соединении — это
        /// /t/, тот же звук, что у <c>ت</c>. На паузе её отличает свой вариант.
        /// </summary>
        private static readonly HashSet<(string, string)> SameSound = new()
        {
            (ArabicScript.TaMarbutaStr, "ت"),
        };

        public static IReadOnlyList<GraphemeConflict> Find(IReadOnlyDictionary<string, string> rules)
        {
            var letters = rules.Where(r => IsLetter(r.Key) && !string.IsNullOrEmpty(r.Value))
                               .OrderBy(r => r.Key, StringComparer.Ordinal)
                               .ToList();
            var conflicts = new List<GraphemeConflict>();

            for (int i = 0; i < letters.Count; i++)
            {
                var (letter, grapheme) = letters[i];

                // Пишется так же, как другая буква. Пара — один спор, а не два.
                for (int j = i + 1; j < letters.Count; j++)
                    if (letters[j].Value == grapheme && !IsSameSound(letter, letters[j].Key))
                        conflicts.Add(new GraphemeConflict(letter, grapheme, new[] { letters[j].Key }));

                // Складывается из двух других графем.
                foreach (var (first, firstGrapheme) in letters)
                {
                    if (first == letter || !grapheme.StartsWith(firstGrapheme, StringComparison.Ordinal)
                                        || firstGrapheme.Length == grapheme.Length)
                        continue;

                    var rest = grapheme[firstGrapheme.Length..];

                    foreach (var (second, secondGrapheme) in letters)
                        if (second != letter && secondGrapheme == rest)
                            conflicts.Add(new GraphemeConflict(letter, grapheme, new[] { first, second }));
                }
            }

            return conflicts;
        }

        /// <summary>
        /// Пример к спору: самое короткое слово текста, в котором есть буква.
        /// Хамза ищется и на носителях — в тексте она редко стоит отдельно.
        /// </summary>
        public static string? ExampleWord(IEnumerable<string> texts, string letter)
        {
            var forms = letter == ArabicScript.HamzaStr
                ? ArabicScript.HamzaCarriers.Append(ArabicScript.HamzaAbove).ToArray()
                : letter.ToCharArray();

            return texts.SelectMany(text => text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
                        .Where(word => word.IndexOfAny(forms) >= 0)
                        .OrderBy(LetterCount)
                        .ThenBy(word => word, StringComparer.Ordinal)
                        .FirstOrDefault();
        }

        private static bool IsLetter(string key) =>
            key.Length == 1 && CharUnicodeInfo.GetUnicodeCategory(key[0]) == UnicodeCategory.OtherLetter;

        private static bool IsSameSound(string a, string b) =>
            SameSound.Contains((a, b)) || SameSound.Contains((b, a));

        private static int LetterCount(string word) =>
            word.Count(c => CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.OtherLetter);
    }
}
