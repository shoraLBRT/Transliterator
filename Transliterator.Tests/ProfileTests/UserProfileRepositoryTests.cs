using Microsoft.Extensions.Logging.Abstractions;
using Transliterator.Core.Repositories;
using Transliterator.Core.Services.Phonology;
using Transliterator.Core.Services.Rules;
using Transliterator.Domain.Entities;
using Transliterator.Domain.Exceptions;
using Transliterator.Domain.Interfaces;
using Xunit;

namespace Transliterator.Tests.ProfileTests
{
    /// <summary>
    /// Профили пользователя поверх localStorage (E1). Хранилище здесь — словарь
    /// в памяти: порчу записи и переполнение квоты в нём можно устроить нарочно.
    /// «Перезагрузка страницы» — новое хранилище профилей над тем же словарём.
    /// </summary>
    public class UserProfileRepositoryTests
    {
        private readonly MemoryStore _store = new();

        private UserProfileRepository Repository() =>
            new(new EmbeddedProfileRepository(NullLogger<EmbeddedProfileRepository>.Instance),
                _store, NullLogger<UserProfileRepository>.Instance);

        private static TransliterationProfile Mine(string name = "Мой", string ba = "b") =>
            new(name, "свой профиль")
            {
                Rules = new Dictionary<string, string>(TestProfiles.Cyrillic.Rules) { ["ب"] = ba }
            };

        private static async Task<List<string>> Names(UserProfileRepository repository) =>
            (await repository.GetAllProfilesAsync()).Select(p => p.Name).ToList();

        [Fact]
        public async Task SavedProfile_SurvivesAReload()
        {
            await Repository().SaveProfileAsync(Mine());

            var reloaded = Repository();
            var profile = await reloaded.GetProfileAsync("Мой");

            Assert.NotNull(profile);
            Assert.Equal("b", profile!.Rules["ب"]);
            Assert.Equal("свой профиль", profile.Description);
            Assert.Contains("Мой", await Names(reloaded));
        }

        [Fact]
        public async Task StoredJson_KeepsArabicAndCyrillicReadable()
        {
            // Профиль в localStorage смотрят и правят руками, как файл профиля.
            await Repository().SaveProfileAsync(Mine());

            var json = _store.Items[UserProfileRepository.KeyFor("Мой")];

            Assert.Contains("\"ب\"", json);
            Assert.Contains("свой профиль", json);
        }

        [Fact]
        public async Task BuiltInAndUserProfiles_AreListedTogether_AndDistinguishable()
        {
            var repository = Repository();
            await repository.SaveProfileAsync(Mine());

            var names = await Names(repository);

            Assert.Equal(new[] { "Cyrillic", "Latin" }, names.Take(2).OrderBy(n => n, StringComparer.Ordinal));
            Assert.Equal("Мой", names[2]);
            Assert.True(await repository.IsBuiltInAsync("Cyrillic"));
            Assert.False(await repository.IsBuiltInAsync("Мой"));
        }

        [Fact]
        public async Task SavingABuiltInProfile_CreatesACopy_AndLeavesTheBuiltInAlone()
        {
            var repository = Repository();
            var edited = await repository.GetProfileAsync("Cyrillic");
            edited!.Rules["ب"] = "b";

            var first = await repository.SaveAsync(edited);
            var second = await repository.SaveAsync(edited);

            Assert.Equal("Cyrillic (копия)", first);
            Assert.Equal("Cyrillic (копия 2)", second);
            Assert.Equal("б", (await repository.GetProfileAsync("Cyrillic"))!.Rules["ب"]);
            Assert.Equal("b", (await repository.GetProfileAsync(first))!.Rules["ب"]);
            Assert.False(await repository.IsBuiltInAsync(first));
        }

        [Fact]
        public async Task EditingAReturnedProfile_ChangesNothingUntilSaved()
        {
            var repository = Repository();

            (await repository.GetProfileAsync("Cyrillic"))!.Rules["ب"] = "b";

            Assert.Equal("б", (await repository.GetProfileAsync("Cyrillic"))!.Rules["ب"]);
        }

        [Fact]
        public async Task UpdateRule_OnABuiltInProfile_GoesToACopy()
        {
            var repository = Repository();
            var service = new TransliterationService(
                repository, new ArabicNormalizer(), new ArabicParser(),
                new RulesService(new WaqfRule(), new WaslRule(), new ArticleRule(),
                                 new AssimilationRule(), new NasalRule(),
                                 new EmphasisRule(), new MaddRule(), new QalqalahRule()),
                new CyrillicRenderer());

            await service.UpdateRuleAsync("ب", "b", "Cyrillic");

            Assert.Equal("б", (await repository.GetProfileAsync("Cyrillic"))!.Rules["ب"]);
            Assert.Equal("b", (await repository.GetProfileAsync("Cyrillic (копия)"))!.Rules["ب"]);
        }

        [Fact]
        public async Task DeletingABuiltInProfile_IsRefused()
        {
            var error = await Assert.ThrowsAsync<TransliterationException>(
                () => Repository().DeleteProfileAsync("Cyrillic"));

            Assert.Contains("Cyrillic", error.Message);
        }

        [Fact]
        public async Task DeletingAUserProfile_RemovesIt()
        {
            var repository = Repository();
            await repository.SaveProfileAsync(Mine());

            await repository.DeleteProfileAsync("Мой");

            Assert.Null(await repository.GetProfileAsync("Мой"));
            Assert.Empty(_store.Items);
        }

        [Theory]
        [InlineData("{ этой скобки не хватало")]
        [InlineData("null")]
        [InlineData("{\"Name\": \"Другой\", \"Description\": \"\", \"Rules\": {\"ب\": \"б\"}}")]
        [InlineData("{\"Name\": \"Битый\", \"Description\": \"\", \"Rules\": null}")]
        public async Task CorruptEntry_IsSkipped_WithAMessage_AndTheRestIsListed(string json)
        {
            var repository = Repository();
            await repository.SaveProfileAsync(Mine());
            _store.Items[UserProfileRepository.KeyFor("Битый")] = json;

            var names = await Names(repository);

            Assert.Contains("Мой", names);
            Assert.DoesNotContain("Битый", names);
            Assert.DoesNotContain("Другой", names);
            Assert.Contains("Битый", Assert.Single(repository.Problems));
            Assert.Null(await repository.GetProfileAsync("Битый"));
        }

        private void StoreByHand(TransliterationProfile profile) =>
            _store.Items[UserProfileRepository.KeyFor(profile.Name)] =
                System.Text.Json.JsonSerializer.Serialize(profile);

        [Fact]
        public async Task StoredEntryNamedLikeABuiltIn_MovesToACopyName_AndTheBuiltInStays()
        {
            // E8: до переименования Standard → Cyrillic свой профиль мог называться «Cyrillic».
            StoreByHand(Mine("Cyrillic"));

            var repository = Repository();
            var names = await Names(repository);

            Assert.Single(names, "Cyrillic");
            Assert.Contains("Cyrillic (копия)", names);
            Assert.Empty(repository.Problems);
            Assert.True(await repository.IsBuiltInAsync("Cyrillic"));
            Assert.Equal("б", (await repository.GetProfileAsync("Cyrillic"))!.Rules["ب"]);
            Assert.Equal("b", (await repository.GetProfileAsync("Cyrillic (копия)"))!.Rules["ب"]);
            Assert.Equal("свой профиль", (await repository.GetProfileAsync("Cyrillic (копия)"))!.Description);

            // Переезд записан в хранилище: после перезагрузки старой записи нет, новая на месте.
            Assert.False(_store.Items.ContainsKey(UserProfileRepository.KeyFor("Cyrillic")));
            Assert.Equal(names, await Names(Repository()));
        }

        [Fact]
        public async Task StoredEntryNamedLikeABuiltIn_TakesTheFirstFreeCopyName()
        {
            StoreByHand(Mine("Cyrillic (копия)", ba: "bb"));
            StoreByHand(Mine("Cyrillic"));

            var repository = Repository();
            var moved = await repository.MoveShadowedAsync();

            Assert.Equal("Cyrillic (копия 2)", Assert.Single(moved).Value);
            Assert.Equal("Cyrillic", Assert.Single(moved).Key);
            Assert.Equal("bb", (await repository.GetProfileAsync("Cyrillic (копия)"))!.Rules["ب"]);
            Assert.Equal("b", (await repository.GetProfileAsync("Cyrillic (копия 2)"))!.Rules["ب"]);
        }

        [Fact]
        public async Task MoveShadowed_RemembersMovesMadeWhileListing()
        {
            // Список профилей мог прочитаться раньше, чем страница поправила выбор:
            // переезд, сделанный при чтении списка, всё равно виден.
            StoreByHand(Mine("Cyrillic"));
            var repository = Repository();
            await Names(repository);

            var moved = await repository.MoveShadowedAsync();

            Assert.Equal("Cyrillic (копия)", moved["Cyrillic"]);
        }

        [Fact]
        public async Task StoredEntryNamedLikeABuiltIn_StaysInStorage_WhenItCannotMove()
        {
            StoreByHand(Mine("Cyrillic"));
            _store.Full = true;

            var repository = Repository();
            var names = await Names(repository);

            Assert.Single(names, "Cyrillic");
            Assert.Contains("Cyrillic", Assert.Single(repository.Problems));
            Assert.True(_store.Items.ContainsKey(UserProfileRepository.KeyFor("Cyrillic")));
            Assert.Empty(await repository.MoveShadowedAsync());
        }

        [Fact]
        public async Task MoveShadowed_LeavesOrdinaryProfilesAlone()
        {
            await Repository().SaveProfileAsync(Mine());

            var repository = Repository();

            Assert.Empty(await repository.MoveShadowedAsync());
            Assert.Contains("Мой", await Names(repository));
        }

        [Fact]
        public async Task ProblemsAreForgotten_OnceTheEntryIsGone()
        {
            var repository = Repository();
            _store.Items[UserProfileRepository.KeyFor("Битый")] = "{";
            await Names(repository);

            _store.Items.Remove(UserProfileRepository.KeyFor("Битый"));
            await Names(repository);

            Assert.Empty(repository.Problems);
        }

        [Fact]
        public async Task QuotaExceeded_IsAMessage_AndThePreviousVersionIsKept()
        {
            var repository = Repository();
            await repository.SaveProfileAsync(Mine(ba: "b"));
            _store.Full = true;

            var error = await Assert.ThrowsAsync<TransliterationException>(
                () => repository.SaveProfileAsync(Mine(ba: "bb")));

            Assert.Contains("хранилище браузера заполнено", error.Message);
            Assert.IsType<StorageQuotaExceededException>(error.InnerException);
            Assert.Equal("b", (await repository.GetProfileAsync("Мой"))!.Rules["ب"]);
        }

        [Fact]
        public async Task UnavailableStorage_StillListsTheBuiltIns()
        {
            _store.Unavailable = true;
            var repository = Repository();

            var names = await Names(repository);

            Assert.Equal(new[] { "Cyrillic", "Latin" }, names.OrderBy(n => n, StringComparer.Ordinal));
            Assert.NotEmpty(repository.Problems);
            Assert.NotNull(await repository.GetProfileAsync("Cyrillic"));
            await Assert.ThrowsAsync<TransliterationException>(() => repository.SaveProfileAsync(Mine()));
        }

        [Fact]
        public async Task ForeignKeys_AreNotProfiles()
        {
            // Выбор профиля (D2) лежит в том же localStorage, но профилем не является.
            _store.Items["transliterator.selected-profile"] = "Latin";
            _store.Items["чужой ключ"] = "{";

            var repository = Repository();

            Assert.Equal(2, (await Names(repository)).Count);
            Assert.Empty(repository.Problems);
        }

        [Theory]
        [InlineData("")]
        [InlineData("  ")]
        public async Task EmptyName_IsRefused(string name)
        {
            await Assert.ThrowsAsync<TransliterationException>(() => Repository().SaveProfileAsync(Mine(name)));
            Assert.Empty(_store.Items);
        }
    }
}
