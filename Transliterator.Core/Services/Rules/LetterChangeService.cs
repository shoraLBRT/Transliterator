using System.Text;
using Transliterator.Domain.Entities;

namespace Transliterator.Core.Services.Rules
{
    public class LetterChangeService
    {
        private const string _fatha = "َ";
        private const string _damma = "ُ";
        private const string _kasra = "ِ";
        private const string _sukun = "ْ";

        private const char _shadda = '\u0651';
        private const char _maddaAlif = 'ٰ';
        private const string _maddaStandalone = "ـٰ";
        private const string _maddaToken = "_MADDA_";

        public string TransliterateLetters(string arabicText, TransliterationProfile profile)
        {
            string normalizedText = arabicText.Normalize();

            StringBuilder result = new StringBuilder();

            var textElements = System.Globalization.StringInfo.GetTextElementEnumerator(normalizedText);

            while (textElements.MoveNext())
            {
                string grapheme = textElements.GetTextElement();
                string transliteratedGrapheme = ProcessGrapheme(grapheme, profile);
                result.Append(transliteratedGrapheme);
            }

            // Post-process _MADDA_ tokens to elongate previous vowel
            return ProcessMaddaTokens(result.ToString(), profile);
        }

        private string ProcessGrapheme(string grapheme, TransliterationProfile profile)
        {
            if (string.IsNullOrEmpty(grapheme))
                return string.Empty;
            if (string.IsNullOrWhiteSpace(grapheme))
                return " ";

            char baseChar = grapheme[0];
            if (!profile.Rules.TryGetValue(baseChar.ToString(), out string baseTranslit))
                baseTranslit = "";

            // Check if grapheme is a standalone madda (big or small)
            if (IsStandaloneMadda(grapheme))
                return _maddaToken;

            bool hasMaddahAlif = grapheme.Contains(_maddaAlif);
            bool hasShadda = grapheme.Contains(_shadda);
            string vowel = GetVowel(grapheme, profile);

            if (UaException(baseChar, vowel))
                baseTranslit = profile.Rules[_kasra];

            StringBuilder result = new StringBuilder();
            result.Append(baseTranslit);
            if (hasShadda)
                result.Append(baseTranslit);

            result.Append(vowel);            
            return result.ToString();
        }

        private string ProcessMaddaTokens(string text, TransliterationProfile profile)
        {
            foreach (var vowel in new[] { _fatha, _damma, _kasra })
            {
                var translit = profile.Rules.ContainsKey(vowel) ? profile.Rules[vowel] : "";
                if (!string.IsNullOrEmpty(translit))
                {
                    string doubleVowel = translit + translit;
                    text = text.Replace(translit + _maddaToken, doubleVowel);
                }
            }

            return text.Replace(_maddaToken, "");
        }

        private bool IsStandaloneMadda(string grapheme)
        {
            return grapheme == _maddaStandalone;
        }

        private string GetVowel(string grapheme, TransliterationProfile profile)
        {
            if (AlifException(grapheme))
                return string.Empty;
            string result = "";
            if (grapheme.Contains(_fatha))
            {
                result = profile.Rules[_fatha];
                if (grapheme.Contains(_maddaAlif))
                    result += profile.Rules[_fatha];
            }
            if (grapheme.Contains(_damma)) result = profile.Rules[_damma];
            if (grapheme.Contains(_kasra)) result = profile.Rules[_kasra];
            if (grapheme.Contains(_sukun)) result = profile.Rules[_sukun];

            return result;
        }

        private bool AlifException(string grapheme)
        {
            return grapheme.Contains('إ') || grapheme.Contains('أ');
        }

        private bool UaException(char baseChar, string vowel)
        {
            return baseChar == 'ي' && string.IsNullOrEmpty(vowel);
        }
    }
}
