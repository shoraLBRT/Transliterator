using System.Xml.Linq;
using Transliterator.Domain.Localization;

namespace Transliterator.Tests.DocsTests
{
    /// <summary>
    /// Строки интерфейса из ресурсов (D7) — то, что страница показывает. Тесты читают
    /// сами файлы <c>.resx</c>: веб-проект — сборка под браузер, и ссылаться на неё
    /// из тестов незачем, а файл и есть источник строки.
    /// </summary>
    internal static class PageStrings
    {
        /// <summary>Ресурсы интерфейса: веб-страница и тексты ядра, которые доходят до человека.</summary>
        public static readonly string[] Resources =
        {
            Path.Combine("Transliterator.Web", "Localization", "Strings"),
            Path.Combine("Transliterator.Core", "Localization", "CoreText"),
        };

        /// <summary>Файл ресурса на языке: нейтральный (русский) — без суффикса.</summary>
        public static string FileOf(string resource, string language) =>
            Path.Combine(ReadmeTests.RepositoryRoot(),
                         language == PageLanguages.Default.Code ? resource + ".resx" : $"{resource}.{language}.resx");

        public static IReadOnlyDictionary<string, string> Read(string resource, string language) =>
            XDocument.Load(FileOf(resource, language))
                     .Root!.Elements("data")
                     .ToDictionary(data => (string)data.Attribute("name")!, data => (string?)data.Element("value") ?? string.Empty);

        /// <summary>Строки страницы на языке.</summary>
        public static IReadOnlyDictionary<string, string> Web(string language) => Read(Resources[0], language);
    }
}
