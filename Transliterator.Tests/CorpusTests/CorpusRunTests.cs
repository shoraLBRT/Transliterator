using System.Text;
using Xunit;

namespace Transliterator.Tests.CorpusTests
{
    /// <summary>
    /// Прогон корпуса через конвейер. Проверяет не отдельное правило, а вывод
    /// целиком, и потому говорит «сломалось», а не «сломалось вот это»: имя
    /// правила называют точечные тесты в <c>TajweedRulesTests</c>. Нужны оба —
    /// точечный тест не заметит правку, которая ломает соседнее правило.
    /// <para>
    /// Случаи строятся из файлов корпуса, а не из списка в коде: сура, добавленная
    /// в ресурсы, добавляет тесты сама. Расхождения, за которыми стоит заведённый
    /// баг, перечислены в <see cref="KnownDivergences"/> — там же написано, почему
    /// они не оставлены просто красными.
    /// </para>
    /// </summary>
    public class CorpusRunTests
    {
        [Theory]
        [MemberData(nameof(CorpusCases.Ayahs), MemberType = typeof(CorpusCases))]
        public void Ayah(string id) => Check(id, CorpusCases.Ayah(id));

        [Theory]
        [MemberData(nameof(CorpusCases.Surahs), MemberType = typeof(CorpusCases))]
        public void Surah(string id) => Check(id, CorpusCases.Surah(id));

        /// <summary>
        /// Реестр расхождений — временный, и стареет он молча: аят переписали
        /// в корпусе или переименовали случай, а запись осталась и продолжает
        /// разрешать расхождение, которого уже нет ни у кого.
        /// </summary>
        [Fact]
        public void KnownDivergences_AllPointAtCasesThatExist()
        {
            var stale = KnownDivergences.Entries.Keys.Where(id => !CorpusCases.IsKnownCase(id)).ToList();

            Assert.True(stale.Count == 0,
                        "В реестре расхождений записи без случая в корпусе: " + string.Join(", ", stale));
        }

        private static void Check(string id, CorpusCases.Case corpusCase)
        {
            var actual = TransliterationPipeline.Transliterate(corpusCase.Arabic);

            if (!KnownDivergences.TryGet(id, out var known))
            {
                if (actual != corpusCase.Expected)
                    Assert.Fail(Compare(id, "вывод не совпал с ожиданием корпуса", corpusCase.Expected, actual));

                return;
            }

            // Баг закрыт — падаем нарочно. Иначе запись переживёт правку и будет
            // разрешать расхождение, которого больше нет.
            if (actual == corpusCase.Expected)
                Assert.Fail($"{id}: вывод сошёлся с ожиданием корпуса — {known.Bug} починен. " +
                            "Удалите запись из KnownDivergences и отметьте баг в docs/BACKLOG.md.");

            if (actual != known.Actual)
                Assert.Fail(Compare(id, $"с ожиданием корпуса не сошлось ({known.Bug}), но и записанный вывод изменился",
                                    known.Actual, actual));
        }

        /// <summary>
        /// Сравнение посимвольное: у транслитерации расхождение — это одна буква
        /// в середине длинной строки, и глазами её не находят. В сообщении обе
        /// строки целиком, номер символа и обе буквы с кодами: «а» кириллическая
        /// и «a» латинская иначе неотличимы.
        /// </summary>
        private static string Compare(string id, string headline, string expected, string actual)
        {
            int i = 0;
            while (i < expected.Length && i < actual.Length && expected[i] == actual[i])
                i++;

            return new StringBuilder()
                .AppendLine($"{id}: {headline}; первое расхождение в символе {i + 1}.")
                .AppendLine($"  ожидание: {expected}")
                .AppendLine($"  вывод:    {actual}")
                .AppendLine($"            {new string(' ', i)}^")
                .Append($"  ожидалось {Describe(expected, i)}, получено {Describe(actual, i)}")
                .ToString();
        }

        private static string Describe(string text, int index) =>
            index < text.Length ? $"'{text[index]}' (U+{(int)text[index]:X4})" : "конец строки";
    }
}
