using Transliterator.Core.Services;
using Xunit;

namespace Transliterator.Tests.ProfileTests
{
    /// <summary>
    /// Спорные графемы (E5): буква, которую по выводу не отличить от другой
    /// или от сочетания двух. Профиль с таким спором сохраняется — это решение
    /// пользователя, и страница только предупреждает.
    /// </summary>
    public class GraphemeConflictsTests
    {
        private static Dictionary<string, string> Standard(params (string Key, string Value)[] changes)
        {
            var rules = new Dictionary<string, string>(TestProfiles.Standard.Rules);
            foreach (var (key, value) in changes)
                rules[key] = value;

            return rules;
        }

        [Fact]
        public void QafAsKaHamza_IsComposedOfKafAndHamza()
        {
            // Пример владельца (G5): «къ» для ق при хамзе «ъ» — это «к + хамза».
            // Standard пишет хамзу дефисом (G2), и «ъ» ей задаёт свой профиль.
            var conflict = Assert.Single(GraphemeConflicts.Find(Standard(("ق", "къ"), ("ء", "ъ"))),
                                         c => c.Letter == "ق");

            Assert.True(conflict.IsComposed);
            Assert.Equal(new[] { "ك", "ء" }, conflict.Parts);
            Assert.True(conflict.Involves("ء"));
        }

        [Fact]
        public void SameGraphemeAsAnotherLetter_IsAConflict_ReportedOnce()
        {
            var conflict = Assert.Single(GraphemeConflicts.Find(Standard(("ق", "к"))), c => c.Involves("ق"));

            Assert.False(conflict.IsComposed);
            Assert.Equal(new[] { "ق", "ك" }.Order(), conflict.Letters.Order());
        }

        [Fact]
        public void Standard_HasNoConflicts() =>
            // Хамза дефисом (G2) сняла единственный спор Standard — ذ «зъ».
            // ت и ة пишутся одинаково по праву, это один звук.
            Assert.Empty(GraphemeConflicts.Find(TestProfiles.Standard.Rules));

        [Fact]
        public void HamzaAsHardSign_ConflictsWithDhal()
        {
            // ذ «зъ» при хамзе «ъ» — тот самый спор, с которого начался G2: так
            // писал Standard до него и так пишет свой профиль, скопированный раньше.
            var conflict = Assert.Single(GraphemeConflicts.Find(Standard(("ء", "ъ"))));

            Assert.Equal("ذ", conflict.Letter);
            Assert.Equal(new[] { "ز", "ء" }, conflict.Parts);
        }

        [Fact]
        public void SilentGraphemes_DoNotConflict() =>
            // Пустая графема — буква осознанно не пишется, и спорить ей не с чем.
            Assert.DoesNotContain(GraphemeConflicts.Find(Standard(("ع", ""), ("ق", ""))),
                                  c => c.Involves("ع") || c.Involves("ق"));

        [Fact]
        public void VariantsAndVowels_AreNotCompared() =>
            // «р» у ر|heavy — та же буква в другом положении, а огласовку буквой не спутать.
            Assert.Empty(GraphemeConflicts.Find(new Dictionary<string, string>
            {
                ["ر"] = "р",
                ["ر|heavy"] = "р",
                ["َ"] = "р",
                ["ل"] = "л",
            }));

        [Fact]
        public void Latin_HasNoConflicts() =>
            Assert.Empty(GraphemeConflicts.Find(TestProfiles.Latin.Rules));

        [Theory]
        [InlineData("ق", "قُلْ")]
        [InlineData("ء", "أَحَدٌ")]   // хамза на носителе
        public void ExampleWord_IsTheShortestWordWithTheLetter(string letter, string expected)
        {
            var texts = new[] { "قُلْ هُوَ ٱللَّهُ أَحَدٌ", "ٱللَّهُ ٱلصَّمَدُ" };

            Assert.Equal(expected, GraphemeConflicts.ExampleWord(texts, letter));
        }

        [Fact]
        public void ExampleWord_IsNullWithoutTheLetter() =>
            Assert.Null(GraphemeConflicts.ExampleWord(new[] { "ٱللَّهُ ٱلصَّمَدُ" }, "ظ"));
    }
}
