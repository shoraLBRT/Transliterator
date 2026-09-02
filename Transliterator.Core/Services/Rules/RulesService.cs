using Transliterator.Domain.Phonology;

namespace Transliterator.Core.Services.Rules
{
    /// <summary>
    /// Порядок применения правил таджвида. Порядок здесь — не оформление, а содержание:
    /// каждая стадия опирается на решения предыдущих.
    /// <list type="number">
    ///   <item>Нормализация орфографии — <c>ArabicNormalizer</c>.</item>
    ///   <item>Разбор в поток сегментов — <c>ArabicParser</c>.</item>
    ///   <item>Разметка пауз (вакф) — <c>WaqfRule</c>. Решает, какие слова соединяются.</item>
    ///   <item>Хамзат аль-васль.</item>
    ///   <item>Лям артикля.</item>
    ///   <item>Нун сакина, танвин, мим сакина, идгамы — <b>не реализовано</b>.</item>
    ///   <item>Тафхим и таркик.</item>
    ///   <item>Длительность мадда.</item>
    ///   <item>Кальканя — <b>не реализовано</b>.</item>
    ///   <item>Рендеринг по профилю — <c>CyrillicRenderer</c>.</item>
    /// </list>
    /// </summary>
    public class RulesService
    {
        private readonly WaqfRule _waqfRule;
        private readonly WaslRule _waslRule;
        private readonly ArticleRule _articleRule;
        private readonly EmphasisRule _emphasisRule;
        private readonly MaddRule _maddRule;

        public RulesService(
            WaqfRule waqfRule,
            WaslRule waslRule,
            ArticleRule articleRule,
            EmphasisRule emphasisRule,
            MaddRule maddRule)
        {
            _waqfRule = waqfRule;
            _waslRule = waslRule;
            _articleRule = articleRule;
            _emphasisRule = emphasisRule;
            _maddRule = maddRule;
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

            // Стадия 6: нун сакина, танвин, мим сакина, идгамы.
            // TODO(P3): изхар, идгам ±гунна, икляб, ихфа.

            _emphasisRule.Apply(segments);
            _maddRule.Apply(segments);

            // Стадия 9: кальканя.
            // TODO(P4).
        }
    }
}
