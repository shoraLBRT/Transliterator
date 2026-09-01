using Transliterator.Domain.Phonology;
using Xunit;

namespace Transliterator.Tests.RulesTests
{
    /// <summary>
    /// Проверки слоя фонем: разбор снимает неоднозначности письма до того,
    /// как к тексту прикоснутся правила таджвида.
    /// </summary>
    public class PhonologyTests
    {
        [Fact]
        public void Sukun_IsDistinctFromMissingHarakah()
        {
            // Прежде сукун отображался в пустую строку и был неотличим от «огласовки нет».
            var segments = TransliterationPipeline.Consonants("عَلَيْهِمْ");

            var ya = Assert.Single(segments, s => s.Letter == "ي");
            Assert.Equal(Harakah.Sukun, ya.Vowel);
        }

        [Fact]
        public void Tanwin_ExpandsToNunSakin()
        {
            // Танвин должен стать настоящим нуном, иначе правила нун сакины
            // пришлось бы дублировать для него отдельно.
            var segments = TransliterationPipeline.Consonants("نَارٌ");

            var nun = Assert.Single(segments, s => s.FromTanwin);
            Assert.Equal("ن", nun.Letter);
            Assert.Equal(Harakah.Sukun, nun.Vowel);
        }

        [Fact]
        public void HamzaCarriers_CollapseToSingleConsonant()
        {
            foreach (var word in new[] { "أَنْعَمْتَ", "إِذْ", "قُرْءَانِ", "شَيْءٍ" })
                Assert.Contains(TransliterationPipeline.Consonants(word), s => s.Letter == "ء");
        }

        [Theory]
        [InlineData("مَـٰلِكِ", "م", 2)]   // надстрочный алиф — долгая ā
        [InlineData("نَارٌ", "ن", 2)]      // голый алиф удлиняет фатху
        [InlineData("ٱلسَّمَآءِ", "م", 4)] // мадд муттасиль перед хамзой
        public void LongVowels_FoldIntoCarrierWithLength(string word, string letter, int expectedLength)
        {
            var segment = TransliterationPipeline.Consonants(word).First(s => s.Letter == letter);

            Assert.Equal(expectedLength, segment.VowelLength);
        }

        [Fact]
        public void MaddLazim_IsSixHarakat()
        {
            // Долгая ā перед удвоенным лямом: ٱلضَّآلِّينَ.
            // Первая ض — это ассимилированный лям артикля, долготу несёт вторая.
            var segment = TransliterationPipeline.Consonants("ٱلضَّآلِّينَ")
                .Last(s => s.Letter == "ض");

            Assert.Equal(6, segment.VowelLength);
        }

        [Fact]
        public void SilentAlefAlFariqa_DoesNotBecomeVowel()
        {
            // Алиф после глагольного "ـوا" не читается: أُوتُوا → "уутуу", а не "уутууа".
            Assert.Equal("уутуу", TransliterationPipeline.Transliterate("أُوتُوا"));
        }
    }
}
