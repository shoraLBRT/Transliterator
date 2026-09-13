using Transliterator.Domain.Entities;
using Transliterator.Domain.Exceptions;

namespace Transliterator.Domain.Interfaces
{
    public interface ITransliterationService
    {
        Task<TransliterationResult> TransliterateAsync(string arabicText, string? profile = null);

        /// <summary>
        /// Транслитерация профилем, переданным целиком, — без обращения к хранилищу.
        /// <para>
        /// Путь для профиля, которого в хранилище нет и не будет: кастомный профиль
        /// пользователя живёт в localStorage браузера, черновик редактора — в памяти
        /// страницы. Профиль не сохраняется и не кешируется; результат несёт его имя.
        /// </para>
        /// <para>
        /// Профиль проверяется до расчёта, даже при пустом тексте: у него должно быть
        /// имя, хотя бы одно правило, ни одного пустого ключа и ни одного значения
        /// <c>null</c>. Пустая строка значением — законная запись (так в <c>Standard</c>
        /// заданы отзвук кальканя и начальная хамза).
        /// </para>
        /// <para>
        /// Неполный набор правил допустим: варианты (<c>"ر|heavy"</c>) откатываются
        /// к базовому ключу, и профиль вправе их не задавать. Ошибка — только базовый
        /// ключ, который понадобился этому тексту и которого в профиле нет:
        /// такой звук иначе молча исчез бы из вывода. В сообщении перечислены все
        /// недостающие ключи разом, а не первый попавшийся.
        /// </para>
        /// </summary>
        /// <exception cref="ArgumentNullException">Профиль не передан.</exception>
        /// <exception cref="TransliterationException">
        /// Профиль не прошёл проверку или в нём нет ключа, нужного тексту.
        /// </exception>
        TransliterationResult Transliterate(string arabicText, TransliterationProfile profile);

        Task UpdateRuleAsync(string arabicLetter, string cyrillicMapping, string? profile = null);
        Task<Dictionary<string, string>> GetRulesAsync(string? profile = null);
        Task<IEnumerable<string>> GetAvailableProfilesAsync();
    }
}
