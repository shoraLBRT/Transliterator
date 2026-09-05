using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Transliterator.Core.Models;
using Transliterator.Core.Repositories;
using Transliterator.Domain.Entities;
using Transliterator.Domain.Exceptions;
using Xunit;

namespace Transliterator.Tests.CorpusTests
{
    /// <summary>
    /// Чтение корпуса примеров. Половина проверок здесь — про отказ читать битый
    /// файл: корпус кормит data-driven прогон, и пропущенная сура превращается
    /// не в красный тест, а в тихо исчезнувшие случаи.
    /// </summary>
    public class CorpusLoaderTests : IDisposable
    {
        private readonly string _corpusPath;

        public CorpusLoaderTests()
        {
            _corpusPath = Path.Combine(Path.GetTempPath(), "translit-corpus-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_corpusPath);
        }

        public void Dispose()
        {
            if (Directory.Exists(_corpusPath))
                Directory.Delete(_corpusPath, recursive: true);
        }

        private JsonCorpusRepository Repository(string? path = null) =>
            new(Options.Create(new StorageSettings { CorpusPath = path ?? _corpusPath }),
                NullLogger<JsonCorpusRepository>.Instance);

        /// <summary>Настоящий корпус из ресурсов — тот же, что уедет в вывод сборки.</summary>
        private JsonCorpusRepository Resources() =>
            Repository(Path.Combine(AppContext.BaseDirectory, "Corpus"));

        private void Write(string fileName, string json) =>
            File.WriteAllText(Path.Combine(_corpusPath, fileName), json);

        private const string Valid = """
            {
              "Number": 110,
              "ArabicName": "سُورَةُ ٱلنَّصْرِ",
              "RussianName": "Ан-Наср",
              "TextEdition": "uthmani-wasl",
              "Ayahs": [
                { "Number": 1, "Arabic": "إِذَا جَآءَ", "Expected": "изъаа джаааъа" },
                { "Number": 2, "Arabic": "كَانَ", "Expected": "каана" }
              ]
            }
            """;

        [Fact]
        public async Task ReadsEveryFieldOfTheSchema()
        {
            Write("110.json", Valid);

            var surah = await Repository().GetSurahAsync(110);

            Assert.NotNull(surah);
            Assert.Equal(110, surah!.Number);
            Assert.Equal("سُورَةُ ٱلنَّصْرِ", surah.ArabicName);
            Assert.Equal("Ан-Наср", surah.RussianName);
            Assert.Equal(CorpusTextEdition.UthmaniWasl, surah.TextEdition);
            Assert.Equal(2, surah.Ayahs.Count);
            Assert.Equal("كَانَ", surah.Ayahs[1].Arabic);
            Assert.Equal("каана", surah.Ayahs[1].Expected);
        }

        [Fact]
        public async Task MissingSurah_IsNull_NotAnError()
        {
            Assert.Null(await Repository().GetSurahAsync(114));
        }

        [Fact]
        public async Task GetAll_SortsByNumber_NotByFileName()
        {
            // Имя файла — номер тремя цифрами именно ради этого: "1.json" рядом
            // с "110.json" сортируется строкой как первый, а суры так не идут.
            Write("110.json", Valid);
            Write("001.json", Valid.Replace("\"Number\": 110", "\"Number\": 1"));

            var numbers = (await Repository().GetAllSurahsAsync()).Select(s => s.Number);

            Assert.Equal(new[] { 1, 110 }, numbers);
        }

        [Fact]
        public async Task MissingDirectory_IsEmpty_NotAnError()
        {
            var repository = Repository(Path.Combine(_corpusPath, "nothing-here"));

            Assert.Empty(await repository.GetAllSurahsAsync());
        }

        [Theory]
        // Редакция — не украшение: от неё зависит половина ожидаемых значений,
        // и текст в современной орфографии под этим ожиданием даст другой вывод.
        [InlineData("\"TextEdition\": \"uthmani-wasl\"", "\"TextEdition\": \"imlai\"")]
        [InlineData("\"TextEdition\": \"uthmani-wasl\"", "\"TextEdition\": \"\"")]
        // Номер — то, по чему сура ищется, и он же имя файла.
        [InlineData("\"Number\": 110,", "\"Number\": 0,")]
        [InlineData("\"Number\": 110,", "\"Number\": 109,")]
        // Пропущенный аят — дыра в покрытии, а не корпус поменьше.
        [InlineData("\"Number\": 2, \"Arabic\"", "\"Number\": 3, \"Arabic\"")]
        [InlineData("\"Expected\": \"каана\"", "\"Expected\": \"\"")]
        [InlineData("\"Arabic\": \"كَانَ\"", "\"Arabic\": \"\"")]
        [InlineData("\"RussianName\": \"Ан-Наср\"", "\"RussianName\": \" \"")]
        [InlineData("\"ArabicName\": \"سُورَةُ ٱلنَّصْرِ\"", "\"ArabicName\": \"\"")]
        public async Task BrokenFile_Throws_InsteadOfBeingSkipped(string original, string broken)
        {
            Write("110.json", Valid.Replace(original, broken));

            await Assert.ThrowsAsync<TransliterationException>(() => Repository().GetAllSurahsAsync());
        }

        [Fact]
        public async Task NotJson_Throws()
        {
            Write("110.json", "{ этой скобки не хватало");

            await Assert.ThrowsAsync<TransliterationException>(() => Repository().GetSurahAsync(110));
        }

        [Fact]
        public async Task Resources_AreCopiedToOutputAndRead()
        {
            // Корпус, забытый в csproj, до выходной папки не доезжает вовсе —
            // и прогон по нему остаётся зелёным, просто ни на чём.
            var surahs = await Resources().GetAllSurahsAsync();

            Assert.NotEmpty(surahs);
            Assert.All(surahs, s => Assert.Equal(CorpusTextEdition.UthmaniWasl, s.TextEdition));
        }

        [Fact]
        public async Task Resources_KeepTheWaslSign_TheEditionTheyDeclare()
        {
            // Редакция объявлена усмани: без знака васлы (ٱ) это уже другой текст,
            // и ожидания под ним другие — см. B1 в бэклоге.
            var surahs = await Resources().GetAllSurahsAsync();

            Assert.Contains(surahs.SelectMany(s => s.Ayahs), a => a.Arabic.Contains('\u0671'));
        }
    }
}
