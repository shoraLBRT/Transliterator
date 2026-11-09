using System.Reflection;
using Transliterator.Core.Services;
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
        public static TheoryData<string> Profiles()
        {
            var data = new TheoryData<string>();
            foreach (var profile in TestProfiles.All)
                data.Add(profile.Name);
            return data;
        }

        private static TransliterationProfile Get(string name) =>
            TestProfiles.All.Single(p => p.Name == name);

        private static IEnumerable<string> VariantKeys(TransliterationProfile profile) =>
            profile.Rules.Keys.Where(k => k.Contains('|'));

        [Theory]
        [MemberData(nameof(Profiles))]
        public void Profile_HasEveryRequiredKey(string name) =>
            // Встроенный профиль проверяется по обязательному набору (F6), а не по
            // другим профилям: «необходимое» — решение, а не то, что оказалось в Standard.
            Assert.Empty(RequiredProfileKeys.Missing(Get(name).Rules));

        [Theory]
        [MemberData(nameof(Profiles))]
        public void Profile_HasNoKeyWithoutADecision(string name) =>
            // Каждый ключ профиля — обязательный, цифра или вариант, который вправе
            // отсутствовать и у которого записано, что тогда будет. Новый вариант
            // без такого решения роняет тест: забытый вариант иначе не виден —
            // рендерер молча откатится к базовому ключу.
            Assert.All(Get(name).Rules.Keys, key => Assert.True(RequiredProfileKeys.IsDecided(key), key));

        [Fact]
        public void EveryRendererVariant_IsRequiredOrOptionalWithAReason()
        {
            // Вариант, который рендерер узнал, требует решения в наборе: обязателен
            // он или нет, и что будет без него.
            var known = RendererVariants();
            var required = RequiredProfileKeys.Variants.Select(k => k[(k.IndexOf('|') + 1)..]);

            Assert.Equal(known.Order(), RequiredProfileKeys.OptionalVariants.Keys.Concat(required).Distinct().Order());
            Assert.All(RequiredProfileKeys.OptionalVariants.Values, reason => Assert.False(string.IsNullOrWhiteSpace(reason)));
        }

        [Fact]
        public void RequiredSet_IsLettersVowelsAndPausalTaMarbuta() =>
            // Набор целиком: 30 букв, три огласовки и та-марбута на паузе. Правка
            // набора — решение владельца, и этот тест держит её на виду.
            Assert.Equal(34, RequiredProfileKeys.All.Count);

        private static HashSet<string> RendererVariants() =>
            typeof(CyrillicRenderer)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.IsLiteral && f.FieldType == typeof(string))
                .Select(f => (string)f.GetRawConstantValue()!)
                .ToHashSet();

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
