using System.Text.RegularExpressions;
using Transliterator.Domain.Entities;

namespace Transliterator.Core.Services.Rules
{
    public class AlifMaqsuraRule
    {
        /// <summary>
        /// Обрабатывает алиф максуру (ى) и заменяет её на длинное "ā" (в русской кириллице — "аа").
        /// </summary>
        public string ApplyAlifMaqsuraRule(string text, TransliterationProfile profile)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // Проверяем, есть ли соответствие для обычной алиф
            string replacement = "аа";
            if (profile.Rules.TryGetValue("ا", out string? translitForAlif))
            {
                // если в профиле алиф задан, удлиняем его
                replacement = translitForAlif + translitForAlif;
            }

            // Заменяем алиф максуру (ى)
            // Важно: она может идти в конце слова, перед знаками и т.п.
            string pattern = @"ى";
            return Regex.Replace(text, pattern, replacement);
        }
    }
}
