using System.Text.Json.Serialization;

namespace Transliterator.Domain.Entities
{
    /// <summary>
    /// Каталог правил таджвида (I1): что за правило, как оно меняет запись,
    /// к какой ступени чтения относится и какие варианты счёта допускает.
    /// Данные, а не интерфейс и не сами ступени: и то, и другое строится поверх
    /// каталога (I2).
    /// <para>
    /// Уровней два, потому что стадия конвейера — не то же, что правило:
    /// стадия 7 исполняет изхар, идгам, икляб и ихфу, стадия 9 — несколько видов
    /// мадда. Человеку понятнее правила, а исполняет их стадия: правило поэтому
    /// всегда знает свою стадию.
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

        /// <summary>
        /// Перевод названия для того, кто не знает арабского (I7): «Мадд лязим» —
        /// арабский термин русскими буквами, рядом с ним — «обязательное удлинение».
        /// Название, которое уже по-русски («Лунный лям»), получает вместо перевода
        /// короткое пояснение, что правило делает: повтор названия ничего не объяснит.
        /// Страница пишет его в скобках после названия везде, где показывает правило.
        /// </summary>
        public string Translation { get; set; } = string.Empty;

        /// <summary>Арабский термин: <c>مد لازم</c>.</summary>
        public string ArabicName { get; set; } = string.Empty;

        /// <summary>Коротко и понятно тому, кто не учил таджвид.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Примеры с выводом по профилю <c>Standard</c>. Каждый сверяется тестом
        /// с конвейером: описание не разойдётся с кодом молча.
        /// </summary>
        public List<TajweedRuleExample> Examples { get; set; } = new();

        /// <summary>
        /// Остаётся ли правило в обычном чтении без таджвида. Решено владельцем
        /// 2026-11-04: правила выбираются не по одному, а ступенями чтения.
        /// </summary>
        public TajweedTier Tier { get; set; }

        /// <summary>
        /// Допустимые счёты в харакатах, по возрастанию, — у правил, где сам
        /// таджвид даёт выбор: мадд арид тянут 2, 4 или 6 счётов. У правила без
        /// выбора пусто.
        /// </summary>
        public List<int> Counts { get; set; } = new();

        /// <summary>Счёт, который конвейер берёт, пока человек не выбрал свой. Без выбора — 0.</summary>
        public int DefaultCount { get; set; }

        /// <summary>
        /// Правило можно выключить по одному в «Неполном таджвиде» (I4, решено владельцем
        /// 2026-11-12). Только у правил ступени «таджвид», которые не меняют того, что
        /// видят следующие стадии: кальканя, гунна (идгам с гунной, икляб, ихфа)
        /// и мадд силя. Вакф, васля, артикль и идгам без гунны так не выключаются —
        /// без них ломается чтение, а не упрощается. Выключенное правило читается
        /// так, как в обычном чтении.
        /// </summary>
        public bool Switchable { get; set; }

        /// <summary>Таджвид даёт здесь выбор — правило из «вариантов внутри таджвида».</summary>
        [JsonIgnore]
        public bool HasVariants => Counts.Count > 0;
    }

    /// <summary>
    /// Ступень правила. Ступени чтения на странице (I2) — полный таджвид,
    /// варианты внутри таджвида, обычное чтение — строятся из неё и из
    /// <see cref="TajweedRule.Counts"/>: варианты есть у правил обеих ступеней.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TajweedTier
    {
        /// <summary>
        /// Есть в любом чтении: так звучит сам арабский язык — паузальные формы,
        /// артикль, твёрдые буквы, слияние двух одинаковых согласных.
        /// </summary>
        Base,

        /// <summary>
        /// Работает только в чтении Корана и в обычном чтении убирается: буквы
        /// не сливаются, долготы — обычные два счёта, силя и отзвук не звучат.
        /// </summary>
        Tajweed,
    }

    /// <summary>Арабский текст и то, что даёт по нему конвейер.</summary>
    public class TajweedRuleExample
    {
        public string Arabic { get; set; } = string.Empty;

        public string Transliteration { get; set; } = string.Empty;
    }
}
