using Transliterator.Core.Services.Phonology;
using Transliterator.Domain.Phonology;

namespace Transliterator.Core.Services.Rules
{
    /// <summary>
    /// Стадия 3 конвейера: разметка пауз (вакф) и правила паузального произношения.
    /// <para>
    /// Определяет границы пауз и применяет трансформации для последних согласных
    /// каждой группы слов:
    /// <list type="bullet">
    ///   <item>Конечная краткая огласовка снимается (становится сукуном).</item>
    ///   <item>Танвин (фатхатан) на паузе удлиняется в мадд ивад (2 харката).</item>
    ///   <item>Танвин (дамматан, кастратан) и его нун-сегмент снимаются полностью.</item>
    ///   <item>Та-марбута → /h/ (в рендере через ключ "|waqf").</item>
    /// </list>
    /// </para>
    /// </summary>
    public class WaqfRule
    {
        public void Apply(IList<Segment> segments)
        {
            for (int i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];
                if (segment.Kind != SegmentKind.Consonant || segment.WaqfAfter == WaqfType.None || segment.WaqfAfter == WaqfType.Forbidden)
                    continue;

                // Политика применения вакфа:
                // - Optional, Preferred, Obligatory, Dual → применяются правила паузы.
                // - Forbidden → соединение, никаких правил.
                // - None → соединение, никаких правил.

                ApplyPausalRules(segments, i);
            }
        }

        /// <summary>
        /// Применяет трансформации последнего сегмента перед паузой.
        /// </summary>
        private void ApplyPausalRules(IList<Segment> segments, int index)
        {
            var segment = segments[index];

            // Сохраняем исходную огласовку для EmphasisRule.
            segment.OriginalVowel = segment.Vowel;

            // Конечная краткая огласовка (1 харакат) снимается.
            if (segment.VowelLength == 1 && segment.Vowel is not (Harakah.None or Harakah.Sukun))
            {
                segment.Vowel = Harakah.Sukun;
            }

            // Та-марбута: огласовка снимается, рендер возьмёт ключ "|waqf" = "h".
            if (segment.IsTaMarbuta && segment.Vowel is not Harakah.Sukun)
            {
                segment.Vowel = Harakah.Sukun;
            }

            // Обработка танвина:
            // Парсер уже развернул танвин в "гласная + нун сакин" с FromTanwin=true.
            // Нужно найти и модифицировать или удалить оба сегмента.

            int nextIndex = SegmentNavigator.NextConsonantInWord(segments, index);
            if (nextIndex > 0)
            {
                var next = segments[nextIndex];
                if (next.FromTanwin && next.Letter == ArabicScript.NunStr)
                {
                    // Фатхатан (ً, разверну в фатха + нун): на паузе фатха удлиняется (мадд ивад),
                    // нун снимается.
                    if (segment.Vowel == Harakah.Fatha && segment.VowelLength == 1)
                    {
                        segment.VowelLength = 2;
                        next.Silent = true;
                    }
                    // Дамматан (ٌ, раскрыто в дамма + нун) и касратан (ٍ, раскрыто в касра + нун):
                    // на паузе оба снимаются полностью.
                    else if (segment.Vowel is Harakah.Damma or Harakah.Kasra)
                    {
                        segment.Vowel = Harakah.Sukun;
                        next.Silent = true;
                    }
                }
            }
        }
    }
}
