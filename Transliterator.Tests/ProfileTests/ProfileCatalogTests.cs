using System.Reflection;
using System.Text;
using Transliterator.Core.Services.Phonology;
using Transliterator.Domain.Entities;
using Xunit;

namespace Transliterator.Tests.ProfileTests
{
    /// <summary>
    /// Инварианты, общие для всех профилей в ресурсах. Профиль правят руками,
    /// и опечатка в нём молча даёт пустую графему: рендерер ищет ключ, не находит
    /// и возвращает пустую строку. Здесь эта тишина превращается в упавший тест.
    /// </summary>
    public class ProfileCatalogTests
    {
        public static TheoryData<string> Profiles()
        {
            var data = new TheoryData<string>();
            foreach (var profile in TestProfiles.All)
                data.Add(profile.Name);
            return data;
        }

        private static TransliterationProfile Get(string name) =>
            TestProfiles.All.Single(p => p.Name == name);

        /// <summary>Ключи без «|» — те, которыми рендерер пользуется всегда.</summary>
        private static IEnumerable<string> BaseKeys(TransliterationProfile profile) =>
            profile.Rules.Keys.Where(k => !k.Contains('|'));

        [Theory]
        [MemberData(nameof(Profiles))]
        public void Profile_CoversEveryBaseKeyTheOthersCover(string name)
        {
            // Базовый ключ — единственное, чего рендерер не может добрать откатом.
            // Профиль без него звук просто не запишет.
            var required = TestProfiles.All.SelectMany(BaseKeys).Distinct();

            Assert.Empty(required.Except(BaseKeys(Get(name))));
        }

        [Theory]
        [MemberData(nameof(Profiles))]
        public void VariantKey_AlwaysHasItsBaseKey(string name)
        {
            // Вариант — это переопределение базовой графемы. Без базовой ему
            // не на что откатываться, и первое же неучтённое состояние даст пусто.
            var profile = Get(name);

            Assert.All(profile.Rules.Keys.Where(k => k.Contains('|')),
                key => Assert.Contains(key[..key.IndexOf('|')], profile.Rules.Keys));
        }

        [Theory]
        [MemberData(nameof(Profiles))]
        public void VariantName_IsOneTheRendererKnows(string name)
        {
            // "ن|ghuna" вместо "ن|ghunna" читается, грузится и не делает ничего.
            // Набор вариантов задаёт рендерер, и сверяться надо с ним.
            var known = typeof(CyrillicRenderer)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.IsLiteral && f.FieldType == typeof(string))
                .Select(f => (string)f.GetRawConstantValue()!)
                .ToHashSet();

            Assert.All(Get(name).Rules.Keys.Where(k => k.Contains('|')),
                key => Assert.Contains(key[(key.IndexOf('|') + 1)..], known));
        }

        [Theory]
        [MemberData(nameof(Profiles))]
        public void Profile_RendersTheWholeFatiha(string name)
        {
            // Профиль, забытый в csproj, до выходной папки не доезжает вовсе.
            // А цифры и всё, чего рендерер не опознал, он отдаёт как есть —
            // и незаданный ключ виден в выводе непереведённой арабской графемой.
            var result = TransliterationPipeline.Transliterate(Fatiha, Get(name));

            Assert.False(string.IsNullOrWhiteSpace(result));
            Assert.DoesNotContain(result, c => c is >= '\u0600' and <= '\u06FF');
        }

        [Theory]
        [MemberData(nameof(Profiles))]
        public void Profile_ChangesTheLettersButNotTheStructure(string name)
        {
            // Тот самый инвариант проекта в виде проверки: где кончается слово,
            // где стоит дефис слияния и где идёт номер аята — решает конвейер.
            // Профиль вправе поменять каждую графему и не вправе сдвинуть ни одну
            // границу. Всё, что не пробел и не дефис, здесь схлопнуто в «·».
            Assert.Equal(Skeleton(TransliterationPipeline.Transliterate(Fatiha)),
                         Skeleton(TransliterationPipeline.Transliterate(Fatiha, Get(name))));
        }

        private static string Skeleton(string rendered)
        {
            var skeleton = new StringBuilder();

            foreach (var c in rendered)
            {
                if (c is ' ' or '-')
                    skeleton.Append(c);
                else if (skeleton.Length == 0 || skeleton[^1] != '·')
                    skeleton.Append('·');
            }

            return skeleton.ToString();
        }

        private const string Fatiha =
            "بِسْمِ ٱللَّهِ ٱلرَّحْمَـٰنِ ٱلرَّحِيمِ ١ ٱلْحَمْدُ لِلَّهِ رَبِّ ٱلْعَـٰلَمِينَ ٢ " +
            "ٱلرَّحْمَـٰنِ ٱلرَّحِيمِ ٣ مَـٰلِكِ يَوْمِ ٱلدِّينِ ٤ إِيَّاكَ نَعْبُدُ وَإِيَّاكَ نَسْتَعِينُ ٥ " +
            "ٱهْدِنَا ٱلصِّرَٰطَ ٱلْمُسْتَقِيمَ ٦ صِرَٰطَ ٱلَّذِينَ أَنْعَمْتَ عَلَيْهِمْ غَيْرِ ٱلْمَغْضُوبِ عَلَيْهِمْ وَلَا ٱلضَّآلِّينَ";
    }
}
