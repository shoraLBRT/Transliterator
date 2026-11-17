using Microsoft.Extensions.Logging.Abstractions;
using Transliterator.Core.Repositories;
using Transliterator.Core.Services;
using Transliterator.Domain.Phonology;
using Xunit;

namespace Transliterator.Tests.ServiceTests
{
    /// <summary>
    /// Подсветка изменённого (D19): после смены настройки отмечены только слова,
    /// которых не было в прежнем выводе, а отрезки склеиваются ровно в новый вывод.
    /// </summary>
    public class OutputDiffTests
    {
        private static string Changed(OutputDiff diff, string text, int line = 0) =>
            string.Join("|", diff.Changed(line).Select(w => text.Split('\n')[line].Substring(w.Start, w.Length)));

        [Fact]
        public void SameText_HasNoChanges()
        {
            var diff = OutputDiff.Compare("bi-smi llahi 1", "bi-smi llahi 1");

            Assert.False(diff.HasChanges);
            Assert.Empty(diff.Changed(0));
        }

        [Fact]
        public void ReplacedWord_IsTheOnlyOneMarked()
        {
            const string now = "ar-rahmani r-rahiim 3";
            var diff = OutputDiff.Compare("ar-rahmaani r-rahiim 3", now);

            Assert.True(diff.HasChanges);
            Assert.Equal("ar-rahmani", Changed(diff, now));
        }

        [Fact]
        public void InsertedWord_IsMarked_DroppedWordMarksNothing()
        {
            Assert.Equal("b", Changed(OutputDiff.Compare("a c", "a b c"), "a b c"));

            var dropped = OutputDiff.Compare("a b c", "a c");
            Assert.False(dropped.HasChanges);
        }

        [Fact]
        public void RepeatedWords_MatchInOrder()
        {
            // «a a» → «a b a»: LCS — оба «a», новое только «b».
            Assert.Equal("b", Changed(OutputDiff.Compare("a a", "a b a"), "a b a"));
        }

        [Fact]
        public void Lines_AreComparedByIndex_NewLineIsChangedWhole()
        {
            const string now = "a b\nc d\ne f";
            var diff = OutputDiff.Compare("a b\nc x", now);

            Assert.Empty(diff.Changed(0));
            Assert.Equal("d", Changed(diff, now, 1));
            Assert.Equal("e|f", Changed(diff, now, 2));
        }

        [Fact]
        public void Segments_JoinBackToTheText()
        {
            const string now = "a  b-c d 2\nx";
            var diff = OutputDiff.Compare("a  q d 2\ny", now);

            Assert.Equal(now, string.Concat(diff.Segments().Select(s => s.Text)));
            Assert.Equal(new[] { "b-c", "x" }, diff.Segments().Where(s => s.Changed).Select(s => s.Text));

            // Кусок строки режет отмеченное слово по своей границе (хвост со знаком аята, D17).
            var lead = diff.Segments(0, 0, 5);
            Assert.Equal("a  b-", string.Concat(lead.Select(s => s.Text)));
            Assert.Equal(new DiffSegment("b-", true), lead[^1]);
            Assert.Equal(new[] { new DiffSegment("c", true) }, diff.Segments(0, 5, 1));
        }

        [Fact]
        public async Task AlFatiha_FullToPlain_MarksOnlyTheWordsThatChanged()
        {
            var corpus = new EmbeddedCorpusRepository(NullLogger<EmbeddedCorpusRepository>.Instance);
            var arabic = CorpusText.Arabic((await corpus.GetSurahAsync(1))!);

            var full = TransliterationPipeline.Transliterate(arabic, reading: ReadingOptions.FullTajweed);
            var plain = TransliterationPipeline.Transliterate(arabic, reading: ReadingOptions.Plain);
            var diff = OutputDiff.Compare(full, plain);

            Assert.True(diff.HasChanges);

            var lines = plain.Split('\n');
            var words = lines.Sum(l => l.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length);
            var marked = Enumerable.Range(0, lines.Length).Sum(i => diff.Changed(i).Count);

            // Отмечено не всё: слова, которые правила не трогают, и номера аятов — без подсветки.
            Assert.InRange(marked, 1, words - 7 - 1);
            for (var i = 0; i < lines.Length; i++)
                Assert.All(diff.Changed(i), w => Assert.False(int.TryParse(lines[i].Substring(w.Start, w.Length), out _)));

            // Каждое отмеченное слово действительно отсутствует в той же строке прежнего вывода.
            var before = full.Split('\n');
            for (var i = 0; i < lines.Length; i++)
            {
                var old = before[i].Split(' ');
                Assert.All(diff.Changed(i), w => Assert.DoesNotContain(lines[i].Substring(w.Start, w.Length), old));
            }
        }
    }
}
