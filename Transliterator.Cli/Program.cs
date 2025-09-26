// Transliterator.Cli/Program.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Transliterator.Core.Repositories;
using Transliterator.Core.Services;
using Transliterator.Domain.Interfaces;

// Создаем хост с конфигурацией
using IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Регистрируем сервисы
        services.AddTransient<LetterChangeService>();
        services.AddTransient<IProfileRepository, JsonProfileRepository>();
        services.AddTransient<ITransliterationService, TransliterationService>();
    })
    .Build();


// Получаем сервис транслитерации
var transliterationService = host.Services.GetRequiredService<ITransliterationService>();

// Проверяем аргументы командной строки
string profileName = "Standard";
string textToTransliterate = "بَتَ";

if (args.Length > 0)
{
    textToTransliterate = args[0];
}
if (args.Length > 1)
{
    profileName = args[1];
}

try
{
    Console.WriteLine($"Using profile: {profileName}");
    Console.WriteLine($"Input text: {textToTransliterate}");

    // Выполняем транслитерацию
    var result = await transliterationService.TransliterateAsync(textToTransliterate, profileName);

    Console.WriteLine($"Output: {result.TransliteratedText}");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}