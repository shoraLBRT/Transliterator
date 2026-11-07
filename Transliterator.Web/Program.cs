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
// Свои — из localStorage (E1), и страница снова даёт их создавать и править (E4).
// Хранилище одно на оба вида: встроенный профиль в нём не перезаписывается,
// а правка его сохраняется копией.
builder.Services.AddSingleton<EmbeddedProfileRepository>();
builder.Services.AddSingleton<IKeyValueStore, LocalStorageStore>();
builder.Services.AddSingleton(services => new UserProfileRepository(
    services.GetRequiredService<EmbeddedProfileRepository>(),
    services.GetRequiredService<IKeyValueStore>(),
    services.GetRequiredService<ILogger<UserProfileRepository>>()));
builder.Services.AddSingleton<IProfileRepository>(services => services.GetRequiredService<UserProfileRepository>());
builder.Services.AddSingleton<ProfileEditor>();

// Корпус — тоже из ресурсов (C3): готовые суры и прогон тестов читают одни и те же файлы.
builder.Services.AddSingleton<ICorpusRepository, EmbeddedCorpusRepository>();
builder.Services.AddTransient<ITransliterationService, TransliterationService>();

// Обратная связь (H3): адрес функции и ключ Turnstile — из wwwroot/appsettings.json.
// Пока они пусты, кнопка отправляет ссылкой на форму GitHub (H1).
builder.Services.AddSingleton(new FeedbackSettings(
    builder.Configuration["Feedback:Endpoint"],
    builder.Configuration["Feedback:TurnstileSiteKey"]));
builder.Services.AddSingleton(_ => new FeedbackClient(new HttpClient()));

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
