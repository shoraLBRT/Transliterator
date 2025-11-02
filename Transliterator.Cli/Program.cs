// Transliterator.Cli/Program.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Transliterator.Core.Repositories;
using Transliterator.Core.Services.Rules;
using Transliterator.Domain.Interfaces;
using System.Text;

// Настраиваем кодировку консоли для отображения Unicode символов
Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

// Создаем хост с конфигурацией
using IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Регистрируем сервисы
        services.AddTransient<LetterChangeService>();
        services.AddTransient<IProfileRepository, JsonProfileRepository>();
        services.AddTransient<ITransliterationService, TransliterationService>();
        services.AddTransient<RulesService>();
        services.AddTransient<VowelRules>();
        services.AddTransient<ArticleRules>();
        services.AddTransient<WaslaRules>();
        services.AddTransient<PostEmphaticVowelReplacer>();
        services.AddTransient<LamRule>();
        services.AddTransient<AlifMaqsuraRule>();
    })
    .Build();


// Получаем сервис транслитерации
var transliterationService = host.Services.GetRequiredService<ITransliterationService>();

// Проверяем аргументы командной строки
string profileName = "Standard";
string textToTransliterate = "بِسْمِ ٱللَّهِ ٱلرَّحْمَـٰنِ ٱلرَّحِيمِ ١ ٱلْحَمْدُ لِلَّهِ رَبِّ ٱلْعَـٰلَمِينَ ٢ ٱلرَّحْمَـٰنِ ٱلرَّحِيمِ ٣ مَـٰلِكِ يَوْمِ ٱلدِّينِ ٤ إِيَّاكَ نَعْبُدُ وَإِيَّاكَ نَسْتَعِينُ ٥ ٱهْدِنَا ٱلصِّرَٰطَ ٱلْمُسْتَقِيمَ ٦ صِرَٰطَ ٱلَّذِينَ أَنْعَمْتَ عَلَيْهِمْ غَيْرِ ٱلْمَغْضُوبِ عَلَيْهِمْ وَلَا ٱلضَّآلِّينَ ";
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