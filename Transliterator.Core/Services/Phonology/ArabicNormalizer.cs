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

            return RestoreNameOfAllah(CollapseWhitespace(result.ToString()));
        }

        /// <summary>
        /// Восстанавливает надстрочный алиф в имени Аллаха. В современной орфографии
        /// его не пишут — «ٱللَّهُ» вместо «ٱللَّٰهُ», — а долготу в этом слове даёт
        /// только он: своей буквы у неё нет.
        /// <para>
        /// Без алифа слог لَّ остаётся кратким, и дальше рушится всё, что на эту
        /// долготу опирается: стадия 7 не узнаёт лям имени Аллаха, а стадия 8
        /// видит перед конечной ه краткую огласовку и принимает коренную ه
        /// за местоименную — со всем мадд силя впридачу.
        /// </para>
        /// <para>
        /// Чиним здесь, а не в правилах, по той же причине, по которой разбор
        /// восстанавливает артикль в «الحمد» (<c>ArabicParser.DetectImlaiWasl</c>):
        /// дальше по конвейеру должно доехать одно написание, а не два.
        /// </para>
        /// </summary>
        private static string RestoreNameOfAllah(string text)
        {
            var result = new StringBuilder(text.Length);

            for (int i = 0; i < text.Length; i++)
            {
                result.Append(text[i]);

                if (!IsNameOfAllahLam(text, i, out int marksEnd))
                    continue;

                result.Append(text, i + 1, marksEnd - i - 1);
                result.Append(ArabicScript.SuperscriptAlef);
                i = marksEnd - 1;
            }

            return result.ToString();
        }

        /// <summary>
        /// Удвоенный лям имени Аллаха: «لَّ» с фатхой, за которым сразу идёт ه.
        /// Возвращает через <paramref name="marksEnd"/> место сразу за диакритикой
        /// ляма — туда и встанет алиф, чтобы порядок знаков остался каноническим
        /// (фатха, шадда, надстрочный алиф).
        /// </summary>
        private static bool IsNameOfAllahLam(string text, int index, out int marksEnd)
        {
            marksEnd = index + 1;
            if (text[index] != ArabicScript.Lam)
                return false;

            bool hasShadda = false;
            bool hasFatha = false;

            while (marksEnd < text.Length && ArabicScript.IsDiacritic(text[marksEnd]))
            {
                // Написание усмани: алиф уже на месте, восстанавливать нечего.
                if (text[marksEnd] == ArabicScript.SuperscriptAlef)
                    return false;

                hasShadda |= text[marksEnd] == ArabicScript.Shadda;
                hasFatha |= text[marksEnd] == ArabicScript.Fatha;
                marksEnd++;
            }

            // Огласовки восстанавливать не берёмся: без шадды и фатхи это
            // неогласованный текст, а его конвейер и так не читает.
            if (!hasShadda || !hasFatha)
                return false;

            if (marksEnd >= text.Length || text[marksEnd] != ArabicScript.Ha)
                return false;

            return IsPrecededByArticleOrPrefixLam(text, index);
        }

        /// <summary>
        /// Слева от удвоенного ляма должно стоять то, что и делает слово именем
        /// Аллаха: лям артикля после алифа (ٱللَّه, بِٱللَّه, ٱللَّهُمَّ) или лям
        /// предлога с касрой (لِلَّه).
        /// <para>
        /// Одного «لَّه» мало: в «قُل لَّهُ» тот же لَّهُ — это «ему», и никакой
        /// долготы в нём нет.
        /// </para>
        /// </summary>
        private static bool IsPrecededByArticleOrPrefixLam(string text, int index)
        {
            int lam = PreviousLetter(text, index);
            if (lam < 0 || text[lam] != ArabicScript.Lam)
                return false;

            if (HasMark(text, lam, ArabicScript.Kasra))
                return true;

            int article = PreviousLetter(text, lam);
            return article >= 0
                   && text[article] is ArabicScript.Alef or ArabicScript.AlefWasla;
        }

        /// <summary>Предыдущий носитель: диакритика своей позицией в слове не считается.</summary>
        private static int PreviousLetter(string text, int index)
        {
            int i = index - 1;
            while (i >= 0 && ArabicScript.IsDiacritic(text[i]))
                i--;

            return i;
        }

        private static bool HasMark(string text, int letterIndex, char mark)
        {
            for (int i = letterIndex + 1; i < text.Length && ArabicScript.IsDiacritic(text[i]); i++)
                if (text[i] == mark)
                    return true;

            return false;
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
