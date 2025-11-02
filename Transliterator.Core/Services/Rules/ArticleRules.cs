using System.Text.RegularExpressions;
using Transliterator.Domain.Entities;

namespace Transliterator.Core.Services.Rules
{
    public class ArticleRules
    {

        private string[] arabicSunLetters = {
                "ت", "ث", "د", "ذ", "ر", "ز", "س", "ش", "ص", "ض", "ط", "ظ",
                "ل", "ن"
            };


        public string ApplySunMoonLettersRule(string text, TransliterationProfile profile)
        {
            var sunLettersInProfile = GetSunLettersByProfile(profile);

            string pattern = @"áл(\w+)";

            return Regex.Replace(text, pattern, match =>
            {
                string wordAfterArticle = match.Groups[1].Value;
                if (string.IsNullOrEmpty(wordAfterArticle))
                    return match.Value;

                string firstLetter = GetFirstGrapheme(wordAfterArticle, sunLettersInProfile);

                if (IsSunLetter(firstLetter, sunLettersInProfile))
                {
                    string restOfWord = wordAfterArticle.Substring(firstLetter.Length);
                    return $"а{firstLetter}-{restOfWord}";
                }
                else
                {
                    return $"аль-{wordAfterArticle}";
                }
            }, RegexOptions.IgnoreCase);
        }

        private string GetFirstGrapheme(string word, List<string> possibleGraphemes)
        {
            // Сначала проверяем составные буквы (длинные последовательности)
            foreach (string grapheme in possibleGraphemes.OrderByDescending(g => g.Length))
            {
                if (word.StartsWith(grapheme, StringComparison.OrdinalIgnoreCase))
                {
                    return grapheme;    
                }
            }

            return word[0].ToString();
        }
        private bool IsSunLetter(string grapheme, List<string> sunLetters)
        {
            return sunLetters.Contains(grapheme, StringComparer.OrdinalIgnoreCase);
        }
        private List<string> GetSunLettersByProfile(TransliterationProfile profile)
        {
            return arabicSunLetters
                .Where(arabicLetter => profile.Rules.ContainsKey(arabicLetter))
                .Select(arabicLetter => profile.Rules[arabicLetter])
                .ToList();
        }
    }
}
