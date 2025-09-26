using System.Text;
using Transliterator.Domain.Entities;

namespace Transliterator.Core.Services
{
    public class LetterChangeService
    {
        public string TransliterateLetters(string arabicText, TransliterationProfile profile)
        {
            // Нормализуем текст для приведения к一致的 форме
            string normalizedText = arabicText.Normalize();

            StringBuilder result = new StringBuilder();

            // Используем StringInfo для корректного обхода графемных кластеров
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

            // 1. Извлекаем базовую букву (первый символ)
            char baseChar = grapheme[0];

            // 2. Проверяем наличие диакритических знаков
            bool hasShadda = grapheme.Contains('\u0651'); // Шадда
            string vowel = GetVowel(grapheme); // Определяем огласовку

            // 3. Ищем замену для базовой буквы в правилах
            if (!rules.TryGetValue(baseChar.ToString(), out string baseTranslit))
            {
                baseTranslit = baseChar.ToString(); // Если не найдено, оставляем как есть
            }

            // 4. Собираем результат с учетом диакритиков
            StringBuilder result = new StringBuilder();

            if (hasShadda)
            {
                // Удваиваем согласную, если есть шадда
                result.Append(baseTranslit);
            }

            result.Append(baseTranslit); // Основная буква
            result.Append(vowel); // Огласовка

            return result.ToString();
        }

        private string GetVowel(string grapheme)
        {
            // Определяем тип огласовки
            if (grapheme.Contains('\u064E')) return "a"; // Фатха
            if (grapheme.Contains('\u064F')) return "u"; // Дамма  
            if (grapheme.Contains('\u0650')) return "i"; // Касра
            return ""; // Нет огласовки или сукун
        }
    }
}