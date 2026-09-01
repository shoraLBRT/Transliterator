namespace Transliterator.Domain.Phonology
{
    /// <summary>
    /// Огласовка сегмента.
    /// <see cref="None"/> и <see cref="Sukun"/> — разные состояния: первое означает,
    /// что огласовка не проставлена, второе — что она проставлена и буква явно безгласна.
    /// Именно их смешение (сукун отображался в пустую строку) ломало разбор ي с сукуном.
    /// </summary>
    public enum Harakah
    {
        None,
        Fatha,
        Damma,
        Kasra,
        Sukun
    }
}
