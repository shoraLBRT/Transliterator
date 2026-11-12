using System.Globalization;

namespace Transliterator.Domain.Localization
{
    /// <summary>
    /// Форма слова при числе: «1 правило», «2 правила», «5 правил». У каждой строки
    /// с числом три ключа — <c>.One</c>, <c>.Few</c>, <c>.Many</c>, — и язык, которому
    /// столько форм не нужно, повторяет текст (в английском Few = Many).
    /// </summary>
    public static class Plural
    {
        public static string Key(string prefix, int count, CultureInfo culture) =>
            $"{prefix}.{Form(count, culture)}";

        private static string Form(int count, CultureInfo culture)
        {
            var n = Math.Abs(count);

            if (culture.TwoLetterISOLanguageName == "ru")
            {
                return (n % 100) switch
                {
                    >= 11 and <= 14 => "Many",
                    _ => (n % 10) switch
                    {
                        1 => "One",
                        >= 2 and <= 4 => "Few",
                        _ => "Many",
                    },
                };
            }

            return n == 1 ? "One" : "Many";
        }
    }
}
