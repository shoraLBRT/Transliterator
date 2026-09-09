using Xunit;

namespace Transliterator.Tests.RulesTests
{
    /// <summary>
    /// Точечный тест на месте открытого бага эпика B. Ожидание известно и записано
    /// в <c>docs/BACKLOG.md</c>, но конвейер его сегодня не даёт — и красный тест
    /// сообщал бы ровно то, что уже написано в бэклоге, зато перестал бы сообщать
    /// про новое. Поэтому проверяется сегодняшний вывод, и проверяется так же
    /// строго, как в <see cref="CorpusTests.KnownDivergences"/>: изменился вывод —
    /// красный, сошёлся с ожиданием — тоже красный, с требованием переписать тест
    /// на ожидание и отметить баг в бэклоге.
    /// </summary>
    internal static class OpenBug
    {
        /// <param name="bug">Номера багов эпика B, из-за которых вывод такой.</param>
        /// <param name="expected">Ожидание из бэклога — то, что должно получиться после починки.</param>
        /// <param name="today">Вывод конвейера на сегодня, целиком.</param>
        public static void StillProduces(string bug, string arabic, string expected, string today)
        {
            var actual = TransliterationPipeline.Transliterate(arabic);

            if (actual == expected)
                Assert.Fail($"{arabic}: вывод сошёлся с ожиданием — {bug} починен. " +
                            "Перепишите тест на ожидание и отметьте баг в docs/BACKLOG.md.");

            if (actual != today)
                Assert.Fail($"{arabic}: {bug} ещё открыт, но вывод изменился." +
                            $"{Environment.NewLine}  записано:  {today}" +
                            $"{Environment.NewLine}  сейчас:    {actual}" +
                            $"{Environment.NewLine}  ожидание:  {expected}");
        }
    }
}
