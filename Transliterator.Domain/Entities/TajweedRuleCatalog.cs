namespace Transliterator.Domain.Entities
{
    /// <summary>
    /// Каталог правил таджвида (I1): что за правило, как оно меняет запись,
    /// и можно ли его выключить. Данные, а не интерфейс и не само отключение —
    /// и то, и другое строится поверх каталога.
    /// <para>
    /// Уровней два, потому что стадия конвейера — не то же, что правило:
    /// стадия 7 исполняет изхар, идгам, икляб и ихфу, стадия 9 — несколько видов
    /// мадда. Человеку понятнее правила, а отключать, возможно, придётся стадиями
    /// или их частями: правило поэтому всегда знает свою стадию.
    /// </para>
    /// <para>
    /// Нормализация, разбор и рендеринг (стадии 1, 2 и 11) правилами таджвида
    /// не являются и в каталог не входят: в нём только то, что исполняет
    /// <c>RulesService</c>.
    /// </para>
    /// </summary>
    public class TajweedRuleCatalog
    {
        /// <summary>Стадии в порядке исполнения — он же порядок показа.</summary>
        public List<TajweedStage> Stages { get; set; } = new();

        /// <summary>Правила, сгруппированные по стадиям в том же порядке.</summary>
        public List<TajweedRule> Rules { get; set; } = new();
    }

    /// <summary>Стадия конвейера, как её нумерует <c>docs/ROADMAP.md</c>.</summary>
    public class TajweedStage
    {
        /// <summary>Номер стадии из <c>ROADMAP.md</c>: от 3 до 10.</summary>
        public int Number { get; set; }

        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Класс, который исполняет стадию (<c>WaqfRule</c>, <c>NasalRule</c>…).
        /// По нему каталог сверяется с <c>RulesService</c>: стадия без записи
        /// роняет тест, а не пропадает со страницы молча.
        /// </summary>
        public string Class { get; set; } = string.Empty;
    }

    /// <summary>Одно правило таджвида — то, что увидит человек.</summary>
    public class TajweedRule
    {
        /// <summary>Устойчивый ключ правила: на него будет ссылаться выбор правил.</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Номер стадии, которая исполняет правило.</summary>
        public int Stage { get; set; }

        /// <summary>Название по-русски: «Мадд лязим».</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Арабский термин: <c>مد لازم</c>.</summary>
        public string ArabicName { get; set; } = string.Empty;

        /// <summary>Коротко и понятно тому, кто не учил таджвид.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Примеры с выводом по профилю <c>Standard</c>. Каждый сверяется тестом
        /// с конвейером: описание не разойдётся с кодом молча.
        /// </summary>
        public List<TajweedRuleExample> Examples { get; set; } = new();

        /// <summary>Можно ли выключить правило, не сломав чтение.</summary>
        public bool CanDisable { get; set; }

        /// <summary>Почему выключить нельзя. У отключаемого правила пусто.</summary>
        public string WhyNotDisable { get; set; } = string.Empty;
    }

    /// <summary>Арабский текст и то, что даёт по нему конвейер.</summary>
    public class TajweedRuleExample
    {
        public string Arabic { get; set; } = string.Empty;

        public string Transliteration { get; set; } = string.Empty;
    }
}
