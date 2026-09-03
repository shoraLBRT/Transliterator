using System.Text;

namespace Transliterator.Core.Services.Phonology
{
    /// <summary>
    /// Стадия 1 конвейера: нормализация орфографии.
    /// <para>
    /// Приводит текст к одному представлению до того, как его начнут разбирать:
    /// NFC, снятие татвиля, сведение вариантов сукуна, разворачивание малых
    /// восстановительных букв и удаление знаков, не несущих звука.
    /// </para>
    /// </summary>
    public class ArabicNormalizer
    {
        public string Normalize(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            var source = text.Normalize(NormalizationForm.FormC);
            var result = new StringBuilder(source.Length);

            foreach (var c in source)
            {
                switch (c)
                {
                    // Татвиль — только соединительная черта. Диакритика, которую он нёс
                    // (например в "ـٰ"), после его снятия переходит на предыдущую букву.
                    case ArabicScript.Tatweel:
                        continue;

                    case ArabicScript.QuranicSukun:
                        result.Append(ArabicScript.Sukun);
                        continue;

                    // Малые восстановительные буквы обозначают долготу, которую
                    // орфография не пишет: دَاوُۥدَ. Разворачиваем в обычные буквы.
                    case ArabicScript.SmallWaw:
                        result.Append(ArabicScript.Waw);
                        continue;

                    case ArabicScript.SmallYa:
                        result.Append(ArabicScript.Yeh);
                        continue;
                }

                // Две пометы нормализация оставляет, всё прочее выбрасывает.
                // Знак вакфа говорит стадии 3, где чтец останавливается; знак икляба
                // (ۢ) стоит в мусхафе вместо сукуна и потому не помета чтеца, а
                // единственный признак нун сакины в مِنۢ بَعْدِ — его читает стадия 6.
                // Остальное (разделители аятов, пометы чтеца) звука не несёт.
                if (ArabicScript.IsWaqfMark(c) || ArabicScript.IsIqlabMark(c))
                {
                    result.Append(c);
                    continue;
                }

                if (ArabicScript.IsRecitationMark(c))
                    continue;

                if (char.IsControl(c) && !char.IsWhiteSpace(c))
                    continue;

                result.Append(c);
            }

            return CollapseWhitespace(result.ToString());
        }

        private static string CollapseWhitespace(string text)
        {
            var result = new StringBuilder(text.Length);
            bool inWhitespace = false;

            foreach (var c in text)
            {
                if (char.IsWhiteSpace(c))
                {
                    if (!inWhitespace && result.Length > 0)
                        result.Append(' ');
                    inWhitespace = true;
                    continue;
                }

                inWhitespace = false;
                result.Append(c);
            }

            return result.ToString().TrimEnd();
        }
    }
}
