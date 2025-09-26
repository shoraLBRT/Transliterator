namespace Transliterator.Domain.Exceptions
{
    public class TransliterationException : Exception
    {
        public TransliterationException(string message) : base(message) { }
        public TransliterationException(string message, Exception inner) : base(message, inner) { }
    }
}
