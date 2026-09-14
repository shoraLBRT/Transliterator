using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Transliterator.Cli;
using Transliterator.Core.Repositories;
using Transliterator.Core.Services;
using Transliterator.Domain.Entities;
using Xunit;

namespace Transliterator.Tests.CliTests
{
    /// <summary>
    /// CLI (F5): профили списком, текст аргументом, из файла, из stdin и сурой
    /// из корпуса. Сервисы собираются тем же <see cref="CliServices"/>, что
    /// у настоящего запуска, — с файловыми хранилищами рядом со сборкой.
    /// </summary>
    public class CliAppTests : IDisposable
    {
        private const string Basmala = "بِسْمِ ٱللَّهِ ٱلرَّحْمَـٰنِ ٱلرَّحِيمِ";
        private const string BasmalaStandard = "бисми-лляяhи-ррохIмаани-ррохIииим";
        private const string BasmalaLatin = "bismi-llaahi-rroḥmaani-rroḥiiim";

        private readonly string _directory = Path.Combine(Path.GetTempPath(), "transliterator-cli-" + Guid.NewGuid().ToString("N"));

        public CliAppTests() => Directory.CreateDirectory(_directory);

        public void Dispose() => Directory.Delete(_directory, recursive: true);

        private sealed record Run(int ExitCode, string Out, string Error);

        private static async Task<Run> RunAsync(string[] args, string? input = null, bool redirected = false)
        {
            using var provider = new ServiceCollection()
                .AddLogging()
                .AddTransliteratorCli(new ConfigurationBuilder().Build())
                .BuildServiceProvider();

            var output = new StringWriter { NewLine = "\n" };
            var error = new StringWriter { NewLine = "\n" };
            var console = new CliConsole(new StringReader(input ?? string.Empty), output, error, redirected);

            var exitCode = await provider.GetRequiredService<CliApp>().RunAsync(args, console);

            return new Run(exitCode, output.ToString(), error.ToString());
        }

        private static CorpusSurah Surah(int number) =>
            new EmbeddedCorpusRepository(NullLogger<EmbeddedCorpusRepository>.Instance)
                .GetSurahAsync(number).GetAwaiter().GetResult()!;

        /// <summary>Ожидание корпуса по суре: аят на строке, номер в конце (D3).</summary>
        private static string CorpusExpectation(int number) =>
            string.Join("\n", Surah(number).Ayahs.Select(a => $"{a.Expected} {a.Number}")) + "\n";

        private string WriteFile(string name, byte[] content)
        {
            var path = Path.Combine(_directory, name);
            File.WriteAllBytes(path, content);
            return path;
        }

        // --- текст аргументом ---

        [Fact]
        public async Task Text_PrintsOnlyTheTransliteration()
        {
            var run = await RunAsync(new[] { Basmala });

            Assert.Equal(CliApp.Success, run.ExitCode);
            Assert.Equal(BasmalaStandard + "\n", run.Out);
            Assert.Equal(string.Empty, run.Error);
        }

        [Theory]
        [InlineData("Latin")]              // прежняя форма: профиль вторым аргументом
        [InlineData("--profile", "Latin")]
        [InlineData("-p", "Latin")]
        [InlineData("-p", "latin")]         // регистр в имени не важен, пока имя однозначно
        public async Task Profile_IsChosenByName(params string[] profile)
        {
            var run = await RunAsync(new[] { Basmala }.Concat(profile).ToArray());

            Assert.Equal(CliApp.Success, run.ExitCode);
            Assert.Equal(BasmalaLatin + "\n", run.Out);
        }

        [Fact]
        public async Task UnknownProfile_ListsTheAvailableOnes()
        {
            var run = await RunAsync(new[] { Basmala, "--profile", "Cyrillic" });

            Assert.Equal(CliApp.UsageError, run.ExitCode);
            Assert.Equal(string.Empty, run.Out);
            Assert.Contains("Latin, Standard", run.Error);
        }

        // --- сура из корпуса ---

        [Theory]
        [InlineData(1)]
        [InlineData(112)]
        public async Task Surah_IsTakenFromTheCorpusByNumber(int number)
        {
            // Та же сура, что подставляет панель веб-версии, и тот же вывод,
            // что записан в корпусе, — строка в строку.
            var run = await RunAsync(new[] { "--surah", number.ToString() });

            Assert.Equal(CliApp.Success, run.ExitCode);
            Assert.Equal(CorpusExpectation(number), run.Out);
        }

        [Fact]
        public async Task Surah_NotInTheCorpus_ListsTheOnesThatAre()
        {
            var run = await RunAsync(new[] { "-s", "2" });

            Assert.Equal(CliApp.UsageError, run.ExitCode);
            Assert.Equal(string.Empty, run.Out);
            Assert.Contains("1, 109, 110, 111, 112, 113, 114", run.Error);
        }

        [Theory]
        [InlineData("0")]
        [InlineData("115")]
        [InlineData("ikhlas")]
        [InlineData("-1")]
        public async Task SurahNumber_OutsideTheQuran_IsAUsageError(string number)
        {
            var run = await RunAsync(new[] { "--surah", number });

            Assert.Equal(CliApp.UsageError, run.ExitCode);
            Assert.Equal(string.Empty, run.Out);
        }

        // --- файл ---

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task File_IsReadAsUtf8_WithOrWithoutBom(bool bom)
        {
            // Строки через CRLF — так файл сохраняет Блокнот. Аяты в выводе
            // остаются на своих строках.
            var text = CorpusText.Arabic(Surah(112)).Replace("\n", "\r\n");
            var bytes = System.Text.Encoding.UTF8.GetBytes(text);
            if (bom)
                bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(bytes).ToArray();

            var run = await RunAsync(new[] { "--file", WriteFile("112.txt", bytes) });

            Assert.Equal(CliApp.Success, run.ExitCode);
            Assert.Equal(CorpusExpectation(112), run.Out);
            Assert.Equal(string.Empty, run.Error);
        }

        [Fact]
        public async Task File_InAnotherEncoding_IsRejectedWithAReason()
        {
            // «الله» в Windows-1256: байты не складываются в UTF-8.
            var path = WriteFile("cp1256.txt", new byte[] { 0xC7, 0xE1, 0xE1, 0xE5 });

            var run = await RunAsync(new[] { "-f", path });

            Assert.Equal(CliApp.Failure, run.ExitCode);
            Assert.Equal(string.Empty, run.Out);
            Assert.Contains("UTF-8", run.Error);
        }

        [Fact]
        public async Task File_NotFound_SaysWhichOne()
        {
            var path = Path.Combine(_directory, "missing.txt");

            var run = await RunAsync(new[] { "--file", path });

            Assert.Equal(CliApp.Failure, run.ExitCode);
            Assert.Contains(path, run.Error);
        }

        // --- stdin ---

        [Theory]
        [InlineData]                            // перенаправленный ввод читается и без флага
        [InlineData("--stdin")]
        [InlineData("-")]
        [InlineData("--profile", "Standard")]
        public async Task Stdin_IsRead(params string[] args)
        {
            var run = await RunAsync(args, input: Basmala + "\n", redirected: true);

            Assert.Equal(CliApp.Success, run.ExitCode);
            Assert.Equal(BasmalaStandard + "\n", run.Out);
        }

        [Fact]
        public async Task Stdin_WithTheOption_IsReadFromAConsoleToo()
        {
            var run = await RunAsync(new[] { "--stdin", "-p", "Latin" }, input: Basmala, redirected: false);

            Assert.Equal(CliApp.Success, run.ExitCode);
            Assert.Equal(BasmalaLatin + "\n", run.Out);
        }

        [Fact]
        public async Task EmptyInput_IsAnError_NotAnEmptyResult()
        {
            var run = await RunAsync(Array.Empty<string>(), input: " \n ", redirected: true);

            Assert.Equal(CliApp.UsageError, run.ExitCode);
            Assert.Equal(string.Empty, run.Out);
        }

        [Fact]
        public async Task InputWithoutArabicLetters_IsTransliterated_WithAWarning()
        {
            // Так выглядит арабица, которую Windows PowerShell отдал в ASCII.
            var run = await RunAsync(Array.Empty<string>(), input: "???? ?????", redirected: true);

            Assert.Equal(CliApp.Success, run.ExitCode);
            Assert.Contains("no Arabic letters", run.Error);
        }

        // --- списки и справка ---

        [Fact]
        public async Task Profiles_AreListedWithTheFirstSentenceOfTheirDescription()
        {
            var run = await RunAsync(new[] { "--profiles" });

            Assert.Equal(CliApp.Success, run.ExitCode);
            Assert.Contains("Latin ", run.Out);
            Assert.Contains("Standard (default)  Расширенная кириллица.", run.Out);
            Assert.DoesNotContain("Ключ вида", run.Out);
        }

        [Fact]
        public async Task Surahs_AreListedWithNumbersAndNames()
        {
            var run = await RunAsync(new[] { "--surahs" });
            var corpus = await new EmbeddedCorpusRepository(NullLogger<EmbeddedCorpusRepository>.Instance).GetAllSurahsAsync();

            Assert.Equal(CliApp.Success, run.ExitCode);
            Assert.All(corpus, surah => Assert.Contains($"{surah.Number,3}  {surah.RussianName} ({surah.ArabicName})", run.Out));
        }

        [Fact]
        public async Task Help_ShowsEveryInput()
        {
            var run = await RunAsync(new[] { "--help" });

            Assert.Equal(CliApp.Success, run.ExitCode);
            Assert.All(new[] { "--file", "--surah", "--stdin", "--profile", "--profiles", "--surahs" },
                option => Assert.Contains(option, run.Out));
        }

        [Theory]
        [InlineData("--frobnicate")]
        [InlineData("--file")]
        [InlineData(Basmala, "--surah", "112")]
        [InlineData(Basmala, "Latin", "extra")]
        [InlineData(Basmala, "Latin", "--profile", "Standard")]
        [InlineData("--file", "a.txt", "Latin")]
        public async Task UsageError_PointsToHelp(params string[] args)
        {
            var run = await RunAsync(args);

            Assert.Equal(CliApp.UsageError, run.ExitCode);
            Assert.Equal(string.Empty, run.Out);
            Assert.Contains("--help", run.Error);
        }

        // --- интерактивный режим ---

        [Fact]
        public async Task Interactive_Surah_ShowsTheListsBeforeAsking()
        {
            var run = await RunAsync(Array.Empty<string>(), input: "2\n112\n2\n");

            Assert.Equal(CliApp.Success, run.ExitCode);
            Assert.True(run.Out.IndexOf(Surah(112).RussianName, StringComparison.Ordinal) < run.Out.IndexOf("Surah number:", StringComparison.Ordinal));
            Assert.True(run.Out.IndexOf("Standard (default)", StringComparison.Ordinal) < run.Out.IndexOf("Profile (", StringComparison.Ordinal));
            Assert.EndsWith("=== Transliteration (Standard) ===\n" + CorpusExpectation(112), run.Out);
        }

        [Theory]
        [InlineData("", BasmalaStandard)]  // Enter — профиль по умолчанию
        [InlineData("1", BasmalaLatin)]    // номер из списка
        [InlineData("Latin", BasmalaLatin)]
        public async Task Interactive_TypedText_WithTheProfileChosen(string answer, string expected)
        {
            var run = await RunAsync(Array.Empty<string>(), input: $"1\n{Basmala}\n{answer}\n");

            Assert.Equal(CliApp.Success, run.ExitCode);
            Assert.EndsWith(expected + "\n", run.Out);
        }

        [Fact]
        public async Task Interactive_File_AcceptsAPathInQuotes()
        {
            var path = WriteFile("basmala.txt", System.Text.Encoding.UTF8.GetBytes(Basmala));

            var run = await RunAsync(Array.Empty<string>(), input: $"3\n\"{path}\"\n\n");

            Assert.Equal(CliApp.Success, run.ExitCode);
            Assert.EndsWith(BasmalaStandard + "\n", run.Out);
        }

        [Theory]
        [InlineData("9\n")]
        [InlineData("")]         // ввод кончился, не дождавшись ответа
        [InlineData("1\n\n")]    // пустой текст
        public async Task Interactive_WithoutAnAnswer_StopsWithAUsageError(string input)
        {
            var run = await RunAsync(Array.Empty<string>(), input: input);

            Assert.Equal(CliApp.UsageError, run.ExitCode);
            Assert.NotEqual(string.Empty, run.Error);
        }
    }
}
