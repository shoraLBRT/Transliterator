namespace Transliterator.Domain.Phonology
{
    public enum SegmentKind
    {
        /// <summary>Согласный со своей огласовкой — основная единица слоя.</summary>
        Consonant,

        /// <summary>Граница слова. При слиянии по васле рендерится дефисом, а не пробелом.</summary>
        Break,

        /// <summary>Номер аята. Одновременно граница высказывания для правила васли.</summary>
        Digit,

        /// <summary>Всё остальное — переносится в вывод без изменений.</summary>
        Other
    }
}
