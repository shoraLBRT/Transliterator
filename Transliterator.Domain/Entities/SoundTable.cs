using Transliterator.Domain.Localization;

namespace Transliterator.Domain.Entities
{
    /// <summary>
    /// Таблица звуков (D11): как читать каждую арабскую букву. Данные, как каталог
    /// правил (I1): тексты о произношении вычитывает владелец, и править их удобнее
    /// в JSON. Запись буквы здесь не хранится — её страница берёт у выбранного
    /// профиля, чтобы таблица не расходилась с выводом.
    /// </summary>
    public class SoundTable
    {
        /// <summary>Звуки в порядке алфавита — он же порядок показа.</summary>
        public List<ArabicSound> Sounds { get; set; } = new();
    }

    /// <summary>Одна буква и её звук.</summary>
    public class ArabicSound
    {
        /// <summary>
        /// Арабская буква — тот же ключ, что у графемы в профиле (<c>ع</c>): по нему
        /// страница находит, как буква пишется в выбранном профиле.
        /// </summary>
        public string Letter { get; set; } = string.Empty;

        /// <summary>Название буквы на каждом языке страницы (D7): «Айн», «ʿAyn».</summary>
        public LocalizedText Name { get; set; } = new();

        /// <summary>
        /// Как произносить — одна-две короткие фразы: где звук образуется и на какой
        /// знакомый звук похож.
        /// </summary>
        public LocalizedText Pronunciation { get; set; } = new();

        /// <summary>
        /// Звука нет в русском: такие страница показывает первыми — их и нужно
        /// выучить, остальные читаются как привычные буквы.
        /// </summary>
        public bool NotInRussian { get; set; }
    }
}
