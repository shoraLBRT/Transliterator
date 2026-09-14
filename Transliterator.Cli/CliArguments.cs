using System.Globalization;

namespace Transliterator.Cli
{
    public enum CliCommand
    {
        Transliterate,
        Interactive,
        ListProfiles,
        ListSurahs,
        Help
    }

    public enum InputSource
    {
        None,
        Text,
        File,
        Surah,
        Stdin
    }

    /// <summary>Ошибка, после которой CLI завершается с сообщением и кодом выхода.</summary>
    public sealed class CliException : Exception
    {
        public CliException(string message, int exitCode) : base(message) => ExitCode = exitCode;

        public int ExitCode { get; }
    }

    /// <summary>
    /// Разобранная командная строка. Прежняя форма <c>"текст" [профиль]</c> работает
    /// как раньше: README и чужие скрипты ею пользуются.
    /// </summary>
    public sealed record CliArguments(
        CliCommand Command,
        InputSource Source = InputSource.None,
        string? Text = null,
        string? FilePath = null,
        int? SurahNumber = null,
        string? Profile = null)
    {
        public const int FirstSurah = 1;
        public const int LastSurah = 114;

        /// <param name="inputRedirected">
        /// Ввод перенаправлен (<c>cli &lt; файл</c>, <c>… | cli</c>). Тогда текст без флага
        /// берётся из stdin: спрашивать интерактивно не у кого.
        /// </param>
        /// <exception cref="CliException">Командная строка не разбирается.</exception>
        public static CliArguments Parse(IReadOnlyList<string> args, bool inputRedirected)
        {
            string? text = null, file = null, profile = null, positionalProfile = null;
            int? surah = null;
            bool stdin = false, help = false, listProfiles = false, listSurahs = false;
            var positional = 0;

            for (var i = 0; i < args.Count; i++)
            {
                switch (args[i])
                {
                    case "-h" or "--help" or "/?":
                        help = true;
                        break;
                    case "--profiles":
                        listProfiles = true;
                        break;
                    case "--surahs":
                        listSurahs = true;
                        break;
                    case "--stdin" or "-":
                        stdin = true;
                        break;
                    case "-f" or "--file":
                        file = Value(args, ref i);
                        break;
                    case "-p" or "--profile":
                        profile = Value(args, ref i);
                        break;
                    case "-s" or "--surah":
                        surah = ParseSurahNumber(Value(args, ref i));
                        break;
                    default:
                        var arg = args[i];

                        // Арабский текст с дефиса не начинается, а опечатка в имени флага
                        // иначе ушла бы в конвейер как текст и вернулась пустым выводом.
                        if (arg.StartsWith('-'))
                            throw Usage($"Unknown option '{arg}'.");

                        if (positional == 0)
                            text = arg;
                        else if (positional == 1)
                            positionalProfile = arg;
                        else
                            throw Usage($"Unexpected argument '{arg}'. Put text with spaces in quotes.");

                        positional++;
                        break;
                }
            }

            if (help)
                return new CliArguments(CliCommand.Help);
            if (listProfiles)
                return new CliArguments(CliCommand.ListProfiles);
            if (listSurahs)
                return new CliArguments(CliCommand.ListSurahs);

            if (profile is not null && positionalProfile is not null)
                throw Usage("The profile is given twice: as the second argument and with --profile.");

            profile ??= positionalProfile;

            var sources = new List<InputSource>();
            if (text is not null) sources.Add(InputSource.Text);
            if (file is not null) sources.Add(InputSource.File);
            if (surah is not null) sources.Add(InputSource.Surah);
            if (stdin) sources.Add(InputSource.Stdin);

            if (sources.Count > 1)
                throw Usage("Choose one input: text, --file, --surah or --stdin. A profile is chosen with --profile.");

            if (sources.Count == 0)
                return inputRedirected
                    ? new CliArguments(CliCommand.Transliterate, InputSource.Stdin, Profile: profile)
                    : new CliArguments(CliCommand.Interactive, Profile: profile);

            return new CliArguments(CliCommand.Transliterate, sources[0], text, file, surah, profile);
        }

        /// <exception cref="CliException">Не число или номер вне 1–114.</exception>
        public static int ParseSurahNumber(string value)
        {
            if (int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var number)
                && number is >= FirstSurah and <= LastSurah)
                return number;

            throw Usage($"A surah number is from {FirstSurah} to {LastSurah}, not '{value}'.");
        }

        private static string Value(IReadOnlyList<string> args, ref int i)
        {
            if (i + 1 >= args.Count)
                throw Usage($"Option '{args[i]}' needs a value.");

            return args[++i];
        }

        private static CliException Usage(string message) => new(message, CliApp.UsageError);
    }
}
