namespace Transliterator.Domain.Exceptions
{
    /// <summary>
    /// Хранилищу не хватило места на запись. Отдельный тип, а не общий сбой:
    /// эту ошибку пользователь может исправить сам — удалить лишний профиль.
    /// </summary>
    public class StorageQuotaExceededException : Exception
    {
        public StorageQuotaExceededException(string message) : base(message) { }

        public StorageQuotaExceededException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
