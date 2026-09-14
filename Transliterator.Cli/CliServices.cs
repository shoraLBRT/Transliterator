using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Transliterator.Core.Models;
using Transliterator.Core.Repositories;
using Transliterator.Core.Services.Phonology;
using Transliterator.Core.Services.Rules;
using Transliterator.Domain.Interfaces;

namespace Transliterator.Cli
{
    /// <summary>
    /// Сборка сервисов CLI. Вынесена из Program, чтобы тесты гоняли CLI той же
    /// сборкой, что и настоящий запуск: с файловыми хранилищами профилей и корпуса.
    /// </summary>
    public static class CliServices
    {
        public static IServiceCollection AddTransliteratorCli(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<StorageSettings>(configuration.GetSection("StorageSettings"));

            services.AddTransient<IProfileRepository, JsonProfileRepository>();
            services.AddTransient<ICorpusRepository, JsonCorpusRepository>();
            services.AddTransient<ITransliterationService, TransliterationService>();
            services.AddTransient<CliApp>();

            // Стадии конвейера
            services.AddTransient<ArabicNormalizer>();
            services.AddTransient<ArabicParser>();
            services.AddTransient<CyrillicRenderer>();

            // Правила таджвида
            services.AddTransient<RulesService>();
            services.AddTransient<WaqfRule>();
            services.AddTransient<WaslRule>();
            services.AddTransient<ArticleRule>();
            services.AddTransient<AssimilationRule>();
            services.AddTransient<NasalRule>();
            services.AddTransient<EmphasisRule>();
            services.AddTransient<MaddRule>();
            services.AddTransient<QalqalahRule>();

            return services;
        }
    }
}
