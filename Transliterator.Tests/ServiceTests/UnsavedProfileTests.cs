using Transliterator.Core.Services.Phonology;
using Transliterator.Core.Services.Rules;
using Transliterator.Domain.Entities;
using Transliterator.Domain.Exceptions;
using Transliterator.Domain.Interfaces;
using Transliterator.Tests.CorpusTests;
using Xunit;

namespace Transliterator.Tests.ServiceTests
{
    /// <summary>
    /// Транслитерация профилем, переданным целиком (C2). Хранилище здесь падает
    /// на любом обращении: весь смысл пути в том, что профиль в хранилище
    /// не лежит, и молча сходить туда за «Standard» было бы ошибкой, а не удобством.
    /// </summary>
    public class UnsavedProfileTests
    {
        private readonly ITransliterationService _service = new TransliterationService(
            new NoStorage(), new ArabicNormalizer(), new ArabicParser(),
            new RulesService(new WaqfRule(), new WaslRule(), new ArticleRule(),
                             new AssimilationRule(), new NasalRule(),
                             new EmphasisRule(), new MaddRule(), new QalqalahRule()),
            new CyrillicRenderer());

        private sealed class NoStorage : IProfileRepository
        {
            private static Exception Touched() => new InvalidOperationException("Хранилище трогать нельзя");

            public Task<TransliterationProfile?> GetProfileAsync(string profileName) => throw Touched();
            public Task<IEnumerable<TransliterationProfile>> GetAllProfilesAsync() => throw Touched();
            public Task SaveProfileAsync(TransliterationProfile profile) => throw Touched();
            public Task DeleteProfileAsync(string profileName) => throw Touched();
            public Task<bool> ProfileExistsAsync(string profileName) => throw Touched();
        }

        /// <summary>Копия Standard: общий профиль из ресурсов тесты не правят.</summary>
        private static TransliterationProfile Copy(string name, Func<KeyValuePair<string, string>, bool>? keep = null) =>
            new(name, "профиль пользователя")
            {
                Rules = TestProfiles.Standard.Rules
                    .Where(rule => keep?.Invoke(rule) ?? true)
                    .ToDictionary(rule => rule.Key, rule => rule.Value)
            };

        private static TransliterationProfile Without(params string[] keys) =>
            Copy("Draft", rule => !keys.Contains(rule.Key));

        [Fact]
        public void Transliterate_UsesTheGivenProfile()
        {
            var profile = Copy("Мой профиль");
            profile.Rules["ب"] = "b";

            var result = _service.Transliterate("بِسْمِ ٱللَّهِ", profile);

            Assert.StartsWith("b", result.TransliteratedText);
            Assert.Equal(TransliterationPipeline.Transliterate("بِسْمِ ٱللَّهِ", profile), result.TransliteratedText);
            Assert.Equal("Мой профиль", result.ProfileName);
            Assert.Equal("بِسْمِ ٱللَّهِ", result.OriginalText);
        }

        [Fact]
        public void Transliterate_NullProfile_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => _service.Transliterate("بِسْمِ", null!));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        public void Transliterate_ProfileWithoutName_Throws(string? name)
        {
            var profile = Copy("Draft");
            profile.Name = name!;

            var error = Assert.Throws<TransliterationException>(() => _service.Transliterate("بِسْمِ", profile));

            Assert.Contains("name", error.Message);
        }

        [Fact]
        public void Transliterate_ProfileWithoutRules_Throws()
        {
            var empty = new TransliterationProfile("Draft", "");
            var nullRules = new TransliterationProfile("Draft", "") { Rules = null! };

            Assert.Throws<TransliterationException>(() => _service.Transliterate("بِسْمِ", empty));
            Assert.Throws<TransliterationException>(() => _service.Transliterate("بِسْمِ", nullRules));
        }

        [Fact]
        public void Transliterate_RuleWithEmptyKey_Throws()
        {
            var profile = Copy("Draft");
            profile.Rules[" "] = "б";

            Assert.Throws<TransliterationException>(() => _service.Transliterate("بِسْمِ", profile));
        }

        [Fact]
        public void Transliterate_RuleWithNullValue_ThrowsAndNamesTheKey()
        {
            // null приходит из JSON ("ب": null). Пустая строка — законная немая
            // графема, а null — незаполненное поле, и путать их нельзя.
            var profile = Copy("Draft");
            profile.Rules["ب"] = null!;

            var error = Assert.Throws<TransliterationException>(() => _service.Transliterate("بِسْمِ", profile));

            Assert.Contains("U+0628", error.Message);
        }

        [Fact]
        public void Transliterate_ChecksTheProfile_EvenForEmptyText()
        {
            // Редактор профиля пересчитывает вывод на каждую правку, и поле ввода
            // в этот момент бывает пустым. Сломанный профиль должен быть виден сразу.
            Assert.Throws<TransliterationException>(
                () => _service.Transliterate("", new TransliterationProfile("Draft", "")));
        }

        [Fact]
        public void Transliterate_EmptyText_GivesEmptyResult()
        {
            var result = _service.Transliterate("  ", Copy("Draft"));

            Assert.Equal(string.Empty, result.TransliteratedText);
            Assert.Equal("Draft", result.ProfileName);
        }

        [Fact]
        public void IncompleteProfile_WithoutVariants_FallsBackToBaseKeys()
        {
            // Вариант — уточнение базовой графемы, и профиль вправе его не задавать.
            // В وَإِيَّاكَ из вариантов участвует только разделитель хамзы: без него
            // она пишется базовой графемой, и ничего больше в выводе не меняется.
            var withoutVariants = Copy("Draft", rule => !rule.Key.Contains('|'));

            var full = _service.Transliterate("وَإِيَّاكَ", TestProfiles.Standard).TransliteratedText;
            var reduced = _service.Transliterate("وَإِيَّاكَ", withoutVariants).TransliteratedText;

            Assert.Contains("-", full);
            Assert.Equal(full.Replace("-", "ъ"), reduced);
        }

        [Fact]
        public void IncompleteProfile_WithoutAKeyTheTextDoesNotNeed_Works()
        {
            var result = _service.Transliterate("بِسْمِ ٱللَّهِ", Without("ظ", "ض", "ص"));

            Assert.Equal(TransliterationPipeline.Transliterate("بِسْمِ ٱللَّهِ"), result.TransliteratedText);
        }

        [Fact]
        public void IncompleteProfile_WithoutAKeyTheTextNeeds_ThrowsAndNamesIt()
        {
            // Без проверки буква молча пропадала: рендерер не нашёл ключ и записал
            // пустую строку, и «бисм» превращалось в «исм» без единого сообщения.
            var error = Assert.Throws<TransliterationException>(
                () => _service.Transliterate("بِسْمِ", Without("ب")));

            Assert.Contains("Draft", error.Message);
            Assert.Contains("U+0628", error.Message);
        }

        [Fact]
        public void IncompleteProfile_NamesEveryMissingKey_NotJustTheFirst()
        {
            // Огласовка — такой же базовый ключ, как буква. Перечислены оба:
            // иначе редактор чинил бы профиль по одному ключу за прогон.
            var error = Assert.Throws<TransliterationException>(
                () => _service.Transliterate("بِسْمِ", Without("ب", "ِ")));

            Assert.Contains("U+0628", error.Message);
            Assert.Contains("U+0650", error.Message);
        }

        [Fact]
        public void IncompleteProfile_VariantThatCoversTheCase_IsEnough()
        {
            // Недостающий ключ — тот, до которого дошёл поиск, а не любой базовый.
            // Начальную хамзу пишет "ء|initial", и до "ء" рендерер не доходит.
            var profile = Without("ء");

            var result = _service.Transliterate("أَعْبُدُ", profile);

            Assert.Equal(TransliterationPipeline.Transliterate("أَعْبُدُ"), result.TransliteratedText);
        }

        public static TheoryData<string> BuiltInProfiles()
        {
            var data = new TheoryData<string>();
            foreach (var profile in TestProfiles.All)
                data.Add(profile.Name);
            return data;
        }

        [Theory]
        [MemberData(nameof(BuiltInProfiles))]
        public void BuiltInProfile_LacksNoKeyTheCorpusNeeds(string name)
        {
            // Недостающий ключ теперь ошибка и на пути через хранилище. Встроенный
            // профиль, которому не хватает ключа на корпусном тексте, уронил бы
            // и страницу, и CLI — проверяется здесь, а не у пользователя.
            var profile = TestProfiles.All.Single(p => p.Name == name);

            Assert.All(CorpusCases.Ayahs.Select(row => (string)row[0]),
                id => _service.Transliterate(CorpusCases.Ayah(id).Arabic, profile));
        }
    }
}
