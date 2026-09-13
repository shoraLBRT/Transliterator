using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Reflection;
using Transliterator.Core.Models;
using Transliterator.Core.Repositories;
using Transliterator.Domain.Entities;
using Transliterator.Domain.Exceptions;
using Xunit;

namespace Transliterator.Tests.CorpusTests
{
    /// <summary>
    /// Корпус, который не касается файловой системы: его прочитает браузер.
    /// Главная проверка здесь — что вшитый корпус это тот же корпус, что лежит
    /// рядом со сборкой и кормит прогон A4: панель сур на странице и случаи
    /// в тестах должны строиться из одного и того же.
    /// </summary>
    public class EmbeddedCorpusRepositoryTests
    {
        private static EmbeddedCorpusRepository Builtin() =>
            new(NullLogger<EmbeddedCorpusRepository>.Instance);

        /// <summary>Хранилище поверх заведомо испорченных ресурсов сборки тестов.</summary>
        private static EmbeddedCorpusRepository Over(string folder) =>
            new(Assembly.GetExecutingAssembly(),
                $"Transliterator.Tests.Resources.{folder}.",
                NullLogger<EmbeddedCorpusRepository>.Instance);

        private static Task<IReadOnlyList<CorpusSurah>> OnDisk() =>
            new JsonCorpusRepository(
                    Options.Create(new StorageSettings { CorpusPath = Path.Combine(AppContext.BaseDirectory, "Corpus") }),
                    NullLogger<JsonCorpusRepository>.Instance)
                .GetAllSurahsAsync();

        [Fact]
        public async Task EmbeddedCorpus_IsTheCorpusNextToTheAssembly()
        {
            // Один и тот же файл включён в csproj ядра и как Content, и как
            // EmbeddedResource. Сура, вшитая, но не скопированная (или наоборот),
            // появилась бы на странице и не попала бы под тесты.
            var embedded = await Builtin().GetAllSurahsAsync();
            var onDisk = await OnDisk();

            Assert.NotEmpty(embedded);
            Assert.Equal(onDisk.Select(s => s.Number), embedded.Select(s => s.Number));

            foreach (var (disk, resource) in onDisk.Zip(embedded))
            {
                Assert.Equal(disk.ArabicName, resource.ArabicName);
                Assert.Equal(disk.RussianName, resource.RussianName);
                Assert.Equal(disk.TextEdition, resource.TextEdition);
                Assert.Equal(disk.Ayahs.Select(a => (a.Number, a.Arabic, a.ArabicImlai, a.Expected)),
                             resource.Ayahs.Select(a => (a.Number, a.Arabic, a.ArabicImlai, a.Expected)));
            }
        }

        [Fact]
        public async Task SurahsComeInOrder_IncludingTheOneWithLeadingZeros()
        {
            // 001.json — имя ресурса, начинающееся с цифр. Компилятор мог бы
            // переписать его в идентификатор, и сура 1 молча пропала бы.
            var numbers = (await Builtin().GetAllSurahsAsync()).Select(s => s.Number).ToList();

            Assert.Equal(1, numbers[0]);
            Assert.Equal(numbers.Order(), numbers);
        }

        [Fact]
        public async Task GetSurah_FindsItByNumber()
        {
            var surah = await Builtin().GetSurahAsync(1);

            Assert.NotNull(surah);
            Assert.Equal(1, surah!.Number);
            Assert.NotEmpty(surah.Ayahs);
        }

        [Fact]
        public async Task MissingSurah_IsNull_NotAnError()
        {
            Assert.Null(await Builtin().GetSurahAsync(2));
        }

        [Fact]
        public async Task EveryReader_GetsItsOwnCopy()
        {
            // Сура изменяема. Файловое хранилище каждому читателю отдаёт свою —
            // и вшитое не должно вести себя иначе: правка суры одной панелью
            // не должна доезжать до другой.
            var repository = Builtin();
            var first = await repository.GetSurahAsync(1);

            first!.Ayahs.Clear();

            Assert.NotEmpty((await repository.GetSurahAsync(1))!.Ayahs);
        }

        [Fact]
        public async Task NoMatchingResources_IsEmptyCorpus_NotAFailure()
        {
            var repository = Over("NoSuchFolder");

            Assert.Empty(await repository.GetAllSurahsAsync());
            Assert.Null(await repository.GetSurahAsync(110));
        }

        [Theory]
        [InlineData("NotJsonCorpus", "not valid JSON")]
        [InlineData("NullCorpus", "is empty")]
        // Проверка схемы та же, что у файлов (CorpusLoaderTests): здесь достаточно
        // убедиться, что ресурс её проходит, а не обходит.
        [InlineData("MisnumberedCorpus", "does not match surah number")]
        public async Task BadResource_FailsLoudly(string folder, string expected)
        {
            var error = await Assert.ThrowsAsync<TransliterationException>(() => Over(folder).GetAllSurahsAsync());

            Assert.Contains(expected, error.Message);
        }
    }
}
