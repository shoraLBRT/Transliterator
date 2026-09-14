using System.Text;
using Transliterator.Core.Services.Phonology;
using Transliterator.Domain.Entities;
using Transliterator.Tests.CorpusTests;
using Xunit;

namespace Transliterator.Tests.ProfileTests
{
    /// <summary>
    /// Весь корпус через каждый встроенный профиль (F4). Ожидание корпуса записано
    /// только для Standard, и сверять Latin с ним буква в букву нечем. Зато можно
    /// проверить то, ради чего второй профиль заведён: правила не знают письма.
    /// Если конвейер где-то пишет графему сам, а не берёт её из профиля, в выводе
    /// другого профиля она окажется чужой буквой; если профиль где-то двигает
    /// границу, разойдётся скелет вывода.
    /// <para>
    /// Случай — пара «профиль, случай корпуса», и имя в отчёте читается так же,
    /// как у прогона A4: <c>Latin, 114:5</c>.
    /// </para>
    /// </summary>
    public class ProfileCorpusTests
    {
        public static TheoryData<string, string> Cases()
        {
            var data = new TheoryData<string, string>();

            foreach (var profile in TestProfiles.All)
                foreach (var row in CorpusCases.Ayahs.Concat(CorpusCases.Surahs))
                    data.Add(profile.Name, (string)row[0]);

            return data;
        }

        private static TransliterationProfile Get(string name) =>
            TestProfiles.All.Single(p => p.Name == name);

        private static string Arabic(string id) =>
            id.StartsWith("сура ") ? CorpusCases.Surah(id).Arabic : CorpusCases.Ayah(id).Arabic;

        [Theory]
        [MemberData(nameof(Cases))]
        public void Output_IsWrittenOnlyWithTheProfilesOwnLetters(string name, string id)
        {
            // Буква вывода, которой нет ни в одной графеме профиля, пришла не из
            // профиля: её дописал конвейер или пропустил рендерер, не найдя ключа.
            // Непереведённая арабица ловится этим же: её в графемах нет.
            var profile = Get(name);
            var result = TransliterationPipeline.Transliterate(Arabic(id), profile);

            var own = profile.Rules.Values.SelectMany(v => v).ToHashSet();
            var foreign = result.Where(c => c is not (' ' or '-' or '\n') && !own.Contains(c))
                                .Distinct()
                                .Select(c => $"'{c}' (U+{(int)c:X4})")
                                .ToList();

            Assert.False(string.IsNullOrWhiteSpace(result), $"{name}, {id}: пустой вывод");
            Assert.True(foreign.Count == 0,
                        $"{name}, {id}: в выводе буквы, которых нет в профиле: {string.Join(", ", foreign)}\n  вывод: {result}");
        }

        [Theory]
        [MemberData(nameof(Cases))]
        public void Output_KeepsTheStructureOfStandard(string name, string id)
        {
            // Тот самый инвариант проекта в виде проверки: где кончается слово,
            // где стоит дефис слияния и где идёт номер аята — решает конвейер.
            // Профиль вправе поменять каждую графему и не вправе сдвинуть ни одну
            // границу. Всё, что не пробел и не дефис, здесь схлопнуто в «·».
            //
            // Разделитель хамзы между гласными — не граница, а графема: Standard
            // пишет его дефисом, Latin — той же ʾ (B7). Чтобы он не сошёл здесь
            // за структуру, вариант снят с обоих профилей и берётся базовый ключ.
            var arabic = Arabic(id);
            var standard = Skeleton(TransliterationPipeline.Transliterate(arabic, WithoutHiatus(TestProfiles.Standard)));
            var actual = Skeleton(TransliterationPipeline.Transliterate(arabic, WithoutHiatus(Get(name))));

            Assert.True(standard == actual,
                        $"{name}, {id}: границы разошлись со Standard\n  Standard: {standard}\n  {name}: {actual}");
        }

        private static TransliterationProfile WithoutHiatus(TransliterationProfile profile) =>
            new(profile.Name, profile.Description)
            {
                Rules = profile.Rules
                    .Where(rule => !rule.Key.EndsWith("|" + CyrillicRenderer.HiatusVariant))
                    .ToDictionary(rule => rule.Key, rule => rule.Value)
            };

        private static string Skeleton(string rendered)
        {
            var skeleton = new StringBuilder();

            foreach (var c in rendered)
            {
                if (c is ' ' or '-' or '\n')
                    skeleton.Append(c);
                else if (skeleton.Length == 0 || skeleton[^1] != '·')
                    skeleton.Append('·');
            }

            return skeleton.ToString();
        }
    }
}
