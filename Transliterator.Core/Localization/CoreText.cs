using System.Globalization;
using System.Resources;

namespace Transliterator.Core.Localization
{
    /// <summary>
    /// Тексты ядра, которые доходят до человека (D7): пояснения вариантов графем,
    /// сообщения редактора профилей. Ресурсы <c>CoreText.resx</c> (русский — нейтральный
    /// язык сборки) и <c>CoreText.&lt;язык&gt;.resx</c>; язык берётся из культуры
    /// интерфейса. Тест сверяет ключи всех языков: строка без перевода роняет его.
    /// </summary>
    public static class CoreText
    {
        /// <summary>Имя ресурса: корневое пространство имён и путь к файлу.</summary>
        public const string ResourceName = "Transliterator.Core.Localization.CoreText";

        private static readonly ResourceManager Resources = new(ResourceName, typeof(CoreText).Assembly);

        /// <summary>Строка на языке интерфейса. Нет ключа — сломанная сборка, а не пустое место.</summary>
        public static string Get(string key) =>
            Resources.GetString(key, CultureInfo.CurrentUICulture)
            ?? throw new MissingManifestResourceException($"Core text '{key}' is missing from {ResourceName}");

        public static string Format(string key, params object?[] args) =>
            string.Format(CultureInfo.CurrentCulture, Get(key), args);
    }
}
