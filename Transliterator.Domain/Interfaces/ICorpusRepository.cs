using Transliterator.Domain.Entities;

namespace Transliterator.Domain.Interfaces
{
    /// <summary>
    /// Чтение корпуса примеров. Только чтение: корпус правят руками в репозитории,
    /// а не из приложения — ожидаемая транслитерация там эталон, и записывать
    /// в него вывод программы значит закреплять в тестах в том числе её ошибки.
    /// </summary>
    public interface ICorpusRepository
    {
        Task<CorpusSurah?> GetSurahAsync(int number);

        /// <summary>Все суры корпуса по возрастанию номера.</summary>
        Task<IReadOnlyList<CorpusSurah>> GetAllSurahsAsync();
    }
}
