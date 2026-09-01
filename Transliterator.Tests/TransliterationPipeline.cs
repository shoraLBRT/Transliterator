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

            new RulesService(new WaslRule(), new ArticleRule(), new EmphasisRule(), new MaddRule())
                .ApplyTajweedRules(segments);

            return segments;
        }

        public static List<Segment> Consonants(string arabicText) =>
            Parse(arabicText).Where(s => s.Kind == SegmentKind.Consonant).ToList();
    }
}
