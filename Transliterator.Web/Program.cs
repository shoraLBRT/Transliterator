// Transliterator.Web/Program.cs
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Transliterator.Core.Repositories;
using Transliterator.Core.Services.Phonology;
using Transliterator.Core.Services.Rules;
using Transliterator.Domain.Interfaces;
using Transliterator.Web;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");

// Профили — только из ресурсов сборки: каталога рядом со сборкой в браузере нет (C1).
// Одно хранилище на страницу: ресурсы читаются при его создании, и читать их
// заново на каждый пересчёт незачем.
builder.Services.AddSingleton<IProfileRepository, EmbeddedProfileRepository>();

// Корпус — тоже из ресурсов (C3): панель сур и прогон тестов читают одни и те же файлы.
builder.Services.AddSingleton<ICorpusRepository, EmbeddedCorpusRepository>();
builder.Services.AddTransient<ITransliterationService, TransliterationService>();

// Стадии конвейера
builder.Services.AddTransient<ArabicNormalizer>();
builder.Services.AddTransient<ArabicParser>();
builder.Services.AddTransient<CyrillicRenderer>();

// Правила таджвида
builder.Services.AddTransient<RulesService>();
builder.Services.AddTransient<WaqfRule>();
builder.Services.AddTransient<WaslRule>();
builder.Services.AddTransient<ArticleRule>();
builder.Services.AddTransient<AssimilationRule>();
builder.Services.AddTransient<NasalRule>();
builder.Services.AddTransient<EmphasisRule>();
builder.Services.AddTransient<MaddRule>();
builder.Services.AddTransient<QalqalahRule>();

await builder.Build().RunAsync();
