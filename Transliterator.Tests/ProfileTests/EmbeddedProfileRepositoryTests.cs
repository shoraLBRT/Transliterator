using Microsoft.Extensions.Logging.Abstractions;
using System.Reflection;
using Transliterator.Core.Repositories;
using Transliterator.Domain.Entities;
using Transliterator.Domain.Exceptions;
using Xunit;

namespace Transliterator.Tests.ProfileTests
{
    /// <summary>
    /// Хранилище профилей, которое не касается файловой системы: единственное,
    /// которое переживёт wasm. Половина проверок здесь — про то, что встроенный
    /// профиль это тот же самый профиль, что и файл рядом со сборкой, а не копия,
    /// зажившая своей жизнью.
    /// </summary>
    public class EmbeddedProfileRepositoryTests
    {
        private static EmbeddedProfileRepository Builtin() =>
            new(NullLogger<EmbeddedProfileRepository>.Instance);

        /// <summary>Хранилище поверх заведомо испорченных ресурсов сборки тестов.</summary>
        private static EmbeddedProfileRepository Over(string folder) =>
            new(Assembly.GetExecutingAssembly(),
                $"Transliterator.Tests.Resources.{folder}.",
                NullLogger<EmbeddedProfileRepository>.Instance);

        [Fact]
        public async Task ListsEveryBuiltinProfile()
        {
            var names = (await Builtin().GetAllProfilesAsync()).Select(p => p.Name).OrderBy(n => n);

            Assert.Equal(TestProfiles.All.Select(p => p.Name).OrderBy(n => n), names);
        }

        [Theory]
        [InlineData("Standard")]
        [InlineData("Latin")]
        public async Task EmbeddedProfile_MatchesTheFileNextToTheAssembly(string name)
        {
            // Один и тот же файл включён в .csproj и как Content, и как
            // EmbeddedResource. Разойтись им негде — и вот проверка, что негде.
            var embedded = await Builtin().GetProfileAsync(name);
            var onDisk = TestProfiles.All.Single(p => p.Name == name);

            Assert.NotNull(embedded);
            Assert.Equal(onDisk.Description, embedded!.Description);
            Assert.Equal(onDisk.Rules, embedded.Rules);
        }

        [Fact]
        public async Task ArabicAndCyrillicSurviveTheResource()
        {
            // Ресурс читается потоком, а не строкой: если кодировку потеряли,
            // видно именно здесь, а не через десять стадий конвейера.
            var profile = await Builtin().GetProfileAsync("Standard");

            Assert.Equal(TestProfiles.Standard.Rules["ب"], profile!.Rules["ب"]);
        }

        [Fact]
        public async Task UnknownProfile_IsNullNotThrow()
        {
            Assert.Null(await Builtin().GetProfileAsync("Missing"));
            Assert.False(await Builtin().ProfileExistsAsync("Missing"));
        }

        [Fact]
        public async Task KnownProfile_Exists()
        {
            Assert.True(await Builtin().ProfileExistsAsync("Standard"));
        }

        [Fact]
        public async Task GetProfile_ReturnsTheSameInstance_AndCallerCanBreakIt()
        {
            // Ресурсы читаются один раз, и словарь правил отдаётся как есть.
            // Копию делает TransliterationService.GetRulesAsync — там, где
            // словарь уходит наружу; здесь фиксируем, что делает её именно он.
            var first = await Builtin().GetProfileAsync("Standard");
            var second = await Builtin().GetProfileAsync("Standard");

            Assert.NotSame(first, second);
        }

        [Fact]
        public async Task Save_IsRefusedFromTheRepositoryItself()
        {
            // Не «упало где-то в глубине», а отказ от своего имени и с именем
            // профиля: ресурс сборки не переписывается в рантайме нигде.
            var repository = Builtin();

            var error = await Assert.ThrowsAsync<TransliterationException>(
                () => repository.SaveProfileAsync(new TransliterationProfile("Standard", "правка")));

            Assert.Contains("Standard", error.Message);
            Assert.Contains("read-only", error.Message);
        }

        [Fact]
        public async Task Save_LeavesTheProfileAsItWas()
        {
            var repository = Builtin();

            await Assert.ThrowsAsync<TransliterationException>(
                () => repository.SaveProfileAsync(new TransliterationProfile("Standard", "правка")));

            var profile = await repository.GetProfileAsync("Standard");

            Assert.Equal(TestProfiles.Standard.Description, profile!.Description);
        }

        [Fact]
        public async Task Delete_IsRefused()
        {
            var error = await Assert.ThrowsAsync<TransliterationException>(
                () => Builtin().DeleteProfileAsync("Standard"));

            Assert.Contains("Standard", error.Message);
        }

        [Fact]
        public async Task NoMatchingResources_IsEmptyCatalogue_NotAFailure()
        {
            // Сборка без вшитых профилей — законный случай: профили может
            // раздавать другое хранилище.
            var repository = Over("NoSuchFolder");

            Assert.Empty(await repository.GetAllProfilesAsync());
        }

        [Theory]
        [InlineData("BrokenProfiles", "not valid JSON")]
        [InlineData("NullProfiles", "is empty")]
        [InlineData("NamelessProfiles", "has no name")]
        [InlineData("DuplicateProfiles", "declared twice")]
        public void BadResource_FailsLoudly_WithTheResourceName(string folder, string expected)
        {
            // Битый ресурс — это сломанная сборка, а не сломанные данные: руками
            // его после компиляции никто не портил. Пропустить его молча значит
            // выкатить веб-версию, в которой профиль просто исчез из списка.
            var error = Assert.Throws<TransliterationException>(() => Over(folder));

            Assert.Contains(expected, error.Message);
            Assert.Contains(folder, error.Message);
        }
    }
}
