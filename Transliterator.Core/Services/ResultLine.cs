using System.Text.RegularExpressions;

namespace Transliterator.Core.Services
{
    /// <summary>
    /// Строка вывода веб-версии (D17): вывод суры — по аяту на строке, и номер
    /// аята закрывает свою строку (<see cref="CorpusText"/>). Страница рисует
    /// строки отдельными блоками, а номер — круглым знаком, как в мусхафе. Знак
    /// держит саму цифру, а пробел перед ним остаётся в тексте строки: склеенные
    /// обратно строки — ровно прежний вывод, и копируется он же.
    /// </summary>
    /// <param name="Text">Текст строки; перед номером — с пробелом на конце.</param>
    /// <param name="Ayah">Номер аята в конце строки или <c>null</c>, если его нет.</param>
    public sealed record ResultLine(string Text, string? Ayah)
    {
        /// <summary>
        /// Номер — число в конце строки после пробела. Одно число без текста
        /// перед ним номером не считается: это мог быть просто вписанный текст.
        /// </summary>
        private static readonly Regex Numbered = new(@"^(.*\S\s)([0-9]{1,3})$", RegexOptions.CultureInvariant);

        /// <summary>Вывод по строкам: перевод строки — граница, номер в конце отделён.</summary>
        public static IReadOnlyList<ResultLine> Split(string result) =>
            result.Split('\n')
                  .Select(line => Numbered.Match(line) is { Success: true } match
                      ? new ResultLine(match.Groups[1].Value, match.Groups[2].Value)
                      : new ResultLine(line, null))
                  .ToArray();

        /// <summary>Строки обратно в текст — тот же, что был разбит.</summary>
        public static string Join(IEnumerable<ResultLine> lines) =>
            string.Join('\n', lines.Select(line => line.Text + line.Ayah));
    }
}
