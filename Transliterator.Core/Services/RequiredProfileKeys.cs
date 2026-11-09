using System.Globalization;
using Transliterator.Core.Services.Phonology;

namespace Transliterator.Core.Services
{
    /// <summary>
    /// Обязательный набор ключей профиля (F6): без них вывод непригоден,
    /// и профиль без них не принимается — ни встроенный, ни свой.
    /// <para>
    /// Обязательно то, чего рендерер не может добрать откатом и без чего звук
    /// пропадёт или прочитается неверно. Всё прочее профиль вправе опустить —
    /// и у каждого такого ключа записано, что тогда будет. Набор утверждает
    /// владелец; пока он не утвердил, это предложение (см. <c>docs/ROADMAP.md</c>).
    /// </para>
    /// </summary>
    public static class RequiredProfileKeys
    {
        /// <summary>
        /// Буквы, которые доходят до рендерера. Носители хамзы, алиф-васла
        /// и малые буквы сюда не входят: их разбирают стадии 1 и 2, и до профиля
        /// они не доходят. Алиф входит: долгая ā пишется его графемой (в
        /// <c>Standard</c> — пустой, долготу там даёт повтор огласовки).
        /// </summary>
        public static IReadOnlyList<string> Letters { get; } = new[]
        {
            "ء", "ا", "ب", "ت", "ة", "ث", "ج", "ح", "خ", "د", "ذ", "ر", "ز", "س", "ش",
            "ص", "ض", "ط", "ظ", "ع", "غ", "ف", "ق", "ك", "ل", "م", "ن", "ه", "و", "ي",
        };

        /// <summary>Огласовки: фатха, дамма, касра. Танвин разбор разворачивает в них же и нун.</summary>
        public static IReadOnlyList<string> Vowels { get; } = new[] { "َ", "ُ", "ِ" };

        /// <summary>
        /// Обязательные варианты. Откат на базовый ключ у них меняет не запись,
        /// а чтение: та-марбута на паузе звучит /h/, а без своего варианта
        /// писалась бы базовой /t/ — «рохIмат» вместо «рохIмаh».
        /// </summary>
        public static IReadOnlyList<string> Variants { get; } = new[]
        {
            ArabicScript.TaMarbutaStr + "|" + CyrillicRenderer.WaqfVariant,
        };

        public static IReadOnlyList<string> All { get; } = Letters.Concat(Vowels).Concat(Variants).ToList();

        /// <summary>
        /// Варианты, которые профиль вправе не задавать, и что тогда будет.
        /// Вариант, которого нет ни здесь, ни среди обязательных, — новый, и решение
        /// о нём ещё не принято: тест профилей это ловит.
        /// </summary>
        public static IReadOnlyDictionary<string, string> OptionalVariants { get; } = new Dictionary<string, string>
        {
            [CyrillicRenderer.InitialVariant] =
                "Хамза в начале слова пишется базовой графемой хамзы — приступ голоса виден на письме.",
            [CyrillicRenderer.HiatusVariant] =
                "Хамза между гласными пишется базовой графемой, а не разделителем.",
            [CyrillicRenderer.QalqalahVariant] =
                "Отзвук кальканя не пишется: всего, что звучит, на письме не обозначить (G6).",
            [CyrillicRenderer.QalqalahStrongVariant] =
                "Усиленный отзвук пишется как обычный.",
            [CyrillicRenderer.GhunnaVariant] =
                "Гунна пишется обычными «н» и «м» (G1).",
            [CyrillicRenderer.SukunVariant] =
                "Буква в закрытом слоге пишется базовой графемой: латинице «ль» не нужно.",
            [CyrillicRenderer.HeavyVariant] =
                "Гласная после твёрдой буквы не окрашивается на письме.",
            [CyrillicRenderer.SoftVariant] =
                "Гласная после мягкого ляма пишется как обычная: латинице «я» и «ю» не нужны.",
        };

        /// <summary>
        /// Цифры необязательны: незаданная пишется как есть, арабской цифрой,
        /// и номер аята от этого не теряется.
        /// </summary>
        public const string OptionalDigits =
            "Незаданная цифра пишется как есть — арабской цифрой.";

        /// <summary>Обязательные ключи, которых в правилах нет, в порядке набора.</summary>
        public static IReadOnlyList<string> Missing(IReadOnlyDictionary<string, string>? rules) =>
            All.Where(key => rules is null || !rules.ContainsKey(key)).ToList();

        /// <summary>
        /// Ключ, решение о котором принято: обязательный, цифра или вариант
        /// из необязательных. Прочие ключи профиля — новые варианты без решения.
        /// </summary>
        public static bool IsDecided(string key)
        {
            if (All.Contains(key))
                return true;

            if (key.Length == 1 && CharUnicodeInfo.GetUnicodeCategory(key[0]) == UnicodeCategory.DecimalDigitNumber)
                return true;

            var separator = key.IndexOf('|');
            return separator > 0 && OptionalVariants.ContainsKey(key[(separator + 1)..]);
        }

        /// <summary>Ключ для человека: огласовка над пунктирным кружком, иначе её не видно.</summary>
        public static string Describe(string key) =>
            key.Length > 0 && CharUnicodeInfo.GetUnicodeCategory(key[0]) == UnicodeCategory.NonSpacingMark
                ? "◌" + key
                : key;
    }
}
