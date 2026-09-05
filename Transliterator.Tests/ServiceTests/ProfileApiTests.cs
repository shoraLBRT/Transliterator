using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Transliterator.Core.Models;
using Transliterator.Core.Repositories;
using Transliterator.Core.Services.Phonology;
using Transliterator.Core.Services.Rules;
using Transliterator.Domain.Entities;
using Transliterator.Domain.Exceptions;
using Xunit;

namespace Transliterator.Tests.ServiceTests
{
    /// <summary>
    /// Работа с профилями снаружи конвейера: перечислить, прочитать, поправить.
    /// Хранилище — настоящее, во временной папке: половина проверок здесь именно
    /// про то, что доезжает до диска и обратно.
    /// </summary>
    public class ProfileApiTests : IDisposable
    {
        private readonly string _storagePath;
        private readonly JsonProfileRepository _repository;
        private readonly TransliterationService _service;

        public ProfileApiTests()
        {
            _storagePath = Path.Combine(Path.GetTempPath(), "translit-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_storagePath);

            WriteProfile(TestProfiles.Standard);

            _repository = new JsonProfileRepository(
                Options.Create(new StorageSettings { ProfilesPath = _storagePath }),
                NullLogger<JsonProfileRepository>.Instance);

            _service = new TransliterationService(
                _repository, new ArabicNormalizer(), new ArabicParser(),
                new RulesService(new WaqfRule(), new WaslRule(), new ArticleRule(),
                                 new NasalRule(), new EmphasisRule(), new MaddRule(),
                                 new QalqalahRule()),
                new CyrillicRenderer());
        }

        private void WriteProfile(TransliterationProfile profile) =>
            File.WriteAllText(
                Path.Combine(_storagePath, $"{profile.Name}.json"),
                JsonSerializer.Serialize(profile, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                }));

        public void Dispose()
        {
            if (Directory.Exists(_storagePath))
                Directory.Delete(_storagePath, recursive: true);
        }

        [Fact]
        public async Task GetAvailableProfiles_ListsEveryProfileInStorage()
        {
            // Профиль ищется по имени без расширения. Пока имя брали вместе с ".json",
            // хранилище дописывало второе и не находило ни одного профиля.
            WriteProfile(new TransliterationProfile("Draft", "второй профиль"));

            var names = await _service.GetAvailableProfilesAsync();

            Assert.Equal(new[] { "Draft", "Standard" }, names);
        }

        [Fact]
        public async Task GetRules_DefaultsToStandard()
        {
            var rules = await _service.GetRulesAsync();

            Assert.Equal(TestProfiles.Standard.Rules["ب"], rules["ب"]);
        }

        [Fact]
        public async Task GetRules_ReturnsCopy_NotTheCachedProfile()
        {
            // Репозиторий отдаёт профили из кеша. Отдай мы сам словарь — правка
            // у вызывающего меняла бы профиль всем остальным.
            var rules = await _service.GetRulesAsync();
            rules["ب"] = "ЗАМЕНА";

            var again = await _service.GetRulesAsync();

            Assert.NotEqual("ЗАМЕНА", again["ب"]);
        }

        [Fact]
        public async Task GetRules_UnknownProfile_Throws()
        {
            await Assert.ThrowsAsync<TransliterationException>(() => _service.GetRulesAsync("Missing"));
        }

        [Fact]
        public async Task UpdateRule_ChangesTransliteration()
        {
            await _service.UpdateRuleAsync("ب", "b");

            var result = await _service.TransliterateAsync("بِسْمِ");

            Assert.StartsWith("b", result.TransliteratedText);
        }

        [Fact]
        public async Task UpdateRule_AcceptsVariantKeys()
        {
            // Ключ правила — не только буква: варианты задаёт профиль, и код
            // не вправе отбраковывать ключ по тому, что это не одна графема.
            await _service.UpdateRuleAsync("ب|qalqalah", "ъ");

            var rules = await _service.GetRulesAsync();

            Assert.Equal("ъ", rules["ب|qalqalah"]);
        }

        [Fact]
        public async Task UpdateRule_AcceptsEmptyMapping()
        {
            // Пустая графема — законная запись: так в Standard заданы отзвук
            // кальканя и начальная хамза.
            await _service.UpdateRuleAsync("ء|initial", string.Empty);

            Assert.Equal(string.Empty, (await _service.GetRulesAsync())["ء|initial"]);
        }

        [Fact]
        public async Task UpdateRule_EmptyKey_Throws()
        {
            await Assert.ThrowsAsync<TransliterationException>(() => _service.UpdateRuleAsync(" ", "б"));
        }

        [Fact]
        public async Task UpdateRule_UnknownProfile_Throws()
        {
            await Assert.ThrowsAsync<TransliterationException>(() => _service.UpdateRuleAsync("ب", "b", "Missing"));
        }

        [Fact]
        public async Task UpdateRule_KeepsProfileFileReadable()
        {
            // Профиль правят руками. Экранирование по умолчанию превратило бы
            // всю арабицу и кириллицу в "\u0631" при первой же записи.
            await _service.UpdateRuleAsync("ب", "б");

            var saved = File.ReadAllText(Path.Combine(_storagePath, "Standard.json"));

            Assert.Contains("\"ب\": \"б\"", saved);
            Assert.DoesNotContain("\\u", saved);
        }

        [Fact]
        public async Task UpdateRule_SurvivesRestart()
        {
            await _service.UpdateRuleAsync("ب", "b");

            var reopened = new JsonProfileRepository(
                Options.Create(new StorageSettings { ProfilesPath = _storagePath }),
                NullLogger<JsonProfileRepository>.Instance);

            var profile = await reopened.GetProfileAsync("Standard");

            Assert.Equal("b", profile!.Rules["ب"]);
        }
    }
}
