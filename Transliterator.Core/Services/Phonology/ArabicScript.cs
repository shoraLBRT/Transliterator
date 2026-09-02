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
        public const char EndOfAyah = '۝';             // ۝

        // --- знаки вакфа (пауз) ---
        public const char WaqfStart = 'ۖ';  // ۖ (U+06D6) — начало диапазона знаков вакфа
        public const char WaqfEnd = 'ۜ';    // ۜ (U+06DC) — конец диапазона знаков вакфа

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
        public const string LamStr = "ل";
        public const string NunStr = "ن";
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
            || c == SmallHighUprightZero;

        public static bool IsArabicDigit(char c) => c is >= '٠' and <= '٩';

        /// <summary>
        /// Знаки паузы, разделители аятов и пометы чтеца. Звука не несут;
        /// на стадии вакфа они понадобятся как подсказки, здесь просто опознаются.
        /// </summary>
        public static bool IsRecitationMark(char c) =>
            c is >= 'ۖ' and <= 'ۜ'   // знаки вакфа
            || c == EndOfAyah
            || c is >= 'ۢ' and <= 'ۤ'
            || c is >= 'ۧ' and <= 'ۭ'
            || c is >= 'ؕ' and <= 'ؚ';
    }
}
