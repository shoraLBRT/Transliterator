using Transliterator.Core.Services.Phonology;
using Transliterator.Core.Services.Rules;
using Transliterator.Domain.Entities;
using Transliterator.Domain.Phonology;

namespace Transliterator.Tests
{
    /// <summary>Сборка конвейера без DI и без обращения к хранилищу профилей.</summary>
    public static class TransliterationPipeline
    {
        public static string Transliterate(string arabicText, TransliterationProfile? profile = null,
                                           ReadingOptions? reading = null)
        {
            var segments = Parse(arabicText, reading);
            return new CyrillicRenderer().Render(segments, profile ?? TestProfiles.Standard);
        }

        /// <summary>Разбор с применением правил, но без рендеринга — для проверок самого слоя фонем.</summary>
        public static List<Segment> Parse(string arabicText, ReadingOptions? reading = null)
        {
            var normalized = new ArabicNormalizer().Normalize(arabicText);
            var segments = new ArabicParser().Parse(normalized);

            new RulesService(new WaqfRule(), new WaslRule(), new ArticleRule(),
                             new AssimilationRule(), new NasalRule(),
                             new EmphasisRule(), new MaddRule(), new QalqalahRule())
                .ApplyTajweedRules(segments, reading);

            return segments;
        }

        /// <summary>
        /// Разбор без правил — для проверок самого парсера. Нужен там, где правила
        /// потом убирают разобранное: танвин на паузе снимается вместе со своим нуном,
        /// и через полный конвейер его уже не увидеть.
        /// </summary>
        public static List<Segment> ParseWithoutRules(string arabicText) =>
            new ArabicParser().Parse(new ArabicNormalizer().Normalize(arabicText));

        public static List<Segment> Consonants(string arabicText, ReadingOptions? reading = null) =>
            Parse(arabicText, reading).Where(s => s.Kind == SegmentKind.Consonant).ToList();
    }
}
