// Transliterator.Cli/Program.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text;
using Transliterator.Cli;

Console.OutputEncoding = Encoding.UTF8;

// Аргументы в хост не передаются: командную строку разбирает CliArguments,
// и "--file путь" не должен становиться ключом конфигурации. Корень — папка
// сборки, а не текущая: иначе appsettings.json не находится, если CLI запущен
// не из своей папки.
using IHost host = Host.CreateDefaultBuilder()
    .UseContentRoot(AppContext.BaseDirectory)
    .ConfigureLogging(logging =>
    {
        // stdout занят результатом, и строка лога в нём была бы порчей вывода.
        logging.ClearProviders();
        logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
        logging.SetMinimumLevel(LogLevel.Warning);
    })
    .ConfigureServices((context, services) => services.AddTransliteratorCli(context.Configuration))
    .Build();

// Перенаправленный ввод читается как UTF-8 явно. Console.In взял бы кодовую
// страницу консоли, и арабица из файла пришла бы испорченной.
TextReader input;
if (Console.IsInputRedirected)
{
    input = new StreamReader(Console.OpenStandardInput(), new UTF8Encoding(false));
}
else
{
    Console.InputEncoding = Encoding.UTF8;
    input = Console.In;
}

var console = new CliConsole(input, Console.Out, Console.Error, Console.IsInputRedirected);

return await host.Services.GetRequiredService<CliApp>().RunAsync(args, console);
