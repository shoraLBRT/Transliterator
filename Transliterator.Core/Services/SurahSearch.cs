using System.Globalization;
using System.Text;
using Transliterator.Domain.Entities;

namespace Transliterator.Core.Services
{
    /// <summary>
    /// Поиск в списке сур веб-версии (D15): по номеру, по имени на любом языке
    /// страницы — не только на текущем: «ikhlas» находит суру и в русском
    /// интерфейсе — и по арабскому имени. Регистр, дефисы, пробелы и диакритика
    /// не считаются: «фат», «Al Fatiha» и «الفاتحة» без харакатов находят
    /// «Аль-Фатиху» и «سُورَةُ ٱلْفَاتِحَةِ».
    /// </summary>
    public static class SurahSearch
    {
        /// <summary>
        /// Суры, подходящие к запросу, в порядке списка. Пустой запрос — все.
        /// Запрос из цифр ищет по началу номера («11» — 11, 110…114), и сура
        /// с ровно этим номером встаёт первой: Enter на «1» выбирает «Аль-Фатиху».
        /// </summary>
        public static IReadOnlyList<CorpusSurah> Filter(IEnumerable<CorpusSurah> surahs, string? query)
        {
            var key = Fold(query);
            if (key.Length == 0)
                return surahs.ToList();

            if (key.All(char.IsAsciiDigit))
            {
                return surahs.Where(surah => surah.Number.ToString(CultureInfo.InvariantCulture).StartsWith(key, StringComparison.Ordinal))
                             .OrderBy(surah => surah.Number.ToString(CultureInfo.InvariantCulture) == key ? 0 : 1)
                             .ToList();
            }

            return surahs.Where(surah => surah.Name.Values.Append(surah.ArabicName).Any(name => Fold(name).Contains(key, StringComparison.Ordinal)))
                         .ToList();
        }

        /// <summary>
        /// Текст для сравнения: без регистра и знаков (NFD снимает ударения,
        /// «й» и «ё» сводит к «и» и «е», а у арабского — харакаты и хамзу над
        /// алифом), васля и безточечная «я» — как обычные алиф и «йа», та-марбута —
        /// как «ха». Остаются только буквы и цифры: дефис и пробел не мешают.
        /// </summary>
        public static string Fold(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var folded = new StringBuilder(text.Length);
            foreach (var c in text.Normalize(NormalizationForm.FormD))
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(c);
                if (category is UnicodeCategory.NonSpacingMark or UnicodeCategory.EnclosingMark)
                    continue;

                var letter = c switch
                {
                    'ٱ' => 'ا',
                    'ى' => 'ي',
                    'ة' => 'ه',
                    _ => char.ToLowerInvariant(c),
                };

                if (char.IsLetterOrDigit(letter) && letter != 'ـ')
                    folded.Append(letter);
            }

            return folded.ToString();
        }
    }
}
