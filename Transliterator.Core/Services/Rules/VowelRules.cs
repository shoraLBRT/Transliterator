using System.Text;

namespace Transliterator.Core.Services.Rules
{
    public class VowelRules
    {

        public string ApplyVowelElongationRule(string text)
        {
            StringBuilder result = new StringBuilder();

            for (int i = 0; i < text.Length; i++)
            {
                char current = text[i];

                // Если текущий символ - гласная и предыдущий такой же гласный
                if (i > 0 && IsVowel(current) && current == text[i - 1])
                {
                    // Уже добавлена одна гласная, просто продолжаем (удвоение уже есть)
                    result.Append(current);
                }
                else
                {
                    result.Append(current);
                }
            }

            return result.ToString();
        }

        private bool IsVowel(char c)
        {
            char[] vowels = { 'а', 'у', 'и', 'о', 'ы', 'э', 'я', 'ю', 'ё', 'е' };
            return Array.Exists(vowels, v => v == char.ToLower(c));
        }
    }
}
