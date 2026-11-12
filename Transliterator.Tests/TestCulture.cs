using System.Globalization;
using System.Runtime.CompilerServices;

namespace Transliterator.Tests
{
    /// <summary>
    /// Язык интерфейса в тестах (D7). Тексты ядра берутся из ресурсов на языке
    /// культуры интерфейса, и без этой строки тест, который ждёт русское сообщение,
    /// проходил бы на машине с русской Windows и падал бы на машине с английской.
    /// Русский — язык страницы по умолчанию; тест другого языка ставит его сам
    /// (<see cref="Use"/>).
    /// </summary>
    internal static class TestCulture
    {
        [ModuleInitializer]
        internal static void UseTheDefaultPageLanguage()
        {
            var russian = CultureInfo.GetCultureInfo("ru");
            CultureInfo.DefaultThreadCurrentCulture = russian;
            CultureInfo.DefaultThreadCurrentUICulture = russian;
        }

        /// <summary>Культура интерфейса на время блока: <c>using var _ = TestCulture.Use("en");</c>.</summary>
        internal static IDisposable Use(string language) => new Scope(CultureInfo.GetCultureInfo(language));

        private sealed class Scope : IDisposable
        {
            private readonly CultureInfo _culture = CultureInfo.CurrentCulture;
            private readonly CultureInfo _uiCulture = CultureInfo.CurrentUICulture;

            public Scope(CultureInfo culture)
            {
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;
            }

            public void Dispose()
            {
                CultureInfo.CurrentCulture = _culture;
                CultureInfo.CurrentUICulture = _uiCulture;
            }
        }
    }
}
