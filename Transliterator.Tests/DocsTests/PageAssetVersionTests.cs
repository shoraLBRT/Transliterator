using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Xunit;

namespace Transliterator.Tests.DocsTests
{
    /// <summary>
    /// Свои скрипты и стили страница подключает с версией в адресе (D7):
    /// <c>js/page.js?v=35292ec5</c>. Сборку Blazor .NET 8 браузер берёт по хешам из
    /// её манифеста, а <c>page.js</c> и <c>app.css</c> — просто по имени, и после
    /// выкладки браузер с прежним <c>page.js</c> в кеше ломал первую отрисовку новой
    /// сборки: функции, которые она вызывает, в старом файле нет. Версия — начало
    /// SHA-256 содержимого, и тест падает, если файл изменился, а адрес — нет.
    /// </summary>
    public class PageAssetVersionTests
    {
        /// <summary>Свой файл из <c>wwwroot</c>: не среда Blazor и не чужой адрес.</summary>
        private static readonly Regex Asset = new(@"(?:href|src)=""(?!_framework/|https?:|//)([^""?#]+\.(?:js|css))(?:\?v=([^""]*))?""");

        [Fact]
        public void EveryOwnScriptAndStyle_IsLinkedWithTheVersionOfItsContent()
        {
            var wwwroot = Path.Combine(ReadmeTests.RepositoryRoot(), "Transliterator.Web", "wwwroot");
            var links = Asset.Matches(File.ReadAllText(Path.Combine(wwwroot, "index.html"))).ToList();

            Assert.Contains(links, link => link.Groups[1].Value == "js/page.js");
            Assert.Contains(links, link => link.Groups[1].Value == "css/app.css");

            Assert.All(links, link =>
            {
                var path = link.Groups[1].Value;
                var expected = VersionOf(Path.Combine(wwwroot, path.Replace('/', Path.DirectorySeparatorChar)));

                Assert.True(link.Groups[2].Value == expected,
                            $"index.html must link {path}?v={expected} (now: {link.Value}) — the file changed");
            });
        }

        /// <summary>
        /// Начало SHA-256 файла с переводами строк LF: на Windows git отдаёт файл с CRLF,
        /// на сборке Linux — с LF, и версия от этого зависеть не должна.
        /// </summary>
        private static string VersionOf(string file)
        {
            var bytes = File.ReadAllBytes(file);
            var normalized = new List<byte>(bytes.Length);

            for (var i = 0; i < bytes.Length; i++)
            {
                if (bytes[i] == '\r' && i + 1 < bytes.Length && bytes[i + 1] == '\n')
                    continue;

                normalized.Add(bytes[i]);
            }

            return Convert.ToHexString(SHA256.HashData(normalized.ToArray()))[..8].ToLowerInvariant();
        }
    }
}
