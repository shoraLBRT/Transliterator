using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Transliterator.Core.Models;
using Transliterator.Core.Repositories;
using Transliterator.Domain.Entities;

namespace Transliterator.Tests.CorpusTests
{
    /// <summary>
    /// Случаи прогона, построенные из файлов корпуса. Список случаев нигде
    /// не записан руками: сура, положенная в <c>Resources/Corpus</c>, приносит
    /// свои случаи сама, а забытая в csproj не приносит ничего — на это есть
    /// отдельная проверка в <see cref="CorpusLoaderTests"/>.
    /// <para>
    /// Идентификатор случая — он же имя в отчёте о прогоне, он же ключ
    /// в <see cref="KnownDivergences"/>. Короткий нарочно: по <c>110:2</c>
    /// сура и аят видны без расшифровки, и по упавшему тесту сразу понятно,
    /// какую строку корпуса открывать.
    /// </para>
    /// </summary>
    public static class CorpusCases
    {
        /// <summary>Хвост идентификатора у случая на современном написании.</summary>
        public const string ImlaiSuffix = " имля";

        private static readonly Lazy<IReadOnlyList<CorpusSurah>> _corpus = new(Load);
        private static readonly Lazy<Dictionary<string, Case>> _ayahs = new(BuildAyahs);
        private static readonly Lazy<Dictionary<string, Case>> _surahs = new(BuildSurahs);

        /// <param name="Arabic">Вход конвейера.</param>
        /// <param name="Expected">Ожидание корпуса — то, что в файле, без подстановок.</param>
        public sealed record Case(string Arabic, string Expected);

        /// <summary>Аяты обоих написаний: <c>110:2</c> и <c>110:2 имля</c>.</summary>
        public static IEnumerable<object[]> Ayahs => _ayahs.Value.Keys.Select(id => new object[] { id });

        /// <summary>Суры целиком, одной строкой: <c>сура 110</c> и <c>сура 110 имля</c>.</summary>
        public static IEnumerable<object[]> Surahs => _surahs.Value.Keys.Select(id => new object[] { id });

        public static Case Ayah(string id) => _ayahs.Value[id];

        public static Case Surah(string id) => _surahs.Value[id];

        public static bool IsKnownCase(string id) => _ayahs.Value.ContainsKey(id) || _surahs.Value.ContainsKey(id);

        private static Dictionary<string, Case> BuildAyahs()
        {
            var cases = new Dictionary<string, Case>();

            foreach (var surah in _corpus.Value)
                foreach (var ayah in surah.Ayahs)
                {
                    cases[$"{surah.Number}:{ayah.Number}"] = new Case(ayah.Arabic, ayah.Expected);

                    // Второе написание — не копия входа, а вторая ветка разбора:
                    // васлю там приходится опознавать, а не читать готовым знаком.
                    // Ожидание у пары одно: орфография — способ записи, не чтения.
                    if (!string.IsNullOrEmpty(ayah.ArabicImlai))
                        cases[$"{surah.Number}:{ayah.Number}{ImlaiSuffix}"] = new Case(ayah.ArabicImlai, ayah.Expected);
                }

            return cases;
        }

        /// <summary>
        /// Сура одной строкой: аяты подряд, между ними номер аята арабскими
        /// цифрами — так текст и напечатан в мусхафе, и так его копирует
        /// пользователь.
        /// <para>
        /// Ожидание складывается из ожиданий аятов, а у известного расхождения —
        /// из его записанного вывода. Проверяет этот случай поэтому не правила
        /// (их проверил поаятный прогон), а стыки: что пауза в конце аята есть,
        /// что номер не съеден и не склеился с соседним словом, что следующий аят
        /// начинается как начало высказывания, а не как середина.
        /// </para>
        /// <para>
        /// Из подстановки следует, что закрытый баг красит сразу два случая: аят
        /// и его суру. Второй гаснет тем же удалением записи из реестра, что
        /// и первый, — отдельно править здесь нечего.
        /// </para>
        /// </summary>
        private static Dictionary<string, Case> BuildSurahs()
        {
            var cases = new Dictionary<string, Case>();

            foreach (var surah in _corpus.Value)
            {
                cases[$"сура {surah.Number}"] = Join(surah, imlai: false);

                // Сура без единого второго написания на второй прогон даёт тот же
                // вход: случай выглядел бы покрытием, а разбирал бы то же самое.
                if (surah.Ayahs.Any(a => !string.IsNullOrEmpty(a.ArabicImlai)))
                    cases[$"сура {surah.Number}{ImlaiSuffix}"] = Join(surah, imlai: true);
            }

            return cases;
        }

        private static Case Join(CorpusSurah surah, bool imlai)
        {
            var arabic = new List<string>();
            var expected = new List<string>();

            foreach (var ayah in surah.Ayahs)
            {
                var spelling = imlai && !string.IsNullOrEmpty(ayah.ArabicImlai) ? ayah.ArabicImlai : ayah.Arabic;
                var id = $"{surah.Number}:{ayah.Number}";

                if (imlai && !string.IsNullOrEmpty(ayah.ArabicImlai))
                    id += ImlaiSuffix;

                arabic.Add($"{spelling} {ArabicDigits(ayah.Number)}");
                expected.Add($"{KnownDivergences.ExpectedToday(id, ayah.Expected)} {ayah.Number}");
            }

            return new Case(string.Join(' ', arabic), string.Join(' ', expected));
        }

        private static string ArabicDigits(int number) =>
            new(number.ToString().Select(c => (char)('٠' + (c - '0'))).ToArray());

        /// <summary>
        /// Тот же корпус, что уедет в вывод сборки, и та же реализация чтения,
        /// что у приложения: копия корпуса в тестах разошлась бы с оригиналом
        /// ровно так, как разошёлся с ним хардкод-профиль.
        /// </summary>
        private static IReadOnlyList<CorpusSurah> Load() =>
            new JsonCorpusRepository(
                    Options.Create(new StorageSettings { CorpusPath = Path.Combine(AppContext.BaseDirectory, "Corpus") }),
                    NullLogger<JsonCorpusRepository>.Instance)
                .GetAllSurahsAsync()
                .GetAwaiter()
                .GetResult();
    }
}
