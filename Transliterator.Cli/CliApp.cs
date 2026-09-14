using System.Globalization;
using System.Text;
using Transliterator.Core.Services;
using Transliterator.Domain.Entities;
using Transliterator.Domain.Exceptions;
using Transliterator.Domain.Interfaces;

namespace Transliterator.Cli
{
    /// <param name="InputRedirected">Ввод пришёл из файла или конвейера, а не с клавиатуры.</param>
    public sealed record CliConsole(TextReader In, TextWriter Out, TextWriter Error, bool InputRedirected);

    /// <summary>
    /// Команды CLI. Консоль передаётся снаружи, поэтому всё, что CLI печатает
    /// и читает, проверяется тестами, а не только руками.
    /// <para>
    /// В stdout идёт только результат: транслитерацию перенаправляют в файл или
    /// передают дальше по конвейеру, и строка «Using profile…» там была бы порчей
    /// вывода. Сообщения и ошибки — в stderr. Исключение — интерактивный режим:
    /// там вывод читает человек, и вопросы стоят рядом с ответом.
    /// </para>
    /// </summary>
    public sealed class CliApp
    {
        public const int Success = 0;
        public const int Failure = 1;
        public const int UsageError = 2;

        public const string DefaultProfile = "Standard";

        /// <summary>
        /// Строгий UTF-8. Файл в другой кодировке (чаще всего Windows-1256) иначе
        /// читается без ошибки, а арабица превращается в «�», и конвейер молча
        /// транслитерирует мусор.
        /// </summary>
        private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

        private readonly ITransliterationService _service;
        private readonly IProfileRepository _profiles;
        private readonly ICorpusRepository _corpus;

        public CliApp(ITransliterationService service, IProfileRepository profiles, ICorpusRepository corpus)
        {
            _service = service;
            _profiles = profiles;
            _corpus = corpus;
        }

        /// <returns>Код выхода: <see cref="Success"/>, <see cref="Failure"/> или <see cref="UsageError"/>.</returns>
        public async Task<int> RunAsync(IReadOnlyList<string> args, CliConsole console)
        {
            CliArguments arguments;

            try
            {
                arguments = CliArguments.Parse(args, console.InputRedirected);
            }
            catch (CliException ex)
            {
                console.Error.WriteLine(ex.Message);
                console.Error.WriteLine("Run with --help to see the options.");
                return ex.ExitCode;
            }

            try
            {
                return arguments.Command switch
                {
                    CliCommand.Help => Help(console.Out),
                    CliCommand.ListProfiles => await ListProfilesAsync(console.Out),
                    CliCommand.ListSurahs => await ListSurahsAsync(console.Out),
                    CliCommand.Interactive => await InteractiveAsync(arguments, console),
                    _ => await TransliterateAsync(arguments, console)
                };
            }
            catch (CliException ex)
            {
                console.Error.WriteLine(ex.Message);
                return ex.ExitCode;
            }
            catch (TransliterationException ex)
            {
                console.Error.WriteLine($"Error: {ex.Message}");
                return Failure;
            }
        }

        private async Task<int> TransliterateAsync(CliArguments arguments, CliConsole console)
        {
            // Профиль проверяется до чтения ввода: опечатка в имени не должна
            // съедать stdin, который второй раз уже не прочитать.
            var profile = await ResolveProfileAsync(arguments.Profile ?? DefaultProfile);

            var text = arguments.Source switch
            {
                InputSource.Text => arguments.Text!,
                InputSource.File => await ReadFileAsync(arguments.FilePath!),
                InputSource.Surah => await SurahTextAsync(arguments.SurahNumber!.Value),
                _ => await console.In.ReadToEndAsync()
            };

            return Write(EnsureNotEmpty(text), profile, console);
        }

        private async Task<int> InteractiveAsync(CliArguments arguments, CliConsole console)
        {
            var output = console.Out;

            output.WriteLine("No input given. Run with --help to see all the options.");
            output.WriteLine();
            output.WriteLine("Input:");
            output.WriteLine("  1. Type Arabic text");
            output.WriteLine("  2. A surah from the corpus");
            output.WriteLine("  3. A UTF-8 text file");

            string text;

            switch (Ask(console, "Select an option (1-3): "))
            {
                case "1":
                    text = Ask(console, "Arabic text: ");
                    break;

                case "2":
                    output.WriteLine();
                    output.WriteLine("Surahs in the corpus:");
                    await ListSurahsAsync(output);
                    text = await SurahTextAsync(CliArguments.ParseSurahNumber(Ask(console, "Surah number: ")));
                    break;

                case "3":
                    // Проводник копирует путь в кавычках.
                    text = await ReadFileAsync(Ask(console, "Path to the file: ").Trim('"'));
                    break;

                case var other:
                    throw new CliException($"Unknown option '{other}'.", UsageError);
            }

            EnsureNotEmpty(text);

            var profile = arguments.Profile is not null
                ? await ResolveProfileAsync(arguments.Profile)
                : await AskProfileAsync(console);

            output.WriteLine();
            output.WriteLine($"=== Transliteration ({profile.Name}) ===");

            return Write(text, profile, console);
        }

        /// <summary>Список профилей стоит перед вопросом: имя вслепую не угадать.</summary>
        private async Task<TransliterationProfile> AskProfileAsync(CliConsole console)
        {
            var profiles = await ProfilesAsync();

            console.Out.WriteLine();
            console.Out.WriteLine("Profiles:");
            WriteProfiles(console.Out, profiles, numbered: true);

            var answer = Ask(console, $"Profile (number or name, Enter for {DefaultProfile}): ");

            if (answer.Length == 0)
                return await ResolveProfileAsync(DefaultProfile);

            if (int.TryParse(answer, NumberStyles.None, CultureInfo.InvariantCulture, out var index)
                && index >= 1 && index <= profiles.Count)
                return profiles[index - 1];

            return await ResolveProfileAsync(answer);
        }

        private int Write(string text, TransliterationProfile profile, CliConsole console)
        {
            // Арабица, испорченная кодировкой по дороге (Windows PowerShell отдаёт
            // программе текст в ASCII), приходит вопросительными знаками. Конвейер
            // пропускает их как есть, и без предупреждения это выглядело бы как
            // ошибка транслитерации, а не ввода.
            if (!text.Any(IsArabic))
                console.Error.WriteLine("Warning: the input has no Arabic letters. If it came through a pipe, " +
                                        "its encoding may have been lost on the way: read the text with --file instead.");

            console.Out.WriteLine(_service.Transliterate(text, profile).TransliteratedText);
            return Success;
        }

        private static string EnsureNotEmpty(string text) =>
            string.IsNullOrWhiteSpace(text)
                ? throw new CliException("The input is empty: there is nothing to transliterate.", UsageError)
                : text;

        private async Task<TransliterationProfile> ResolveProfileAsync(string name)
        {
            var profiles = await ProfilesAsync();

            // Регистр в имени профиля в командной строке не важен, если имя от этого
            // не становится двусмысленным: «latin» — это Latin.
            var match = profiles.FirstOrDefault(p => p.Name == name);
            if (match is null)
            {
                var similar = profiles.Where(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)).ToList();
                if (similar.Count == 1)
                    match = similar[0];
            }

            return match ?? throw new CliException(
                $"Profile '{name}' not found. Available: {string.Join(", ", profiles.Select(p => p.Name))}.", UsageError);
        }

        private async Task<List<TransliterationProfile>> ProfilesAsync() =>
            (await _profiles.GetAllProfilesAsync())
                .Where(p => !string.IsNullOrWhiteSpace(p.Name))
                .DistinctBy(p => p.Name, StringComparer.Ordinal)
                .OrderBy(p => p.Name, StringComparer.Ordinal)
                .ToList();

        private async Task<string> SurahTextAsync(int number)
        {
            var surah = await _corpus.GetSurahAsync(number);
            if (surah is not null)
                return CorpusText.Arabic(surah);

            var available = (await _corpus.GetAllSurahsAsync()).Select(s => s.Number.ToString(CultureInfo.InvariantCulture));
            throw new CliException($"Surah {number} is not in the corpus. Available: {string.Join(", ", available)}.", UsageError);
        }

        private static async Task<string> ReadFileAsync(string path)
        {
            try
            {
                using var reader = new StreamReader(path, StrictUtf8, detectEncodingFromByteOrderMarks: true);
                return await reader.ReadToEndAsync();
            }
            catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
            {
                throw new CliException($"File not found: {path}", Failure);
            }
            catch (DecoderFallbackException)
            {
                throw new CliException($"File {path} is not in UTF-8. Save it as UTF-8: " +
                                       "in another encoding the Arabic letters cannot be read.", Failure);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                throw new CliException($"Cannot read the file '{path}': {ex.Message}", Failure);
            }
        }

        private async Task<int> ListProfilesAsync(TextWriter output)
        {
            WriteProfiles(output, await ProfilesAsync(), numbered: false);
            return Success;
        }

        private static void WriteProfiles(TextWriter output, IReadOnlyList<TransliterationProfile> profiles, bool numbered)
        {
            if (profiles.Count == 0)
            {
                output.WriteLine("  No profiles found.");
                return;
            }

            var labels = profiles.Select(p => p.Name == DefaultProfile ? $"{p.Name} (default)" : p.Name).ToList();
            var width = labels.Max(label => label.Length);

            for (var i = 0; i < profiles.Count; i++)
            {
                var number = numbered ? $"{i + 1}. " : string.Empty;
                output.WriteLine($"  {number}{labels[i].PadRight(width)}  {FirstSentence(profiles[i].Description)}");
            }
        }

        /// <summary>
        /// Описание профиля бывает в полстраницы (у Standard оно перечисляет все
        /// варианты ключей), а в списке нужна одна строка, чтобы выбрать.
        /// </summary>
        private static string FirstSentence(string? description)
        {
            if (string.IsNullOrWhiteSpace(description))
                return string.Empty;

            var end = description.IndexOf(". ", StringComparison.Ordinal);
            return end < 0 ? description.Trim() : description[..(end + 1)];
        }

        private async Task<int> ListSurahsAsync(TextWriter output)
        {
            var surahs = await _corpus.GetAllSurahsAsync();

            if (surahs.Count == 0)
                output.WriteLine("  The corpus is empty.");

            foreach (var surah in surahs)
                output.WriteLine($"  {surah.Number,3}  {surah.RussianName} ({surah.ArabicName})");

            return Success;
        }

        private static int Help(TextWriter output)
        {
            output.WriteLine("""
                Usage:
                  Transliterator.Cli "<Arabic text>" [profile]
                  Transliterator.Cli --file <path> [--profile <name>]
                  Transliterator.Cli --surah <number> [--profile <name>]
                  Transliterator.Cli --stdin [--profile <name>]
                  Transliterator.Cli --profiles
                  Transliterator.Cli --surahs

                Options:
                  -f, --file <path>      Read the text from a UTF-8 file.
                  -s, --surah <number>   Take a surah from the corpus (see --surahs).
                      --stdin, -         Read the text from standard input.
                                         Redirected input is read without this option too.
                  -p, --profile <name>   Profile to write with (see --profiles). Default: Standard.
                      --profiles         List the profiles.
                      --surahs           List the surahs in the corpus.
                  -h, --help             Show this help.

                Only the transliteration goes to standard output; messages go to standard error.
                Without arguments the application asks for the input and the profile.
                """);
            return Success;
        }

        private static string Ask(CliConsole console, string prompt)
        {
            console.Out.Write(prompt);
            return console.In.ReadLine()?.Trim() ?? string.Empty;
        }

        /// <summary>Блоки арабицы: основной, дополнение, расширение и формы представления.</summary>
        private static bool IsArabic(char c) =>
            c is >= '؀' and <= 'ۿ'
              or >= 'ݐ' and <= 'ݿ'
              or >= 'ࢠ' and <= 'ࣿ'
              or >= 'ﭐ' and <= '﷿'
              or >= 'ﹰ' and <= 'ﻼ';
    }
}
