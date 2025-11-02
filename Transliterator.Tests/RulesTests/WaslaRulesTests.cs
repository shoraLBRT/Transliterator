using Transliterator.Core.Services.Rules;
using Transliterator.Domain.Entities;
using Xunit;

namespace Transliterator.Tests.RulesTests
{
    public class WaslaRulesTests
    {
        private readonly WaslaRules _rules;
        private readonly TransliterationProfile _profile;
        public WaslaRulesTests()
        {
            _rules = new WaslaRules();
            _profile = TestProfiles.StandardProfile;
        }

        [Theory]
        [InlineData("бисми áлллаhи", "бисми-ллаhи")]
        [InlineData("бисми áлллаhи áлррахIмаани áлррахIийми", "бисми-ллаhи-ррахIмаани-ррахIийми")]
        public void ProcessWaslaAlif_MergeWords_MergeCorrectly(string cyrrilicInput, string expected)
        {
            var result = _rules.ProcessWaslaAlif(cyrrilicInput, _profile);
            Assert.Equal(expected, result);
        }
        [Theory]
        [InlineData("2 áлррахIмаани", "2 áлррахIмаани")]
        [InlineData("5 áhдинаа", "5 иhдинаа")]
        public void ProcessWaslaAlif_FirstWasla_SettingVowelCorrectly(string cyrrilicInput, string expected)
        {
            var result = _rules.ProcessWaslaAlif(cyrrilicInput, _profile);
            Assert.Equal(expected, result);
        }

        [Theory(Skip = "Не реализовано")]
        [InlineData("áдхъул", "удхъуль")]
        public void ProcessWaslaAlif_FirstWaslaWithDamma_SettingVowelCorrectly(string cyrrilicInput, string expected)
        {
            var result = _rules.ProcessWaslaAlif(cyrrilicInput, _profile);
            Assert.Equal(expected, result);
        }
    }
}
