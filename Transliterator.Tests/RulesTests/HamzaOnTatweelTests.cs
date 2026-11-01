using Transliterator.Domain.Phonology;
using Xunit;

namespace Transliterator.Tests.RulesTests
{
    /// <summary>
    /// Хамза на татвиле (B13). Усмани пишет хамзу без носителя внутри слова
    /// надстрочным знаком на татвиле: татвиль, огласовка, U+0654. Это та же хамза,
    /// что и на носителе, и читается она так же — со своей огласовкой, а не
    /// приросшей к предыдущей букве.
    /// </summary>
    public class HamzaOnTatweelTests
    {
        [Theory]
        [InlineData("يَسْـَٔلُونَ", "يَسْأَلُونَ", "йасъалююн")]
        [InlineData("شَيْـًٔا", "شَيْئًا", "щайъаа")]            // и танвин на месте
        [InlineData("ٱلْـَٔاخِرَةِ", "ٱلْءَاخِرَةِ", "аль-ъаахъироh")] // и артикль на месте
        public void HamzaOnTatweel_ReadsLikeHamzaOnCarrier(string tatweel, string carrier, string expected)
        {
            Assert.Equal(expected, TransliterationPipeline.Transliterate(carrier));
            Assert.Equal(expected, TransliterationPipeline.Transliterate(tatweel));
        }

        [Theory]
        [InlineData("مَسْـُٔولًا", "مَسْؤُولًا")]
        [InlineData("ٱلْأَفْـِٔدَةِ", "ٱلْأَفْئِدَةِ")]
        public void HamzaOnTatweel_ReadsLikeHamzaOnCarrier_WithEveryVowel(string tatweel, string carrier) =>
            // Дамма и касра на татвиле — те же, что на носителе. Ожидание здесь —
            // само написание через носитель, а не строка вывода: какой графемой
            // писать хамзу, решает профиль (G2), а не этот баг.
            Assert.Equal(TransliterationPipeline.Transliterate(carrier),
                         TransliterationPipeline.Transliterate(tatweel));

        [Fact]
        public void HamzaOnTatweel_IsHamzaConsonantWithItsOwnVowel()
        {
            var segments = TransliterationPipeline.Consonants("يَسْـَٔلُونَ");

            var hamza = Assert.Single(segments, s => s.Letter == "ء");
            Assert.Equal(Harakah.Fatha, hamza.Vowel);

            // Прежде фатха хамзы прирастала к сину, и тот из безгласного становился огласованным.
            var sin = Assert.Single(segments, s => s.Letter == "س");
            Assert.Equal(Harakah.Sukun, sin.Vowel);
        }

        [Fact]
        public void HamzaOnTatweel_DoesNotDependOnTypedMarkOrder()
        {
            // Хамза набрана раньше огласовки — NFC переставит знаки в канонический порядок.
            const string hamzaFirst = "يَسْـَٔلُونَ";

            Assert.Equal("йасъалююн", TransliterationPipeline.Transliterate(hamzaFirst));
        }

        [Fact]
        public void PlainTatweel_IsStillDropped()
        {
            // Татвиль без хамзы — по-прежнему только соединительная черта.
            Assert.Equal(TransliterationPipeline.Transliterate("مَٰلِكِ"),
                         TransliterationPipeline.Transliterate("مَـٰلِكِ"));
        }
    }
}
