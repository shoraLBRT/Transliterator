using System.Text.RegularExpressions;
using Transliterator.Domain.Entities;

namespace Transliterator.Core.Services.Rules
{
    public class WaslaRules
    {
        private const string _wasla = "ٱ";
        private const string _lam = "ل";
        private const string _i = "إ";
        private const string _u = "ُ";

        private string _allowedCharacters;

        // Алиф с васлей транслитерируется как "á"
        // Если "á" стоит в начале слова после пробела/начала строки,
        // он сливается с окончанием предыдущего слова

        // Пример: "бисми áл-лаhи" → "бисмил-лаhи"
        // Паттерн: слово + пробел + á
        public string ProcessWaslaAlif(string cyrillicText, TransliterationProfile profile)
        {
            _allowedCharacters = GetAllowedCharactersFromProfile(profile);

            var profileWasla = profile.Rules[_wasla];
            var profileLam = profile.Rules[_lam];

            var result = ProcessWordMerges(cyrillicText, profile);

            result = ProcessFirstWaslaAlif(result, profile);
            return result;
        }
        private string ProcessWordMerges(string cyrillicText, TransliterationProfile profile)
        {
            var profileWasla = profile.Rules[_wasla];
            var profileLam = profile.Rules[_lam];

            string pattern = $@"([{_allowedCharacters}]+)\s+{profileWasla}(?:{profileLam})?([{_allowedCharacters}]*)";

            string result = cyrillicText;

            bool hasMatch;
            do
            {
                hasMatch = false;
                result = Regex.Replace(result, pattern, match =>
                {
                    hasMatch = true; // нашли совпадение
                    string previousWord = match.Groups[1].Value;
                    string restOfWord = match.Groups[2].Value;
                    return $"{previousWord}-{restOfWord}";
                });
            } while (hasMatch); // повторяем, пока есть новые совпадения

            return result;
        }

        public string ProcessFirstWaslaAlif(string cyrillicText, TransliterationProfile profile)
        {

            var profileWasla = profile.Rules[_wasla];
            var profileLam = profile.Rules[_lam];
            var profileU = profile.Rules[_u];
            var profileI = profile.Rules[_i];

            string pattern = @$"\b{profileWasla}([{_allowedCharacters}]+)";

            return Regex.Replace(cyrillicText, pattern, match =>
            {
                string restOfWord = match.Groups[1].Value;

                if (restOfWord.StartsWith(profileLam))
                {
                    return profileWasla + restOfWord;
                }
                if (restOfWord.Length >= 3)
                {
                    string third = restOfWord[2].ToString();
                    if (third == profileU)
                    {
                        return "у" + restOfWord;
                    }
                }
                return "и" + restOfWord;
            });
        }

        private string GetAllowedCharactersFromProfile(TransliterationProfile profile)
        {
            // Все ключи цифр арабского языка
            var arabicDigits = new HashSet<string> { "٠", "١", "٢", "٣", "٤", "٥", "٦", "٧", "٨", "٩" };

            // Отбираем только ключи, которые не цифры
            var nonDigitKeys = profile.Rules.Keys.Where(k => !arabicDigits.Contains(k));

            // Берём значения этих ключей
            var allowedCharsSet = new HashSet<char>();
            foreach (var key in nonDigitKeys)
            {
                var val = profile.Rules[key];
                if (string.IsNullOrEmpty(val))
                    continue;

                foreach (var ch in val)
                {
                    allowedCharsSet.Add(ch);
                }
            }

            // Собираем строку и экранируем для regex
            return Regex.Escape(new string(allowedCharsSet.ToArray()));
        }
    }
}
