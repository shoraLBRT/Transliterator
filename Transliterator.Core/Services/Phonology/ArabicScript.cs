namespace Transliterator.Core.Services.Phonology
{
    /// <summary>
    /// Кодовые точки и наборы букв арабского письма. Единственное место в проекте,
    /// где арабские символы задаются явно.
    /// </summary>
    public static class ArabicScript
    {
        // --- огласовки и знаки ---
        public const char Fathatan = 'ً'; // ً
        public const char Dammatan = 'ٌ'; // ٌ
        public const char Kasratan = 'ٍ'; // ٍ
        public const char Fatha = 'َ';    // َ
        public const char Damma = 'ُ';    // ُ
        public const char Kasra = 'ِ';    // ِ
        public const char Shadda = 'ّ';   // ّ
        public const char Sukun = 'ْ';    // ْ
        public const char Maddah = 'ٓ';   // ٓ
        public const char SuperscriptAlef = 'ٰ'; // ٰ
        public const char Tatweel = 'ـ';  // ـ

        // --- кораническая разметка ---
        public const char QuranicSukun = 'ۡ';          // ۡ  сукун в некоторых мусхафах
        public const char SmallHighRoundedZero = '۟';  // ۟  буква не читается
        public const char SmallHighUprightZero = '۠';  // ۠  буква не читается
        public const char SmallWaw = 'ۥ';              // ۥ  восстановленная долгая ū
        public const char SmallYa = 'ۦ';               // ۦ  восстановленная долгая ī

        /// <summary>
        /// ۢ  Знак икляба: малая мим над нуном или танвином (مِنۢ بَعْدِ).
        /// Стоит вместо сукуна и потому несёт звук, а не совет чтецу —
        /// нормализация обязана его сохранить, а разбор прочесть как безгласность.
        /// </summary>
        public const char SmallHighMeemIsolated = 'ۢ';
        public const char EndOfAyah = '۝';             // ۝

        // --- знаки вакфа (U+06D6..U+06DC) ---
        // Не звук, а совет чтецу об уместности остановки. Стадия вакфа читает их,
        // поэтому нормализация обязана их сохранить, а не выбросить как прочую разметку.
        public const char WaqfContinuePreferred = 'ۖ'; // ۖ  صلے
        public const char WaqfStopPreferred = 'ۗ';     // ۗ  قلے
        public const char WaqfObligatory = 'ۘ';        // ۘ  مـ
        public const char WaqfForbidden = 'ۙ';         // ۙ  لا
        public const char WaqfPermissible = 'ۚ';       // ۚ  ج
        public const char WaqfEmbracing = 'ۛ';         // ۛ  معانقة
        public const char WaqfSaktah = 'ۜ';            // ۜ  س

        // --- буквы ---
        public const char Hamza = 'ء';           // ء
        public const char AlefMadda = 'آ';       // آ
        public const char AlefHamzaAbove = 'أ';  // أ
        public const char WawHamza = 'ؤ';        // ؤ
        public const char AlefHamzaBelow = 'إ';  // إ
        public const char YehHamza = 'ئ';        // ئ
        public const char Alef = 'ا';            // ا
        public const char TaMarbuta = 'ة';       // ة
        public const char AlefWasla = 'ٱ';       // ٱ
        public const char AlefWavyHamzaAbove = 'ٲ'; // ٲ
        public const char AlefWavyHamzaBelow = 'ٳ'; // ٳ
        public const char AlefMaqsura = 'ى';     // ى
        public const char Waw = 'و';             // و
        public const char Yeh = 'ي';             // ي
        public const char Lam = 'ل';             // ل
        public const char Nun = 'ن';             // ن
        public const char Meem = 'م';            // م
        public const char Ba = 'ب';              // ب
        public const char Ra = 'ر';              // ر
        public const char Ha = 'ه';              // ه

        public const string HamzaStr = "ء";
        public const string AlefStr = "ا";
        public const string MeemStr = "م";
        public const string LamStr = "ل";
        public const string NunStr = "ن";
        public const string HaStr = "ه";
        public const string TaMarbutaStr = "ة";
        public const string AlefWaslaStr = "ٱ";

        /// <summary>Солнечные буквы: лям артикля перед ними ассимилируется.</summary>
        public static readonly HashSet<char> SunLetters = new()
        {
            'ت', // ت
            'ث', // ث
            'د', // د
            'ذ', // ذ
            'ر', // ر
            'ز', // ز
            'س', // س
            'ش', // ش
            'ص', // ص
            'ض', // ض
            'ط', // ط
            'ظ', // ظ
            'ل', // ل
            'ن'  // ن
        };

        /// <summary>
        /// Буквы изхара халькы: нун сакина перед ними произносится чисто, без назализации.
        /// Все они гортанные — соединить с ними носовой призвук нечем.
        /// </summary>
        public static readonly HashSet<char> ThroatLetters = new()
        {
            Hamza,
            'ه', // ه
            'ع', // ع
            'ح', // ح
            'غ', // غ
            'خ'  // خ
        };

        /// <summary>Буквы идгама с гунной (يرملون без ل и ر): нун сливается с ними, назализация остаётся.</summary>
        public static readonly HashSet<char> IdghamWithGhunna = new()
        {
            Yeh, Nun, Meem, Waw
        };

        /// <summary>Буквы идгама без гунны: нун исчезает в них бесследно.</summary>
        public static readonly HashSet<char> IdghamWithoutGhunna = new()
        {
            Lam, Ra
        };

        /// <summary>
        /// Буквы кальканя (قطب جد): взрывные, у которых смычка полная и звонкая.
        /// Безгласными их не удержать — размыкание слышно отдельным отзвуком.
        /// </summary>
        public static readonly HashSet<char> QalqalahLetters = new()
        {
            'ق', // ق
            'ط', // ط
            'ب', // ب
            'ج', // ج
            'د'  // د
        };

        /// <summary>Буквы истиля — всегда произносятся твёрдо (тафхим).</summary>
        public static readonly HashSet<char> AlwaysHeavy = new()
        {
            'خ', // خ
            'ص', // ص
            'ض', // ض
            'ط', // ط
            'ظ', // ظ
            'غ', // غ
            'ق'  // ق
        };

        /// <summary>Все согласные-носители, которые могут стать сегментом.</summary>
        public static readonly HashSet<char> Consonants = new()
        {
            Hamza, AlefMadda, AlefHamzaAbove, WawHamza, AlefHamzaBelow, YehHamza,
            Alef, AlefWasla, AlefWavyHamzaAbove, AlefWavyHamzaBelow, AlefMaqsura,
            TaMarbuta, Waw, Yeh,
            'ب', // ب
            'ت', // ت
            'ث', // ث
            'ج', // ج
            'ح', // ح
            'خ', // خ
            'د', // د
            'ذ', // ذ
            'ر', // ر
            'ز', // ز
            'س', // س
            'ش', // ش
            'ص', // ص
            'ض', // ض
            'ط', // ط
            'ظ', // ظ
            'ع', // ع
            'غ', // غ
            'ف', // ف
            'ق', // ق
            'ك', // ك
            'ل', // ل
            'م', // م
            'ن', // ن
            'ه'  // ه
        };

        /// <summary>Носители хамзы. Все сводятся к одному сегменту ء со своей огласовкой.</summary>
        public static readonly HashSet<char> HamzaCarriers = new()
        {
            Hamza, AlefHamzaAbove, AlefHamzaBelow, WawHamza, YehHamza,
            AlefWavyHamzaAbove, AlefWavyHamzaBelow
        };

        public static bool IsDiacritic(char c) =>
            c is >= 'ً' and <= 'ٕ'
            || c == SuperscriptAlef
            || c == QuranicSukun
            || c == SmallHighRoundedZero
            || c == SmallHighUprightZero
            || c == SmallHighMeemIsolated;

        public static bool IsArabicDigit(char c) => c is >= '٠' and <= '٩';

        /// <summary>Знак вакфа. Единственная разметка, которая доживает до слоя сегментов.</summary>
        public static bool IsWaqfMark(char c) =>
            c is >= WaqfContinuePreferred and <= WaqfSaktah;

        /// <summary>Знак икляба. Как и знак вакфа, переживает нормализацию: его читает стадия 6.</summary>
        public static bool IsIqlabMark(char c) => c == SmallHighMeemIsolated;

        /// <summary>
        /// Разметка, не несущая звука: знаки вакфа, разделители аятов и пометы чтеца.
        /// Знаки вакфа и знак икляба опознаются и здесь, но нормализация их сохраняет —
        /// их читают стадии 3 и 6.
        /// </summary>
        public static bool IsRecitationMark(char c) =>
            IsWaqfMark(c)
            || c == EndOfAyah
            || c is >= 'ۢ' and <= 'ۤ'
            || c is >= 'ۧ' and <= 'ۭ'
            || c is >= 'ؕ' and <= 'ؚ';
    }
}
