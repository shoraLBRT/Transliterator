using System.Reflection;
using Transliterator.Core.Services.Phonology;
using Transliterator.Domain.Entities;
using Xunit;

namespace Transliterator.Tests.ProfileTests
{
    /// <summary>
    /// Инварианты, общие для всех профилей в ресурсах. Профиль правят руками,
    /// и опечатка в нём молча даёт пустую графему: рендерер ищет ключ, не находит
    /// и возвращает пустую строку. Здесь эта тишина превращается в упавший тест.
    /// Как профили пишут весь корпус, проверяет <see cref="ProfileCorpusTests"/>.
    /// </summary>
    public class ProfileCatalogTests
    {
        /// <summary>
        /// Варианты, которые профиль не задаёт нарочно, с причиной. Откат на базовый
        /// ключ у них — решение системы записи, а не забытая строка; проверен он
        /// в <see cref="LatinProfileTests"/>.
        /// </summary>
        private static readonly Dictionary<string, string[]> OmittedVariants = new()
        {
            // Мягкость ляма кириллице приходится дописывать («ль», «ля», «лю»),
            // латинице — нет: «l», «a» и «u» пишутся одинаково в любом слоге.
            ["Latin"] = new[] { "ل|sukun", "َ|soft", "ُ|soft" }
        };

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

        private static IEnumerable<string> VariantKeys(TransliterationProfile profile) =>
            profile.Rules.Keys.Where(k => k.Contains('|'));

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
        public void Profile_DecidesEveryVariantTheOthersSet(string name)
        {
            // Незаданный вариант не ломает вывод — рендерер откатится к базовому
            // ключу, — и потому забытый вариант не виден ни одним другим тестом.
            // Вариант, появившийся в одном профиле, требует решения в каждом:
            // задать его или записать сюда, почему не нужен. Запись, которая
            // перестала быть правдой, роняет тест так же.
            var missing = TestProfiles.All.SelectMany(VariantKeys).Distinct()
                                          .Except(VariantKeys(Get(name)))
                                          .Order(StringComparer.Ordinal);
            var omitted = OmittedVariants.GetValueOrDefault(name, Array.Empty<string>())
                                         .Order(StringComparer.Ordinal);

            Assert.Equal(omitted, missing);
        }

        [Theory]
        [MemberData(nameof(Profiles))]
        public void VariantKey_AlwaysHasItsBaseKey(string name)
        {
            // Вариант — это переопределение базовой графемы. Без базовой ему
            // не на что откатываться, и первое же неучтённое состояние даст пусто.
            var profile = Get(name);

            Assert.All(VariantKeys(profile),
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

            Assert.All(VariantKeys(Get(name)),
                key => Assert.Contains(key[(key.IndexOf('|') + 1)..], known));
        }
    }
}
