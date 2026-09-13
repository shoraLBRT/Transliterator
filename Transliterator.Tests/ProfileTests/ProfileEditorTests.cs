using Microsoft.Extensions.Logging.Abstractions;
using Transliterator.Core.Repositories;
using Transliterator.Core.Services;
using Transliterator.Domain.Exceptions;
using Xunit;

namespace Transliterator.Tests.ProfileTests
{
    /// <summary>
    /// Редактор профилей (E2): копия, переименование, удаление, правка графемы
    /// и проверка имени. «Перезагрузка страницы» — новое хранилище над тем же словарём.
    /// </summary>
    public class ProfileEditorTests
    {
        private readonly MemoryStore _store = new();

        private UserProfileRepository Repository() =>
            new(new EmbeddedProfileRepository(NullLogger<EmbeddedProfileRepository>.Instance),
                _store, NullLogger<UserProfileRepository>.Instance);

        private ProfileEditor Editor(UserProfileRepository repository) => new(repository);

        [Fact]
        public async Task NewProfile_IsACopyOfAnExistingOne()
        {
            var repository = Repository();

            var copy = await Editor(repository).CreateCopyAsync("Standard", "Мой");

            Assert.Equal("Мой", copy.Name);
            Assert.Equal(TestProfiles.Standard.Rules, copy.Rules);
            Assert.False(await repository.IsBuiltInAsync("Мой"));
            Assert.Equal(TestProfiles.Standard.Rules, (await Repository().GetProfileAsync("Мой"))!.Rules);
        }

        [Fact]
        public async Task CopyOfACopy_IsAlsoAllowed()
        {
            var editor = Editor(Repository());
            await editor.CreateCopyAsync("Latin", "Мой");

            var second = await editor.CreateCopyAsync("Мой", "Мой второй");

            Assert.Equal(TestProfiles.All.Single(p => p.Name == "Latin").Rules, second.Rules);
        }

        [Fact]
        public async Task NameIsTrimmed()
        {
            var copy = await Editor(Repository()).CreateCopyAsync("Standard", "  Мой  ");

            Assert.Equal("Мой", copy.Name);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("Standard")]
        [InlineData("standard")]
        [InlineData("МОЙ")]
        public async Task EmptyOrTakenName_IsRefused_AndNothingIsSaved(string name)
        {
            var editor = Editor(Repository());
            await editor.CreateCopyAsync("Standard", "Мой");
            var before = _store.Items.Count;

            var error = await Assert.ThrowsAsync<TransliterationException>(() => editor.CreateCopyAsync("Standard", name));

            Assert.Equal(await editor.CheckNameAsync(name), error.Message);
            Assert.Equal(before, _store.Items.Count);
        }

        [Fact]
        public async Task CheckName_ExplainsWhatIsWrong()
        {
            var editor = Editor(Repository());
            await editor.CreateCopyAsync("Standard", "Мой");

            Assert.Null(await editor.CheckNameAsync("Новый"));
            Assert.Equal("Имя профиля не может быть пустым.", await editor.CheckNameAsync(" "));
            Assert.Equal("Профиль «Standard» уже есть.", await editor.CheckNameAsync("STANDARD"));
            Assert.Equal("Имя не изменилось.", await editor.CheckNameAsync("Мой", renaming: "Мой"));
            Assert.Null(await editor.CheckNameAsync("мой", renaming: "Мой"));
        }

        [Fact]
        public async Task Rename_MovesTheProfile_WithItsRules()
        {
            var repository = Repository();
            var editor = Editor(repository);
            var profile = await editor.CreateCopyAsync("Standard", "Мой");
            await editor.SetRuleAsync(profile, "ب", "b");

            var renamed = await editor.RenameAsync("Мой", "Другой");

            Assert.Equal("Другой", renamed.Name);
            Assert.Null(await Repository().GetProfileAsync("Мой"));
            Assert.Equal("b", (await Repository().GetProfileAsync("Другой"))!.Rules["ب"]);
            Assert.Single(_store.Items);
        }

        [Fact]
        public async Task Rename_ToAnotherCase_IsAllowed()
        {
            var editor = Editor(Repository());
            await editor.CreateCopyAsync("Standard", "мой");

            await editor.RenameAsync("мой", "Мой");

            Assert.NotNull(await Repository().GetProfileAsync("Мой"));
            Assert.Null(await Repository().GetProfileAsync("мой"));
        }

        [Fact]
        public async Task Rename_ToATakenName_LeavesTheProfileAlone()
        {
            var editor = Editor(Repository());
            await editor.CreateCopyAsync("Standard", "Мой");
            await editor.CreateCopyAsync("Standard", "Другой");

            await Assert.ThrowsAsync<TransliterationException>(() => editor.RenameAsync("Мой", "Другой"));

            Assert.NotNull(await Repository().GetProfileAsync("Мой"));
            Assert.Equal(2, _store.Items.Count);
        }

        [Fact]
        public async Task Rename_WhenTheWriteFails_KeepsTheOldProfile()
        {
            var repository = Repository();
            var editor = Editor(repository);
            await editor.CreateCopyAsync("Standard", "Мой");
            _store.Full = true;

            await Assert.ThrowsAsync<TransliterationException>(() => editor.RenameAsync("Мой", "Другой"));

            Assert.NotNull(await Repository().GetProfileAsync("Мой"));
        }

        [Fact]
        public async Task BuiltInProfile_CannotBeRenamedDeletedOrEdited()
        {
            var repository = Repository();
            var editor = Editor(repository);
            var standard = (await repository.GetProfileAsync("Standard"))!;

            await Assert.ThrowsAsync<TransliterationException>(() => editor.RenameAsync("Standard", "Мой"));
            await Assert.ThrowsAsync<TransliterationException>(() => editor.DeleteAsync("Standard"));
            await Assert.ThrowsAsync<TransliterationException>(() => editor.SetRuleAsync(standard, "ب", "b"));

            Assert.Empty(_store.Items);
            Assert.Equal("б", (await repository.GetProfileAsync("Standard"))!.Rules["ب"]);
        }

        [Fact]
        public async Task Delete_RemovesTheUserProfile()
        {
            var editor = Editor(Repository());
            await editor.CreateCopyAsync("Standard", "Мой");

            await editor.DeleteAsync("Мой");

            Assert.Null(await Repository().GetProfileAsync("Мой"));
            Assert.Empty(_store.Items);
        }

        [Fact]
        public async Task SetRule_IsSeenOnTheTextAtOnce_AndSurvivesAReload()
        {
            var editor = Editor(Repository());
            var profile = await editor.CreateCopyAsync("Standard", "Мой");

            await editor.SetRuleAsync(profile, "ب", "b");

            Assert.StartsWith("b", TransliterationPipeline.Transliterate("بِسْمِ", profile));
            Assert.Equal("b", (await Repository().GetProfileAsync("Мой"))!.Rules["ب"]);
        }

        [Fact]
        public async Task SetRule_ToEmpty_IsAnEmptyGrapheme_NotARemovedKey()
        {
            var editor = Editor(Repository());
            var profile = await editor.CreateCopyAsync("Standard", "Мой");

            await editor.SetRuleAsync(profile, "ب", null);

            var reloaded = (await Repository().GetProfileAsync("Мой"))!;
            Assert.True(reloaded.Rules.ContainsKey("ب"));
            Assert.Equal(string.Empty, reloaded.Rules["ب"]);
        }

        [Fact]
        public async Task SetRule_WhenTheWriteFails_ShowsTheEdit_ButKeepsTheSavedVersion()
        {
            var editor = Editor(Repository());
            var profile = await editor.CreateCopyAsync("Standard", "Мой");
            _store.Full = true;

            await Assert.ThrowsAsync<TransliterationException>(() => editor.SetRuleAsync(profile, "ب", "b"));

            Assert.Equal("b", profile.Rules["ب"]);
            Assert.Equal("б", (await Repository().GetProfileAsync("Мой"))!.Rules["ب"]);
        }

        [Fact]
        public async Task SuggestedCopyName_IsFree()
        {
            var editor = Editor(Repository());
            await editor.CreateCopyAsync("Standard", "Standard (копия)");

            var suggested = await editor.SuggestCopyNameAsync("Standard");

            Assert.Equal("Standard (копия 2)", suggested);
            Assert.Null(await editor.CheckNameAsync(suggested));
        }
    }
}
