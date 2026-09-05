using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Transliterator.Core.Models;
using Transliterator.Domain.Entities;
using Transliterator.Domain.Exceptions;
using Transliterator.Domain.Interfaces;

namespace Transliterator.Core.Repositories
{
    /// <summary>
    /// Корпус примеров из папки ресурсов: один JSON на суру, имя файла — номер
    /// суры тремя цифрами (<c>001.json</c>, <c>110.json</c>), чтобы имена
    /// сортировались по порядку сур, а не по первой цифре.
    /// </summary>
    /// <remarks>
    /// В отличие от <see cref="JsonProfileRepository"/> битый файл здесь не
    /// пропускается, а роняет чтение с внятным сообщением. Пропуск для профиля —
    /// потеря одного варианта письма, а для корпуса — молча исчезнувшие тесты:
    /// data-driven прогон построит на один случай меньше и останется зелёным.
    /// </remarks>
    public class JsonCorpusRepository : ICorpusRepository
    {
        private const string FileMask = "*.json";

        private readonly string _corpusPath;
        private readonly ILogger<JsonCorpusRepository> _logger;

        public JsonCorpusRepository(IOptions<StorageSettings> options, ILogger<JsonCorpusRepository> logger)
        {
            _logger = logger;
            _corpusPath = ResolveCorpusPath(options.Value.CorpusPath);
        }

        /// <summary>
        /// Корпус копируется рядом со сборкой, а рабочая папка при запуске через
        /// "dotnet run" — папка проекта. Относительный путь считаем от сборки.
        /// </summary>
        private static string ResolveCorpusPath(string configuredPath) =>
            Path.IsPathRooted(configuredPath)
                ? configuredPath
                : Path.Combine(AppContext.BaseDirectory, configuredPath);

        /// <summary>Имя файла суры: номер тремя цифрами.</summary>
        public static string FileNameFor(int number) => $"{number:000}.json";

        public async Task<CorpusSurah?> GetSurahAsync(int number)
        {
            var filePath = Path.Combine(_corpusPath, FileNameFor(number));

            if (!File.Exists(filePath))
                return null;

            return await ReadAsync(filePath);
        }

        public async Task<IReadOnlyList<CorpusSurah>> GetAllSurahsAsync()
        {
            if (!Directory.Exists(_corpusPath))
            {
                _logger.LogWarning("Corpus directory not found: {Path}", _corpusPath);
                return Array.Empty<CorpusSurah>();
            }

            var surahs = new List<CorpusSurah>();

            foreach (var file in Directory.GetFiles(_corpusPath, FileMask).OrderBy(f => f, StringComparer.Ordinal))
                surahs.Add(await ReadAsync(file));

            return surahs.OrderBy(s => s.Number).ToList();
        }

        private static async Task<CorpusSurah> ReadAsync(string filePath)
        {
            CorpusSurah? surah;

            try
            {
                surah = JsonSerializer.Deserialize<CorpusSurah>(await File.ReadAllTextAsync(filePath));
            }
            catch (JsonException ex)
            {
                throw new TransliterationException($"Corpus file is not valid JSON: {filePath}", ex);
            }

            if (surah is null)
                throw new TransliterationException($"Corpus file is empty: {filePath}");

            Validate(surah, filePath);

            return surah;
        }

        /// <summary>
        /// Проверяет ровно то, на что опираются читатели корпуса: номер, по которому
        /// сура ищется, названия для панели примеров, редакцию текста и сплошную
        /// нумерацию аятов. Пропущенный аят — это не «корпус поменьше», а дыра
        /// в покрытии, и увидеть её надо при чтении, а не в отчёте о прогоне.
        /// </summary>
        private static void Validate(CorpusSurah surah, string filePath)
        {
            void Require(bool condition, string message)
            {
                if (!condition)
                    throw new TransliterationException($"{Path.GetFileName(filePath)}: {message}");
            }

            Require(surah.Number is >= 1 and <= 114, $"surah number {surah.Number} is out of range 1..114");
            Require(Path.GetFileName(filePath) == FileNameFor(surah.Number),
                    $"file name does not match surah number {surah.Number}");
            Require(!string.IsNullOrWhiteSpace(surah.ArabicName), "arabic name is empty");
            Require(!string.IsNullOrWhiteSpace(surah.RussianName), "russian name is empty");
            Require(CorpusTextEdition.IsKnown(surah.TextEdition),
                    $"unknown text edition '{surah.TextEdition}'");
            Require(surah.Ayahs.Count > 0, "surah has no ayahs");

            for (int i = 0; i < surah.Ayahs.Count; i++)
            {
                var ayah = surah.Ayahs[i];

                Require(ayah.Number == i + 1, $"ayah #{i + 1} is numbered {ayah.Number}");
                Require(!string.IsNullOrWhiteSpace(ayah.Arabic), $"ayah {ayah.Number} has no arabic text");
                Require(!string.IsNullOrWhiteSpace(ayah.Expected), $"ayah {ayah.Number} has no expected transliteration");
            }
        }
    }
}
