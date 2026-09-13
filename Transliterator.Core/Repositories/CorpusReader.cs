using System.Text.Json;
using Transliterator.Core.Services.Phonology;
using Transliterator.Domain.Entities;
using Transliterator.Domain.Exceptions;

namespace Transliterator.Core.Repositories
{
    /// <summary>
    /// Разбор и проверка одной суры корпуса — общие у всех хранилищ корпуса.
    /// Хранилище решает, <b>где лежит</b> сура (каталог, ресурс сборки), а что
    /// считать битой сурой, решается здесь один раз: иначе корпус, которого
    /// каталог не принял бы, спокойно доехал бы до браузера через ресурс.
    /// </summary>
    internal static class CorpusReader
    {
        /// <summary>Имя файла суры: номер тремя цифрами.</summary>
        public static string FileNameFor(int number) => $"{number:000}.json";

        /// <param name="stream">Содержимое файла суры.</param>
        /// <param name="source">Откуда сура взялась, целиком — для сообщения об ошибке.</param>
        /// <param name="fileName">Имя файла суры без пути: сверяется с её номером.</param>
        /// <param name="kind">Как назвать источник в сообщении: «Corpus file», «Embedded corpus resource».</param>
        public static async Task<CorpusSurah> ReadAsync(Stream stream, string source, string fileName, string kind)
        {
            CorpusSurah? surah;

            try
            {
                surah = await JsonSerializer.DeserializeAsync<CorpusSurah>(stream);
            }
            catch (JsonException ex)
            {
                throw new TransliterationException($"{kind} is not valid JSON: {source}", ex);
            }

            if (surah is null)
                throw new TransliterationException($"{kind} is empty: {source}");

            Validate(surah, fileName);

            return surah;
        }

        /// <summary>
        /// Проверяет ровно то, на что опираются читатели корпуса: номер, по которому
        /// сура ищется, названия для панели примеров, редакцию текста и сплошную
        /// нумерацию аятов. Пропущенный аят — это не «корпус поменьше», а дыра
        /// в покрытии, и увидеть её надо при чтении, а не в отчёте о прогоне.
        /// <para>
        /// Пару написаний проверяем с обеих сторон. Аят с васлей без современного
        /// написания — молча непроверенная ветка <c>DetectImlaiWasl</c>; аят без
        /// васли с современным написанием — лишний прогон, который выглядит как
        /// покрытие, но ничего нового не разбирает.
        /// </para>
        /// </summary>
        private static void Validate(CorpusSurah surah, string fileName)
        {
            void Require(bool condition, string message)
            {
                if (!condition)
                    throw new TransliterationException($"{fileName}: {message}");
            }

            Require(surah.Number is >= 1 and <= 114, $"surah number {surah.Number} is out of range 1..114");
            Require(fileName == FileNameFor(surah.Number),
                    $"file name does not match surah number {surah.Number}");
            Require(!string.IsNullOrWhiteSpace(surah.ArabicName), "arabic name is empty");
            Require(!string.IsNullOrWhiteSpace(surah.RussianName), "russian name is empty");
            Require(CorpusTextEdition.IsKnown(surah.TextEdition),
                    $"unknown text edition '{surah.TextEdition}'");
            Require(surah.Ayahs.Count > 0, "surah has no ayahs");

            for (int i = 0; i < surah.Ayahs.Count; i++)
            {
                var ayah = surah.Ayahs[i];

                Require(ayah.Number == i + 1, $"ayah #{i + 1} is numbered {ayah.Number}");
                Require(!string.IsNullOrWhiteSpace(ayah.Arabic), $"ayah {ayah.Number} has no arabic text");
                Require(!string.IsNullOrWhiteSpace(ayah.Expected), $"ayah {ayah.Number} has no expected transliteration");

                bool hasWasl = ayah.Arabic.Contains(ArabicScript.AlefWasla);
                bool hasImlai = !string.IsNullOrWhiteSpace(ayah.ArabicImlai);

                Require(hasWasl || !hasImlai,
                        $"ayah {ayah.Number} has no wasl sign but carries an imlai spelling");
                Require(!hasWasl || hasImlai,
                        $"ayah {ayah.Number} has a wasl sign but no imlai spelling");
                Require(!hasImlai || !ayah.ArabicImlai.Contains(ArabicScript.AlefWasla),
                        $"ayah {ayah.Number}: imlai spelling still contains the wasl sign");
            }
        }
    }
}
