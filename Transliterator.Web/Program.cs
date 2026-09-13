// Transliterator.Web/Program.cs
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Transliterator.Core.Repositories;
using Transliterator.Core.Services;
using Transliterator.Core.Services.Phonology;
using Transliterator.Core.Services.Rules;
using Transliterator.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Transliterator.Web;
using Transliterator.Web.Storage;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");

// Встроенные профили — из ресурсов сборки: каталога рядом со сборкой в браузере нет (C1).
// Свои — из localStorage (E1). Страница видит их одним хранилищем: встроенный
// профиль там не перезаписывается, а правка его сохраняется копией.
builder.Services.AddSingleton<EmbeddedProfileRepository>();
builder.Services.AddSingleton<IKeyValueStore, LocalStorageStore>();
builder.Services.AddSingleton(services => new UserProfileRepository(
    services.GetRequiredService<EmbeddedProfileRepository>(),
    services.GetRequiredService<IKeyValueStore>(),
    services.GetRequiredService<ILogger<UserProfileRepository>>()));
builder.Services.AddSingleton<IProfileRepository>(services => services.GetRequiredService<UserProfileRepository>());
builder.Services.AddSingleton<ProfileEditor>();

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
