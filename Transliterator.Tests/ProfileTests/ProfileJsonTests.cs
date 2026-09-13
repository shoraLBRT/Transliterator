using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Transliterator.Core.Models;
using Transliterator.Core.Repositories;
using Transliterator.Core.Services;
using Transliterator.Domain.Entities;
using Transliterator.Domain.Exceptions;
using Xunit;

namespace Transliterator.Tests.ProfileTests
{
    /// <summary>
    /// Импорт и экспорт профиля (E3). Главная проверка — что выгруженный файл
    /// читает настоящее файловое хранилище, а не только сам импорт: иначе профиль
    /// из браузера не занести в репозиторий.
    /// </summary>
    public class ProfileJsonTests : IDisposable
    {
        private readonly string _directory =
            Path.Combine(Path.GetTempPath(), "translit-export-" + Guid.NewGuid().ToString("N"));

        private readonly MemoryStore _store = new();

        public ProfileJsonTests() => Directory.CreateDirectory(_directory);

        public void Dispose()
        {
            if (Directory.Exists(_directory))
                Directory.Delete(_directory, recursive: true);
        }

        private JsonProfileRepository FileStorage() =>
            new(Options.Create(new StorageSettings { ProfilesPath = _directory }),
                NullLogger<JsonProfileRepository>.Instance);

        private UserProfileRepository BrowserStorage() =>
            new(new EmbeddedProfileRepository(NullLogger<EmbeddedProfileRepository>.Instance),
                _store, NullLogger<UserProfileRepository>.Instance);

        private static TransliterationProfile Mine(string name = "Мой") =>
            new(name, "профиль из браузера")
            {
                Rules = new Dictionary<string, string>(TestProfiles.Standard.Rules) { ["ب"] = "b" }
            };

        private void SaveExport(TransliterationProfile profile) =>
            File.WriteAllText(Path.Combine(_directory, ProfileJson.FileNameFor(profile.Name)), ProfileJson.Export(profile));

        [Fact]
        public async Task Export_IsReadByTheFileStorage_WithoutEdits()
        {
            SaveExport(Mine());

            var profile = await FileStorage().GetProfileAsync("Мой");

            Assert.NotNull(profile);
            Assert.Equal("профиль из браузера", profile!.Description);
            Assert.Equal(Mine().Rules, profile.Rules);
        }

        [Fact]
        public async Task Export_OfANameTheFileSystemRejects_IsStillListed()
        {
            SaveExport(Mine("мой/новый: v2?"));

            var names = (await FileStorage().GetAllProfilesAsync()).Select(p => p.Name);

            Assert.Contains("мой/новый: v2?", names);
        }

        [Theory]
        [InlineData("Мой", "Мой.json")]
        [InlineData("Standard (копия)", "Standard (копия).json")]
        [InlineData("мой/новый: v2?", "мой_новый_ v2_.json")]
        [InlineData("...", "profile.json")]
        public void FileName_FollowsTheProfileName(string name, string expected)
        {
            Assert.Equal(expected, ProfileJson.FileNameFor(name));
        }

        [Fact]
        public void Export_KeepsArabicAndCyrillicReadable()
        {
            var json = ProfileJson.Export(TestProfiles.Standard);

            Assert.Contains("\"ب\": \"б\"", json);
            Assert.DoesNotContain("\\u", json);
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
        public void BuiltInProfileFile_ImportsAsItIs(string name)
        {
            // Формат импорта — формат файлов в Resources/Profiles, а не новый.
            var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Profiles", name + ".json"));

            var result = ProfileJson.Import(json);

            Assert.Empty(result.Problems);
            Assert.Equal(TestProfiles.All.Single(p => p.Name == name).Rules, result.Profile!.Rules);
        }

        [Fact]
        public void ExportThenImport_GivesTheSameProfile()
        {
            var result = ProfileJson.Import(ProfileJson.Export(Mine()));

            Assert.Empty(result.Problems);
            Assert.Equal("Мой", result.Profile!.Name);
            Assert.Equal("профиль из браузера", result.Profile.Description);
            Assert.Equal(Mine().Rules, result.Profile.Rules);
        }

        [Theory]
        [InlineData("", "Файл пуст.")]
        [InlineData("{ этой скобки не хватало", "не читается как JSON: строка 1")]
        [InlineData("[]", "должен быть объектом JSON")]
        [InlineData("{\"Description\": \"\", \"Rules\": {\"ب\": \"б\"}}", "Нет поля «Name».")]
        [InlineData("{\"Name\": \"  \", \"Rules\": {\"ب\": \"б\"}}", "Имя профиля (Name) пустое.")]
        [InlineData("{\"Name\": 5, \"Rules\": {\"ب\": \"б\"}}", "Поле «Name» должно быть строкой.")]
        [InlineData("{\"Name\": \"X\", \"Description\": [], \"Rules\": {\"ب\": \"б\"}}", "Поле «Description» должно быть строкой.")]
        [InlineData("{\"Name\": \"X\"}", "Нет поля «Rules».")]
        [InlineData("{\"Name\": \"X\", \"Rules\": {}}", "В профиле нет ни одного правила.")]
        [InlineData("{\"Name\": \"X\", \"Rules\": []}", "должно быть объектом «ключ: графема»")]
        [InlineData("{\"Name\": \"X\", \"Rules\": {\"ب\": null}}", "нет графемы (null)")]
        [InlineData("{\"Name\": \"X\", \"Rules\": {\"ب\": 1}}", "должна быть строкой")]
        [InlineData("{\"Name\": \"X\", \"Rules\": {\" \": \"б\"}}", "Пустой ключ")]
        [InlineData("{\"Name\": \"X\", \"Rules\": {\"ب\": \"б\", \"ب\": \"b\"}}", "задан дважды")]
        [InlineData("{\"Name\": \"X\", \"Rules\": {\"ن\": \"н\", \"ن|ghuna\": \"н\"}}", "Неизвестный вариант «ghuna»")]
        [InlineData("{\"Name\": \"X\", \"Rules\": {\"ر|heavy\": \"р\"}}", "нет базового ключа")]
        [InlineData("{\"Name\": \"X\", \"rules\": {\"ب\": \"б\"}}", "Поле «rules» надо писать как «Rules»")]
        [InlineData("{\"Name\": \"X\", \"Rules\": {\"ب\": \"б\"}, \"Extra\": 1}", "Неизвестное поле «Extra».")]
        [InlineData("{\"Name\": \"X\", \"Rules\": {\"ب\": \"б\",}}", "не читается как JSON")]
        public void BrokenFile_IsExplained_NotThrown(string json, string expected)
        {
            var result = ProfileJson.Import(json);

            Assert.Null(result.Profile);
            Assert.Contains(result.Problems, problem => problem.Contains(expected));
        }

        [Fact]
        public void BrokenFile_ReportsEveryProblem_NotJustTheFirst()
        {
            var result = ProfileJson.Import("{\"name\": \"X\", \"Rules\": {\"ب\": null, \"ر|heavy\": \"р\"}}");

            Assert.Equal(4, result.Problems.Count);
        }

        [Fact]
        public void Import_TrimsTheName_AndDescriptionIsOptional()
        {
            var result = ProfileJson.Import("{\"Name\": \"  Мой  \", \"Rules\": {\"ب\": \"б\"}}");

            Assert.Equal("Мой", result.Profile!.Name);
            Assert.Equal(string.Empty, result.Profile.Description);
        }

        [Fact]
        public async Task ImportIntoTheBrowser_SavesTheProfile()
        {
            var storage = BrowserStorage();

            var result = await new ProfileEditor(storage).ImportAsync(ProfileJson.Export(Mine()));

            Assert.Null(result.RenamedFrom);
            Assert.Equal("b", (await BrowserStorage().GetProfileAsync("Мой"))!.Rules["ب"]);
        }

        [Fact]
        public async Task ImportOfABuiltInName_CannotOverwriteIt()
        {
            var storage = BrowserStorage();
            var fake = Mine("Standard");

            var result = await new ProfileEditor(storage).ImportAsync(ProfileJson.Export(fake));

            Assert.Equal("Standard", result.RenamedFrom);
            Assert.Equal("Standard (копия)", result.Profile!.Name);
            Assert.Equal("б", (await storage.GetProfileAsync("Standard"))!.Rules["ب"]);
            Assert.Equal("b", (await storage.GetProfileAsync("Standard (копия)"))!.Rules["ب"]);
        }

        [Fact]
        public async Task ImportOfATakenUserName_DoesNotOverwriteThatProfileEither()
        {
            var storage = BrowserStorage();
            var editor = new ProfileEditor(storage);
            await editor.CreateCopyAsync("Latin", "Мой");

            var result = await editor.ImportAsync(ProfileJson.Export(Mine("мой")));

            Assert.Equal("мой", result.RenamedFrom);
            Assert.Equal("мой (копия)", result.Profile!.Name);
            Assert.Equal(TestProfiles.All.Single(p => p.Name == "Latin").Rules["ب"],
                         (await storage.GetProfileAsync("Мой"))!.Rules["ب"]);
        }

        [Fact]
        public async Task ImportOfABrokenFile_SavesNothing()
        {
            var result = await new ProfileEditor(BrowserStorage()).ImportAsync("{\"Name\": \"X\"}");

            Assert.Null(result.Profile);
            Assert.NotEmpty(result.Problems);
            Assert.Empty(_store.Items);
        }

        [Fact]
        public async Task ImportWhenStorageIsFull_IsAnError_NotASilentLoss()
        {
            _store.Full = true;

            await Assert.ThrowsAsync<TransliterationException>(
                () => new ProfileEditor(BrowserStorage()).ImportAsync(ProfileJson.Export(Mine())));
        }
    }
}
