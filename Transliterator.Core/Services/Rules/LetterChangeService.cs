using System.Text;
using Transliterator.Domain.Entities;

namespace Transliterator.Core.Services.Rules
{
    public class LetterChangeService
    {
        public string TransliterateLetters(string arabicText, TransliterationProfile profile)
        {
            string normalizedText = arabicText.Normalize();

            StringBuilder result = new StringBuilder();

            var textElements = System.Globalization.StringInfo.GetTextElementEnumerator(normalizedText);

            while (textElements.MoveNext())
            {
                string grapheme = textElements.GetTextElement();
                string transliteratedGrapheme = ProcessGrapheme(grapheme, profile.Rules);
                result.Append(transliteratedGrapheme);
            }

            return result.ToString();
        }

        private string ProcessGrapheme(string grapheme, Dictionary<string, string> rules)
        {
            if (string.IsNullOrEmpty(grapheme))
                return string.Empty;

            char baseChar = grapheme[0];

            if (HasLittleAlif(grapheme))
                baseChar = grapheme[1];

            bool hasShadda = HasShadda(grapheme);
            string vowel = GetVowel(grapheme);

            if (!rules.TryGetValue(baseChar.ToString(), out string baseTranslit))
            {
                baseTranslit = baseChar.ToString();
            }
            StringBuilder result = new StringBuilder();

            if (hasShadda)
            {
                result.Append(baseTranslit);
            }

            result.Append(baseTranslit);

            result.Append(vowel);

            return result.ToString();
        }

        private string GetVowel(string grapheme)
        {
            if (grapheme.Contains('\u064E')) return "а"; // Фатха
            if (grapheme.Contains('\u064F')) return "у"; // Дамма
            if (grapheme.Contains('\u0650')) return "и"; // Касра
            if (grapheme.Contains('\u0652')) return "";  // Сукун
            return ""; // Нет огласовки
        }

        private bool HasLittleAlif(string grapheme)
        {
            if (grapheme.Contains("ـٰ"))
                return true;
            else return false;
        }

        // Shadda - doubling a consonant
        private bool HasShadda(string grapheme)
        {
            if (grapheme.Contains('\u0651'))
                return true;
            else return false;
        }
    }
}