namespace Transliterator.Domain.Phonology
{
    /// <summary>
    /// Вариант огласовки, выбираемый согласным. Позволяет держать окраску гласной
    /// в профиле ("َ|heavy", "َ|soft"), а не в коде правила.
    /// </summary>
    public enum VowelVariant
    {
        Plain,

        /// <summary>После эмфатического согласного: а → о.</summary>
        Heavy,

        /// <summary>После мягкого ляма: а → я.</summary>
        Soft
    }
}
