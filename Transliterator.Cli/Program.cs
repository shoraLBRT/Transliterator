// Transliterator.Cli/Program.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Transliterator.Core.Repositories;
using Transliterator.Core.Services.Rules;
using Transliterator.Domain.Interfaces;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

using IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
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

var transliterationService = host.Services.GetRequiredService<ITransliterationService>();

string profileName = "Standard";
string? textToTransliterate = null;

if (args.Length > 0)
{
    textToTransliterate = args[0];
    if (args.Length > 1)
    {
        profileName = args[1];
    }
}
else
{
    Console.WriteLine("No input arguments detected.");
    Console.WriteLine("Choose input mode:");
    Console.WriteLine("1. Enter Arabic text manually");
    Console.WriteLine("2. Use example text from Al-Fatiha");
    Console.Write("Select an option (1/2): ");

    var choice = Console.ReadLine()?.Trim();
    if (choice == "1")
    {
        Console.Write("Enter Arabic text: ");
        textToTransliterate = Console.ReadLine();
    }
    else if (choice == "2")
    {
        textToTransliterate = "بِسْمِ ٱللَّهِ ٱلرَّحْمَـٰنِ ٱلرَّحِيمِ ١ ٱلْحَمْدُ لِلَّهِ رَبِّ ٱلْعَـٰلَمِينَ ٢ ٱلرَّحْمَـٰنِ ٱلرَّحِيمِ ٣ مَـٰلِكِ يَوْمِ ٱلدِّينِ ٤ إِيَّاكَ نَعْبُدُ وَإِيَّاكَ نَسْتَعِينُ ٥ ٱهْدِنَا ٱلصِّرَٰطَ ٱلْمُسْتَقِيمَ ٦ صِرَٰطَ ٱلَّذِينَ أَنْعَمْتَ عَلَيْهِمْ غَيْرِ ٱلْمَغْضُوبِ عَلَيْهِمْ وَلَا ٱلضَّآلِّينَ ";
        Console.WriteLine("Loaded example text from Al-Fatiha.");
    }
    else
    {
        Console.WriteLine("Invalid option. Exiting.");
        return;
    }

    Console.Write($"Enter profile name (default: {profileName}): ");
    var profileInput = Console.ReadLine()?.Trim();
    if (!string.IsNullOrWhiteSpace(profileInput))
    {
        profileName = profileInput;
    }
}

try
{
    Console.WriteLine($"\nUsing profile: {profileName}");
    Console.WriteLine($"Input text: {textToTransliterate}");

    var result = await transliterationService.TransliterateAsync(textToTransliterate!, profileName);

    Console.WriteLine($"\n=== Transliteration Result ===");
    Console.WriteLine(result.TransliteratedText);
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
