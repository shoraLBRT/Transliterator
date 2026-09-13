using Microsoft.Extensions.Logging;
using System.Reflection;
using Transliterator.Domain.Entities;
using Transliterator.Domain.Exceptions;
using Transliterator.Domain.Interfaces;

namespace Transliterator.Core.Repositories
{
    /// <summary>
    /// Корпус примеров из ресурсов сборки. Файловой системы не касается вовсе
    /// и потому работает в браузере (Blazor WebAssembly) — там панель готовых сур
    /// (D3) строит список отсюда.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Корпус вшит в сборку <b>целиком</b>, а не подгружается по одной суре.
    /// Семь сур — около десяти килобайт текста, того же порядка, что два профиля,
    /// и отдельный запрос за каждой сурой стоил бы дороже, чем они весят.
    /// Подгрузка понадобится вместе с «Полным Кораном в корпусе» — и будет другой
    /// реализацией <see cref="ICorpusRepository"/>, а не правкой этой.
    /// </para>
    /// <para>
    /// Разбор и проверка суры — те же, что у <see cref="JsonCorpusRepository"/>
    /// (<see cref="CorpusReader"/>): битый ресурс роняет чтение, а не пропускается.
    /// В отличие от <see cref="EmbeddedProfileRepository"/> ресурс читается при
    /// каждом обращении, а не один раз: сура — изменяемый объект, и общий экземпляр
    /// на всех читателей вёл бы себя иначе, чем файловое хранилище, которое
    /// каждому отдаёт свою копию.
    /// </para>
    /// </remarks>
    public class EmbeddedCorpusRepository : ICorpusRepository
    {
        /// <summary>
        /// Имя ресурса собирается компилятором из корневого пространства имён
        /// и пути к файлу: <c>Resources\Corpus\110.json</c> превращается
        /// в <c>Transliterator.Core.Resources.Corpus.110.json</c>.
        /// </summary>
        public const string DefaultResourcePrefix = "Transliterator.Core.Resources.Corpus.";

        private const string ResourceSuffix = ".json";

        private readonly Assembly _assembly;
        private readonly string _resourcePrefix;
        private readonly ILogger<EmbeddedCorpusRepository> _logger;

        public EmbeddedCorpusRepository(ILogger<EmbeddedCorpusRepository> logger)
            : this(typeof(EmbeddedCorpusRepository).Assembly, DefaultResourcePrefix, logger)
        {
        }

        /// <summary>
        /// Корпус можно вшить и в чужую сборку: класс знает, как сура читается,
        /// и не знает, откуда она взялась.
        /// </summary>
        public EmbeddedCorpusRepository(Assembly assembly, string resourcePrefix, ILogger<EmbeddedCorpusRepository> logger)
        {
            _assembly = assembly;
            _resourcePrefix = resourcePrefix;
            _logger = logger;
        }

        public async Task<CorpusSurah?> GetSurahAsync(int number)
        {
            var resourceName = _resourcePrefix + CorpusReader.FileNameFor(number);

            if (!ResourceNames().Contains(resourceName))
                return null;

            return await ReadAsync(resourceName);
        }

        public async Task<IReadOnlyList<CorpusSurah>> GetAllSurahsAsync()
        {
            var resourceNames = ResourceNames().ToList();

            if (resourceNames.Count == 0)
            {
                _logger.LogWarning("No embedded corpus resources under {Prefix} in {Assembly}",
                                   _resourcePrefix, _assembly.GetName().Name);
                return Array.Empty<CorpusSurah>();
            }

            var surahs = new List<CorpusSurah>();

            foreach (var resourceName in resourceNames)
                surahs.Add(await ReadAsync(resourceName));

            return surahs.OrderBy(s => s.Number).ToList();
        }

        private IEnumerable<string> ResourceNames() =>
            _assembly.GetManifestResourceNames()
                     .Where(name => name.StartsWith(_resourcePrefix, StringComparison.Ordinal)
                                 && name.EndsWith(ResourceSuffix, StringComparison.Ordinal))
                     .OrderBy(name => name, StringComparer.Ordinal);

        private async Task<CorpusSurah> ReadAsync(string resourceName)
        {
            await using var stream = _assembly.GetManifestResourceStream(resourceName)
                ?? throw new TransliterationException($"Embedded corpus resource is unreadable: {resourceName}");

            // Имя файла внутри имени ресурса — всё, что после префикса папки:
            // по нему, как и у файла на диске, сверяется номер суры.
            var fileName = resourceName[_resourcePrefix.Length..];

            return await CorpusReader.ReadAsync(stream, resourceName, fileName, "Embedded corpus resource");
        }
    }
}
