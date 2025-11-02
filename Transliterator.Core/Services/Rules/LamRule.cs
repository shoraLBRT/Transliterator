using System.Text;
using Transliterator.Domain.Entities;

namespace Transliterator.Core.Services.Rules
{
    public class LamRule
    {
        private string[] VowelLetters = { "ا", "و", "إ" }; // а, у, и

        public string ApplyLamRule(string text, TransliterationProfile profile)
        {
            var result = ApplySoftLRule(text, profile);
            result = ApplyYaAfterLRule(result);
            return result;
        }

        private string ApplySoftLRule(string text, TransliterationProfile profile)
        {
            var vowelLettersInProfile = GetVowelLetterInProfile(profile);

            // Исключение для имени Аллаха
            if (text.Contains("Аллах"))
            {
                text = text.Replace("Аллах", "___ALLAH___");
            }

            StringBuilder result = new StringBuilder();
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == 'л' || text[i] == 'Л')
                {
                    bool isNextVowelOrL = false;
                    // Проверяем, есть ли после "л" гласная или другая "л"
                    if (i + 1 < text.Length)
                    {
                        string nextChar = text[i + 1].ToString();
                        isNextVowelOrL = IsVowel(nextChar, vowelLettersInProfile) || nextChar == "л" || nextChar == "Л";
                    }

                    // Заменяем "л" на "ль" только если после неё НЕ гласная и НЕ "л"
                    if (!isNextVowelOrL)
                    {
                        result.Append(text[i] == 'л' ? "ль" : "Ль");
                    }
                    else
                    {
                        result.Append(text[i]);
                    }
                }
                else
                {
                    result.Append(text[i]);
                }
            }

            // Возвращаем имя Аллаха обратно
            return result.ToString().Replace("___ALLAH___", "Аллах");
        }

        private string ApplyYaAfterLRule(string text)
        {
            // Исключение для имени Аллаха
            if (text.Contains("Аллах"))
            {
                text = text.Replace("Аллах", "___ALLAH___");
            }

            StringBuilder result = new StringBuilder();
            for (int i = 0; i < text.Length; i++)
            {
                if ((text[i] == 'л' || text[i] == 'Л') && i + 1 < text.Length)
                {
                    // Проверяем, что после "л" идёт "а"
                    if (text[i + 1] == 'а' || text[i + 1] == 'А')
                    {
                        result.Append(text[i]); // Добавляем "л"
                        result.Append(text[i + 1] == 'а' ? 'я' : 'Я'); // Заменяем "а" на "я"
                        i++; // Пропускаем следующий символ
                        continue;
                    }
                }
                result.Append(text[i]);
            }

            // Возвращаем имя Аллаха обратно
            return result.ToString().Replace("___ALLAH___", "Аллах");
        }
        private bool IsVowel(string character, List<string> vowelLettersInProfile)
        {
            return vowelLettersInProfile.Contains(character);
        }

        private List<string> GetVowelLetterInProfile(TransliterationProfile profile)
        {
            return VowelLetters
                .Where(arabicLetter => profile.Rules.ContainsKey(arabicLetter))
                .Select(arabicLetter => profile.Rules[arabicLetter])
                .ToList();
        }
    }
}
