using System.Globalization;
using Microsoft.Extensions.Localization;
using Transliterator.Domain.Localization;

namespace Transliterator.Web.Localization
{
    /// <summary>
    /// Строки интерфейса страницы (D7) — <c>Strings.resx</c> (русский, нейтральный язык
    /// сборки) и <c>Strings.&lt;язык&gt;.resx</c> для остальных языков страницы. Класс
    /// пустой: он только имя ресурса для <see cref="IStringLocalizer{T}"/>, которым
    /// компоненты берут строки (<c>@L["Mode.Full"]</c>).
    /// <para>
    /// Тест сверяет ключи всех языков и ключи, которые берёт разметка, с ресурсом:
    /// строка без перевода или ключ, которого нет, роняют его. Текст по-русски прямо
    /// в разметке тоже роняет — иначе он остался бы русским в любом интерфейсе.
    /// </para>
    /// </summary>
    public sealed class Strings
    {
    }

    public static class StringLocalizerExtensions
    {
        /// <summary>«13 правил», «1 rule» — на языке интерфейса.</summary>
        public static string Count(this IStringLocalizer localizer, string prefix, int count) =>
            localizer[Plural.Key(prefix, count, CultureInfo.CurrentUICulture), count];
    }
}
