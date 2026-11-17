using Microsoft.Extensions.Logging.Abstractions;
using Transliterator.Core.Repositories;
using Transliterator.Domain.Entities;
using Transliterator.Domain.Exceptions;
using Transliterator.Domain.Localization;
using Xunit;

namespace Transliterator.Tests.RulesTests
{
    /// <summary>
    /// Таблица звуков (D11): у каждой буквы — название и как произносить на каждом
    /// языке страницы, и каждая буква — ключ графемы встроенных профилей: по нему
    /// страница показывает, как буква пишется в выбранном профиле.
    /// </summary>
    public class SoundTableTests
    {
        private static SoundTable Table => EmbeddedSoundTable.Table;

        /// <summary>Хамза и 28 букв алфавита — в порядке алфавита, хамза первой.</summary>
        private const string Alphabet = "ءابتثجحخدذرزسشصضطظعغفقكلمنهوي";

        [Fact]
        public void Table_CoversTheAlphabetAndHamza_InOrder()
        {
            Assert.Equal(Alphabet.Select(c => c.ToString()), Table.Sounds.Select(s => s.Letter));
        }

        [Fact]
        public void EverySound_HasItsTexts_InEveryPageLanguage()
        {
            foreach (var sound in Table.Sounds)
                foreach (var language in PageLanguages.All)
                {
                    Assert.True(sound.Name.Has(language.Code), $"{sound.Letter}: no name in {language.Code}");
                    Assert.True(sound.Pronunciation.Has(language.Code), $"{sound.Letter}: no pronunciation in {language.Code}");
                }
        }

        [Theory]
        [InlineData(ProfileNames.Cyrillic)]
        [InlineData(ProfileNames.Latin)]
        public async Task EveryLetter_IsAGraphemeKey_OfTheBuiltInProfile(string name)
        {
            var profile = await new EmbeddedProfileRepository(NullLogger<EmbeddedProfileRepository>.Instance).GetProfileAsync(name);

            Assert.NotNull(profile);
            foreach (var sound in Table.Sounds)
                Assert.True(profile!.Rules.ContainsKey(sound.Letter), $"{name} has no grapheme for {sound.Letter}");
        }

        [Fact]
        public void SoundsRussianLacks_AreMarked()
        {
            // Эти звуки страница показывает первыми: привычной буквой их не прочитать.
            // و — согласный w, в русском его нет; ي — это «й», он есть.
            var marked = Table.Sounds.Where(s => s.NotInRussian).Select(s => s.Letter);

            Assert.Equal("ءثحخذصضطظعغقهو".Select(c => c.ToString()), marked);
        }

        [Fact]
        public void Table_IsReadFromTheAssembly_NotFromDisk()
        {
            Assert.Contains(EmbeddedSoundTable.ResourceName, typeof(EmbeddedSoundTable).Assembly.GetManifestResourceNames());
        }

        [Theory]
        [InlineData("NoEnglishPronunciation", "no pronunciation in en")]
        [InlineData("TwiceDeclared", "declared twice")]
        public void BrokenTable_FailsToLoad(string name, string message)
        {
            var exception = Assert.Throws<TransliterationException>(() =>
                EmbeddedSoundTable.Load(typeof(SoundTableTests).Assembly,
                                        $"Transliterator.Tests.Resources.BrokenSoundTables.{name}.json"));

            Assert.Contains(message, exception.Message);
        }
    }
}
