using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Runtime.Versioning;
using Transliterator.Core.Models;
using Transliterator.Domain.Entities;
using Transliterator.Domain.Interfaces;

namespace Transliterator.Core.Repositories
{
    /// <summary>
    /// Корпус примеров из папки ресурсов: один JSON на суру, имя файла — номер
    /// суры тремя цифрами (<c>001.json</c>, <c>110.json</c>), чтобы имена
    /// сортировались по порядку сур, а не по первой цифре.
    /// </summary>
    /// <remarks>
    /// <para>
    /// В отличие от <see cref="JsonProfileRepository"/> битый файл здесь не
    /// пропускается, а роняет чтение с внятным сообщением. Пропуск для профиля —
    /// потеря одного варианта письма, а для корпуса — молча исчезнувшие тесты:
    /// data-driven прогон построит на один случай меньше и останется зелёным.
    /// </para>
    /// <para>
    /// Как и <see cref="JsonProfileRepository"/>, читает каталог рядом со сборкой
    /// и потому в браузере неприменима. Там корпус читает
    /// <see cref="EmbeddedCorpusRepository"/> — из ресурсов сборки, тем же разбором.
    /// </para>
    /// </remarks>
    [UnsupportedOSPlatform("browser")]
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
        public static string FileNameFor(int number) => CorpusReader.FileNameFor(number);

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
            await using var stream = File.OpenRead(filePath);

            return await CorpusReader.ReadAsync(stream, filePath, Path.GetFileName(filePath), "Corpus file");
        }
    }
}
