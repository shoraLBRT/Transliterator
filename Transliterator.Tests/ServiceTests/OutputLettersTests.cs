using Transliterator.Core.Services;
using Transliterator.Core.Services.Phonology;
using Transliterator.Core.Services.Rules;
using Transliterator.Domain.Entities;
using Xunit;

namespace Transliterator.Tests.ServiceTests
{
    /// <summary>
    /// Подсказки букв (D41): отрисовщик отмечает, из какой арабской буквы получился
    /// каждый кусок вывода, а страница по этим кускам подсвечивает буквы, звука
    /// которых нет в русском. Буква берётся из происхождения, не из поиска графемы.
    /// </summary>
    public class OutputLettersTests
    {
        private static (string Text, List<LetterSpan> Letters) Render(string arabic)
        {
            var letters = new List<LetterSpan>();
            var text = new CyrillicRenderer().Render(TransliterationPipeline.Parse(arabic), TestProfiles.Cyrillic, null, letters);
            return (text, letters);
        }

        private static string Of(string text, LetterSpan span) => text.Substring(span.Start, span.Length);

        [Fact]
        public void EachConsonant_IsMarkedWithItsLetter_WithoutTheVowel()
        {
            var (text, letters) = Render("الْحَمْدُ");

            var ha = Assert.Single(letters, s => s.Letter == "ح");
            Assert.Equal(TestProfiles.Cyrillic.Rules["ح"], Of(text, ha));
            Assert.Equal("хI", Of(text, ha));
        }

        [Fact]
        public void DoubledConsonant_IsOneSpanOverBothHalves()
        {
            var (text, letters) = Render("رَبِّ");

            var ba = Assert.Single(letters, s => s.Letter == "ب");
            Assert.Equal("бб", Of(text, ba));
        }

        [Fact]
        public void Ayn_IsMarkedEvenThoughItIsOnlyAnApostrophe()
        {
            var (text, letters) = Render("الْعَالَمِينَ");

            var ayn = Assert.Single(letters, s => s.Letter == "ع");
            Assert.Equal("'", Of(text, ayn));
        }

        [Fact]
        public void Spans_AreInOrder_DoNotOverlap_AndStayInsideTheText()
        {
            var (text, letters) = Render("بِسْمِ اللَّهِ الرَّحْمَٰنِ الرَّحِيمِ\nالْحَمْدُ لِلَّهِ رَبِّ الْعَالَمِينَ");

            Assert.NotEmpty(letters);
            var end = 0;
            foreach (var span in letters)
            {
                Assert.True(span.Start >= end, $"span {span} overlaps the previous one");
                Assert.True(span.Length > 0);
                Assert.True(span.Start + span.Length <= text.Length);
                Assert.DoesNotContain('\n', Of(text, span));
                end = span.Start + span.Length;
            }
        }

        [Fact]
        public void ServiceResult_CarriesTheSameSpansAsTheRenderer()
        {
            // Профиль передаётся целиком, хранилище сервису не нужно (C2).
            var service = new TransliterationService(
                null!, new ArabicNormalizer(), new ArabicParser(),
                new RulesService(new WaqfRule(), new WaslRule(), new ArticleRule(),
                                 new AssimilationRule(), new NasalRule(),
                                 new EmphasisRule(), new MaddRule(), new QalqalahRule()),
                new CyrillicRenderer());

            var result = service.Transliterate("الْحَمْدُ لِلَّهِ", TestProfiles.Cyrillic);
            var (text, letters) = Render("الْحَمْدُ لِلَّهِ");

            Assert.Equal(text, result.TransliteratedText);
            Assert.Equal(letters, result.Letters);
        }

        [Fact]
        public void Build_KeepsOnlyHintedLetters_InLinePositions()
        {
            const string text = "аб\nвгд";
            var letters = new[]
            {
                new LetterSpan(0, 1, "x"), new LetterSpan(1, 1, "y"),
                new LetterSpan(4, 2, "x"),
            };

            var hints = OutputLetters.Build(text, letters, letter => letter == "x");

            Assert.Equal(new[] { new LetterSpan(0, 1, "x") }, hints.Line(0));
            Assert.Equal(new[] { new LetterSpan(1, 2, "x") }, hints.Line(1));
            Assert.Empty(hints.Line(2));
        }

        [Fact]
        public void Split_CutsChangedPartsByLetters_AndGluesBackToTheSameText()
        {
            const string text = "ab cdef";
            var hints = OutputLetters.Build(text, new[] { new LetterSpan(1, 1, "x"), new LetterSpan(4, 2, "x") }, _ => true);

            // Кусок строки с позиции 1 — «b cdef», «cdef» изменилось (D19).
            var parts = new[] { new DiffSegment("b ", false), new DiffSegment("cdef", true) };
            var pieces = hints.Split(0, 1, parts);

            Assert.Equal("b cdef", string.Concat(pieces.Select(p => p.Text)));
            Assert.Equal(
                new[]
                {
                    new OutputPiece("b", false, "x"), new OutputPiece(" ", false, null),
                    new OutputPiece("c", true, null), new OutputPiece("de", true, "x"), new OutputPiece("f", true, null),
                },
                pieces);
        }

        /// <summary>D45: слово без огласовок — ни у одной буквы нет знака чтения.</summary>
        [Theory]
        [InlineData("قل هو الله أحد", 4, 4)]
        [InlineData("قُلْ هو اللَّهُ أحد", 4, 2)]
        [InlineData("قُلْ هُوَ اللَّهُ أَحَدٌ", 4, 0)]
        [InlineData("و قُلْ", 1, 0)] // однобуквенное слово не считается
        public void VowelCheck_CountsWordsWithoutMarks(string arabic, int words, int unvowelled)
        {
            var segments = TransliterationPipeline.ParseWithoutRules(arabic);

            var coverage = VowelCheck.Mark(new ArabicNormalizer().Normalize(arabic), segments);

            Assert.Equal(new VowelCoverage(words, unvowelled), coverage);
        }

        /// <summary>D45: в корпусе огласовано всё — ни одного слова без огласовок, ни в одном написании.</summary>
        [Theory]
        [MemberData(nameof(CorpusTests.CorpusCases.Ayahs), MemberType = typeof(CorpusTests.CorpusCases))]
        public void Corpus_HasNoUnvowelledWords(string id)
        {
            var arabic = CorpusTests.CorpusCases.Ayah(id).Arabic;
            var coverage = VowelCheck.Mark(new ArabicNormalizer().Normalize(arabic), TransliterationPipeline.ParseWithoutRules(arabic));

            Assert.True(coverage.Words > 0, $"{id}: no words");
            Assert.Equal(0, coverage.Unvowelled);
        }

        [Fact]
        public void UnvowelledWord_IsMarkedAsAWholeOutputWord()
        {
            var segments = TransliterationPipeline.ParseWithoutRules("قُلْ هو اللَّهُ");
            VowelCheck.Mark(new ArabicNormalizer().Normalize("قُلْ هو اللَّهُ"), segments);
            var letters = new List<LetterSpan>();
            var text = new CyrillicRenderer().Render(segments, TestProfiles.Cyrillic, null, letters);

            var hints = OutputLetters.Build(text, letters, _ => false);
            var word = Assert.Single(hints.Unvowelled(0));
            var marked = text.Substring(word.Start, word.Length);

            Assert.DoesNotContain(' ', marked);
            Assert.Equal(text.Split(' ')[1], marked);
            Assert.Equal(text, string.Concat(hints.Split(0, 0, new[] { new DiffSegment(text, false) }).Select(p => p.Text)));
            Assert.Contains(hints.Split(0, 0, new[] { new DiffSegment(text, false) }), p => p.Unvowelled && p.Text == marked);
        }

        [Fact]
        public void ServiceResult_SaysWhenTheTextIsMostlyUnvowelled()
        {
            var service = new TransliterationService(
                null!, new ArabicNormalizer(), new ArabicParser(),
                new RulesService(new WaqfRule(), new WaslRule(), new ArticleRule(),
                                 new AssimilationRule(), new NasalRule(),
                                 new EmphasisRule(), new MaddRule(), new QalqalahRule()),
                new CyrillicRenderer());

            Assert.True(service.Transliterate("قل هو الله أحد", TestProfiles.Cyrillic).MostlyUnvowelled);
            var partly = service.Transliterate("قُلْ هُوَ اللَّهُ أحد", TestProfiles.Cyrillic);
            Assert.False(partly.MostlyUnvowelled);
            Assert.Equal(1, partly.UnvowelledWords);
            Assert.False(service.Transliterate("قُلْ هُوَ اللَّهُ أَحَدٌ", TestProfiles.Cyrillic).MostlyUnvowelled);
        }

        [Fact]
        public void None_HasNoLetters()
        {
            var hints = OutputLetters.None("ab\ncd");

            Assert.Empty(hints.Line(0));
            Assert.Empty(hints.Line(1));
            Assert.Equal(new[] { new OutputPiece("ab", false, null) }, hints.Split(0, 0, new[] { new DiffSegment("ab", false) }));
        }
    }
}
