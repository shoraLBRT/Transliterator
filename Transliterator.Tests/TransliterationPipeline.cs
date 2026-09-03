using Transliterator.Core.Services.Phonology;
using Transliterator.Core.Services.Rules;
using Transliterator.Domain.Entities;
using Transliterator.Domain.Phonology;

namespace Transliterator.Tests
{
    /// <summary>Сборка конвейера без DI и без обращения к хранилищу профилей.</summary>
    public static class TransliterationPipeline
    {
        public static string Transliterate(string arabicText, TransliterationProfile? profile = null)
        {
            var segments = Parse(arabicText);
            return new CyrillicRenderer().Render(segments, profile ?? TestProfiles.Standard);
        }

        /// <summary>Разбор с применением правил, но без рендеринга — для проверок самого слоя фонем.</summary>
        public static List<Segment> Parse(string arabicText)
        {
            var normalized = new ArabicNormalizer().Normalize(arabicText);
            var segments = new ArabicParser().Parse(normalized);

            new RulesService(new WaqfRule(), new WaslRule(), new ArticleRule(),
                             new NasalRule(), new EmphasisRule(), new MaddRule())
                .ApplyTajweedRules(segments);

            return segments;
        }

        /// <summary>
        /// Разбор без правил — для проверок самого парсера. Нужен там, где правила
        /// потом убирают разобранное: танвин на паузе снимается вместе со своим нуном,
        /// и через полный конвейер его уже не увидеть.
        /// </summary>
        public static List<Segment> ParseWithoutRules(string arabicText) =>
            new ArabicParser().Parse(new ArabicNormalizer().Normalize(arabicText));

        public static List<Segment> Consonants(string arabicText) =>
            Parse(arabicText).Where(s => s.Kind == SegmentKind.Consonant).ToList();
    }
}
