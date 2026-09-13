using System.Globalization;
using Transliterator.Domain.Entities;

namespace Transliterator.Core.Services
{
    /// <summary>
    /// Сура корпуса как текст для поля ввода: так её подставляет панель готовых сур
    /// веб-версии (D3), и так же её гоняют тесты.
    /// </summary>
    public static class CorpusText
    {
        /// <summary>
        /// Каждый аят на своей строке, номер — в конце аята арабскими цифрами,
        /// как он напечатан в мусхафе. Перевод строки для правил — та же граница
        /// слов, что и пробел, а вывод сохраняет его: аяты не сливаются в полотно
        /// ни во вводе, ни в результате.
        /// </summary>
        public static string Arabic(CorpusSurah surah) =>
            string.Join('\n', surah.Ayahs.Select(ayah => $"{ayah.Arabic} {ArabicDigits(ayah.Number)}"));

        /// <summary>Число арабскими цифрами: <c>112</c> → <c>١١٢</c>.</summary>
        public static string ArabicDigits(int number) =>
            new(number.ToString(CultureInfo.InvariantCulture).Select(c => (char)('٠' + (c - '0'))).ToArray());
    }
}
