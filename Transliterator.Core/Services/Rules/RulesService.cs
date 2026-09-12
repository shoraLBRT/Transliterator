using Transliterator.Domain.Phonology;

namespace Transliterator.Core.Services.Rules
{
    /// <summary>
    /// Порядок применения правил таджвида. Порядок здесь — не оформление, а содержание:
    /// каждая стадия опирается на решения предыдущих.
    /// <para>Что из этого сделано, а что нет — в <c>docs/ROADMAP.md</c>.</para>
    /// <list type="number">
    ///   <item>Нормализация орфографии — <c>ArabicNormalizer</c>.</item>
    ///   <item>Разбор в поток сегментов — <c>ArabicParser</c>.</item>
    ///   <item>Разметка пауз (вакф) — <c>WaqfRule</c>. Решает, какие слова соединяются.</item>
    ///   <item>Хамзат аль-васль.</item>
    ///   <item>Лям артикля.</item>
    ///   <item>Идгам однородных и близких согласных — <c>AssimilationRule</c>.</item>
    ///   <item>Нун сакина, танвин, мим сакина, идгамы — <c>NasalRule</c>.</item>
    ///   <item>Тафхим и таркик.</item>
    ///   <item>Длительность мадда.</item>
    ///   <item>Кальканя — <c>QalqalahRule</c>.</item>
    ///   <item>Рендеринг по профилю — <c>CyrillicRenderer</c>.</item>
    /// </list>
    /// </summary>
    public class RulesService
    {
        private readonly WaqfRule _waqfRule;
        private readonly WaslRule _waslRule;
        private readonly ArticleRule _articleRule;
        private readonly AssimilationRule _assimilationRule;
        private readonly NasalRule _nasalRule;
        private readonly EmphasisRule _emphasisRule;
        private readonly MaddRule _maddRule;
        private readonly QalqalahRule _qalqalahRule;

        public RulesService(
            WaqfRule waqfRule,
            WaslRule waslRule,
            ArticleRule articleRule,
            AssimilationRule assimilationRule,
            NasalRule nasalRule,
            EmphasisRule emphasisRule,
            MaddRule maddRule,
            QalqalahRule qalqalahRule)
        {
            _waqfRule = waqfRule;
            _waslRule = waslRule;
            _articleRule = articleRule;
            _assimilationRule = assimilationRule;
            _nasalRule = nasalRule;
            _emphasisRule = emphasisRule;
            _maddRule = maddRule;
            _qalqalahRule = qalqalahRule;
        }

        public void ApplyTajweedRules(IList<Segment> segments)
        {
            if (segments.Count == 0)
                return;

            // Стадия 3: вакф. Должна идти здесь — до всякого межсловного стыка:
            // она решает, какие слова вообще окажутся соседями.
            _waqfRule.Apply(segments);

            _waslRule.Apply(segments);
            _articleRule.Apply(segments);

            // Стадия 6: идгам однородных и близких согласных. Стоит после артикля:
            // шадда солнечной буквы к этому моменту снята и за отметку слияния
            // больше не сойдёт. Носовых не касается — они решают свою судьбу сами.
            _assimilationRule.Apply(segments);

            // Стадия 7: нун сакина, танвин, мим сакина. Носовым нужен тот же
            // уже слитый текст — у солнечного ляма на этом месте не ل, — но своё
            // слияние они решают сами: по одной шадде изхар и ихфу от идгама
            // не отличить. И до эмфазы, потому что идгам меняет её условия.
            _nasalRule.Apply(segments);

            _emphasisRule.Apply(segments);
            _maddRule.Apply(segments);

            // Стадия 10: кальканя. Идёт последней: безгласность, от которой она
            // зависит, — итог всех предыдущих стадий.
            _qalqalahRule.Apply(segments);
        }
    }
}
