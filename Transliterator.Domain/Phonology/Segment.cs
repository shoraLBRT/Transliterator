namespace Transliterator.Domain.Phonology
{
    /// <summary>
    /// Единица фонологического слоя: согласный вместе со своей огласовкой.
    /// <para>
    /// Правила таджвида работают именно с этим типом, а не с готовой кириллицей,
    /// потому что им нужны сукун, шадда, тип хамзы и границы слов — всё то,
    /// что при побуквенной замене стирается.
    /// </para>
    /// <para>
    /// Долгота хранится числом харакатов, а не удвоением графемы, поэтому
    /// мадд табии (2), муттасиль (4) и лязим (6) остаются различимыми до рендеринга.
    /// </para>
    /// </summary>
    public sealed class Segment
    {
        public SegmentKind Kind { get; set; } = SegmentKind.Consonant;

        /// <summary>Каноническая арабская буква. Она же ключ поиска в профиле.</summary>
        public string Letter { get; set; } = string.Empty;

        public Harakah Vowel { get; set; } = Harakah.None;

        /// <summary>
        /// Длительность гласной в харакатах: 1 — краткая, 2 — мадд табии, 4 и 6 — удлинённые.
        /// <para>
        /// У безгласного глайда тем же числом хранится мадд лин: там голос
        /// тянет сами و и ي, а не фатху перед ними.
        /// </para>
        /// </summary>
        public int VowelLength { get; set; } = 1;

        public bool Shadda { get; set; }

        /// <summary>Назализация: гунна на نّ и مّ, а также при идгаме и икалябе.</summary>
        public bool Ghunna { get; set; }

        public Emphasis Emphasis { get; set; } = Emphasis.Light;

        /// <summary>Вариант огласовки этого сегмента. Проставляется правилом эмфазы.</summary>
        public VowelVariant VowelVariant { get; set; } = VowelVariant.Plain;

        /// <summary>Буква написана, но не читается: васля в соединении, алиф аль-фарика, немой кружок.</summary>
        public bool Silent { get; set; }

        /// <summary>Хамзат аль-васль: озвучивается только в начале высказывания.</summary>
        public bool IsWaslHamza { get; set; }

        /// <summary>Нун, полученный разворачиванием танвина. Подчиняется правилам нун сакины наравне с написанным.</summary>
        public bool FromTanwin { get; set; }

        /// <summary>Та-марбута: /t/ в соединении, /h/ на паузе. Решается стадией вакфа.</summary>
        public bool IsTaMarbuta { get; set; }

        /// <summary>
        /// Первая половина удвоения (идгам солнечного ляма). Такой сегмент безгласен,
        /// но это не закрытый слог, поэтому вариант "|sukun" к нему не применяется.
        /// </summary>
        public bool IsGeminateFirstHalf { get; set; }

        /// <summary>
        /// Огласовка, которую с этого сегмента сняла пауза.
        /// <para>
        /// Пауза гасит звук огласовки, но не то, что эта огласовка сделала с согласным:
        /// ر в ٱلْفَجْرِ на паузе безгласна и всё равно мягкая — по своей исходной касре.
        /// Хранить исходную огласовку оказалось точнее, чем восстанавливать её потом
        /// по соседям: у безгласной ر и у обеззвученной паузой ر соседи одинаковые,
        /// а читаются они по-разному.
        /// </para>
        /// </summary>
        public Harakah OriginalVowel { get; set; } = Harakah.None;

        public bool StartsWord { get; set; }

        /// <summary>Знак вакфа, написанный на этой границе слов. Только для <see cref="SegmentKind.Break"/>.</summary>
        public WaqfMark Waqf { get; set; } = WaqfMark.None;

        /// <summary>
        /// На этой границе чтение прерывается. Проставляется стадией вакфа и после неё
        /// означает уже не совет мусхафа, а решение конвейера: правила сюда не заглядывают.
        /// </summary>
        public bool IsPause { get; set; }

        /// <summary>Дефис после сегмента при рендеринге: артикль.</summary>
        public bool HyphenAfter { get; set; }

        /// <summary>Для <see cref="SegmentKind.Break"/>, <see cref="SegmentKind.Digit"/> и <see cref="SegmentKind.Other"/> — готовый текст.</summary>
        public string Literal { get; set; } = string.Empty;

        public static Segment Break() =>
            new() { Kind = SegmentKind.Break, Literal = " " };

        public override string ToString() =>
            Kind == SegmentKind.Consonant
                ? $"{Letter}{(Shadda ? "ّ" : "")} {Vowel}x{VowelLength}{(Silent ? " silent" : "")}"
                : $"{Kind}:{Literal}";
    }
}
