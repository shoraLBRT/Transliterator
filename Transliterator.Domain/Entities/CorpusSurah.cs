namespace Transliterator.Domain.Entities
{
    /// <summary>
    /// Сура в корпусе примеров: арабский текст и та транслитерация, которую
    /// проект считает правильной.
    /// <para>
    /// Корпус — данные ядра, а не тестов. Один и тот же файл читают тесты
    /// и интерфейс: иначе примеры на странице и примеры в тестах разъедутся
    /// ровно так, как разъехались хардкод-профиль и <c>Standard.json</c>.
    /// </para>
    /// <para>
    /// <see cref="CorpusAyah.Expected"/> задан по профилю <c>Standard</c>:
    /// это решение о системе записи, а не снимок вывода программы. Расхождение
    /// «вывод ≠ ожидание» — повод завести баг, а не поправить ожидание.
    /// </para>
    /// </summary>
    public class CorpusSurah
    {
        /// <summary>Номер суры в Коране, от 1 до 114. Он же — имя файла.</summary>
        public int Number { get; set; }

        public string ArabicName { get; set; } = string.Empty;

        public string RussianName { get; set; } = string.Empty;

        /// <summary>
        /// Редакция арабского текста. От неё зависит половина ожидаемых значений:
        /// в написании со знаком васлы (<c>ٱ</c>) опознавать нечего, а в современном
        /// (<c>ا</c>) васлю ищет разбор — и вывод получается разный.
        /// Известные значения перечислены в <see cref="CorpusTextEdition"/>.
        /// </summary>
        public string TextEdition { get; set; } = string.Empty;

        public List<CorpusAyah> Ayahs { get; set; } = new();
    }

    /// <summary>Один аят: номер, арабский текст, ожидаемая транслитерация.</summary>
    public class CorpusAyah
    {
        /// <summary>Номер аята внутри суры, от 1.</summary>
        public int Number { get; set; }

        public string Arabic { get; set; } = string.Empty;

        /// <summary>Ожидаемая транслитерация по профилю <c>Standard</c>.</summary>
        public string Expected { get; set; } = string.Empty;
    }

    /// <summary>Редакции арабского текста, которые корпус умеет различать.</summary>
    public static class CorpusTextEdition
    {
        /// <summary>
        /// Усмани со знаком хамзат аль-васль (<c>ٱ</c>), надстрочным алифом
        /// и малыми восстановительными буквами — то, как текст напечатан в мусхафе.
        /// </summary>
        public const string UthmaniWasl = "uthmani-wasl";

        public static bool IsKnown(string edition) => edition == UthmaniWasl;
    }
}
