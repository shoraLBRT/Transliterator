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

        /// <summary>
        /// Хвост строки с номером — последнее слово и пробел перед знаком: страница
        /// держит их вместе со знаком, чтобы знак не переносился на строку один.
        /// <c>Lead + Word + Gap + Ayah</c> — та же строка.
        /// </summary>
        public string Lead => Ayah is null ? Text : Text[..WordStart];

        /// <summary>
        /// Последнее слово перед номером, а у слова через дефис — его последняя
        /// часть (пусто, если номера нет). Дефис остаётся в <see cref="Lead"/>:
        /// длинное слово переносится по нему, а хвост со знаком — целиком.
        /// </summary>
        public string Word => Ayah is null ? string.Empty : Text[WordStart..^1];

        /// <summary>Пробел между словом и номером — тот, что был в выводе.</summary>
        public string Gap => Ayah is null ? string.Empty : Text[^1..];

        private int WordStart
        {
            get
            {
                var end = Text.Length - 1;
                var start = end;
                while (start > 0 && !char.IsWhiteSpace(Text[start - 1]) && Text[start - 1] != '-')
                    start--;
                return start;
            }
        }

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
