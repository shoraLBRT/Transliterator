using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Transliterator.Domain.Entities;

namespace Transliterator.Core.Services.Rules
{
    public class PostEmphaticVowelReplacer
    {
        private string[] EmphaticLetters = { "ر", "غ", "ق", "ض", "ص", "ظ", "ط" };
        private string[] VowelLetters = { "ا", "ي" };

        public string ApplyPostEmphaticVowelRule(string text, TransliterationProfile profile)
        {
            var emphaticLettersInProfile = GetEmphaticLetterInProfile(profile);
            var vowelLettersInProfile = GetVowelLetterInProfile(profile);

            var result = new StringBuilder();
            int i = 0;
            while (i < text.Length)
            {
                bool isEmphatic = false;
                // Проверяем, начинается ли текущая позиция с эмфатической буквы
                foreach (var emphaticLetter in emphaticLettersInProfile.OrderByDescending(s => s.Length))
                {
                    if (text.Substring(i).StartsWith(emphaticLetter, StringComparison.OrdinalIgnoreCase))
                    {
                        isEmphatic = true;
                        result.Append(emphaticLetter);
                        i += emphaticLetter.Length;
                        break;
                    }
                }

                if (isEmphatic && i < text.Length)
                {
                    // Проверяем, является ли следующий символ гласной из профиля
                    foreach (var vowel in vowelLettersInProfile)
                    {
                        if (text.Substring(i).StartsWith(vowel, StringComparison.OrdinalIgnoreCase))
                        {
                            // Заменяем гласную
                            string replacement = vowel == "а" ? "о" : "ы";
                            result.Append(replacement);
                            i += vowel.Length;
                            // Проверяем удлинённую гласную
                            while (i < text.Length && text.Substring(i).StartsWith(vowel, StringComparison.OrdinalIgnoreCase))
                            {
                                result.Append(replacement);
                                i += vowel.Length;
                            }
                            break;
                        }
                    }
                    continue;
                }
                // Если не эмфатическая буква или не гласная, просто копируем символ
                result.Append(text[i]);
                i++;
            }
            return result.ToString();
        }

        private List<string> GetEmphaticLetterInProfile(TransliterationProfile profile)
        {
            return EmphaticLetters
                .Where(arabicLetter => profile.Rules.ContainsKey(arabicLetter))
                .Select(arabicLetter => profile.Rules[arabicLetter])
                .ToList();
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
