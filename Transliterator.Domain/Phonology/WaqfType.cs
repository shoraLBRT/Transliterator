namespace Transliterator.Domain.Phonology
{
    /// <summary>
    /// Типы пауз (вакф) в кораническом тексте. Определяют, произойдёт ли остановка
    /// и применятся ли правила паузального произношения.
    /// <para>
    /// Политика применения вакфа:
    /// <list type="bullet">
    ///   <item><see cref="None"/>: соединение, никаких правил паузы не применяется.</item>
    ///   <item><see cref="Optional"/>: ۖ (паузу можно использовать), остаются правила паузы.</item>
    ///   <item><see cref="Preferred"/>: ۗ (предпочтительна пауза), применяются правила паузы.</item>
    ///   <item><see cref="Obligatory"/>: ۘ (обязательная пауза), применяются правила паузы.</item>
    ///   <item><see cref="Forbidden"/>: ۙ (запрещена пауза), соединение, никаких правил.</item>
    ///   <item><see cref="Dual"/>: ۚ (пауза в одном из двух мест), применяются правила паузы.</item>
    /// </list>
    /// </para>
    /// </summary>
    public enum WaqfType
    {
        None = 0,
        Optional = 1,
        Preferred = 2,
        Obligatory = 3,
        Forbidden = 4,
        Dual = 5
    }
}
