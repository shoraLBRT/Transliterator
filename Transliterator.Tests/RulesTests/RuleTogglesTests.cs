using Transliterator.Core.Repositories;
using Transliterator.Core.Services;
using Transliterator.Core.Services.Rules;
using Transliterator.Domain.Entities;
using Transliterator.Domain.Phonology;
using Transliterator.Tests.CorpusTests;
using Xunit;

namespace Transliterator.Tests.RulesTests
{
    /// <summary>
    /// Правила, которые в «Неполном таджвиде» выключаются по одному (I4, решено
    /// владельцем 2026-11-12): кальканя, гунна (идгам с гунной, икляб, ихфа)
    /// и мадд силя. Выключенное правило читается так, как в обычном чтении,
    /// а всё остальное — как при полном таджвиде. Главное условие владельца —
    /// проверка на корпусе: без правила вывод отличается только там, где оно
    /// сработало бы, и только тем, что оно делает.
    /// </summary>
    public class RuleTogglesTests
    {
        private static TajweedRuleCatalog Catalog => EmbeddedTajweedRuleCatalog.Catalog;

        private static ReadingOptions Without(params string[] ruleIds) =>
            new() { Off = new HashSet<string>(ruleIds) };

        // ---- Какие правила выключаются ----

        /// <summary>
        /// Раскладка владельца. Тест держит её, чтобы правка каталога не сделала
        /// выключаемым правило, от которого зависят следующие стадии, молча.
        /// </summary>
        private static readonly string[] OwnersChoice =
        {
            "idgham-bi-ghunna", "iqlab", "ikhfa", "madd-sila", "qalqalah-sughra", "qalqalah-kubra",
        };

        [Fact]
        public void Switchable_AreTheOwnersChoice() =>
            Assert.Equal(OwnersChoice.Order(), Catalog.Rules.Where(r => r.Switchable).Select(r => r.Id).Order());

        [Fact]
        public void EverySwitchableRule_IsReadByThePipeline() =>
            // Переключатель на странице, которого конвейер не читает, ничего бы не выключал.
            Assert.Equal(Catalog.Rules.Where(r => r.Switchable).Select(r => r.Id).Order(),
                         TajweedRuleIds.Switchable.Order());

        // ---- Что делает выключенное правило ----

        [Theory]
        [InlineData(TajweedRuleIds.IdghamBiGhunna, "مَن يَقُولُ", "ман йаqууль")]
        [InlineData(TajweedRuleIds.IdghamBiGhunna, "مِن نِّعْمَةٍ وَمَا", "мин-ни'матин уамаа")] // нун с нуном сливается и без таджвида
        [InlineData(TajweedRuleIds.Iqlab, "مِنۢ بَعْدِ", "мин ба'д")]
        [InlineData(TajweedRuleIds.Ikhfa, "مِن شَرِّ", "мин щарр")]                   // Standard гунну не пишет
        [InlineData(TajweedRuleIds.MaddSila, "إِنَّهُۥ كَانَ", "иннаhу каан")]
        [InlineData(TajweedRuleIds.MaddSila, "مَالَهُۥٓ أَخْلَدَ", "мааляhу ахъляд")]   // силя кубра: мунфасиль не тянет краткую
        [InlineData(TajweedRuleIds.MaddSila, "لَهُ مَا", "ляhу маа")]                 // силя без разметки мусхафа
        public void OffRule_IsReadAsInPlainReading(string ruleId, string arabic, string expected) =>
            Assert.Equal(expected, TransliterationPipeline.Transliterate(arabic, reading: Without(ruleId)));

        [Fact]
        public void OffGhunna_LeavesNoNasalisationOnItsNun()
        {
            Assert.DoesNotContain(TransliterationPipeline.Consonants("مِن شَرِّ", Without(TajweedRuleIds.Ikhfa)), s => s.Ghunna);
            Assert.DoesNotContain(TransliterationPipeline.Consonants("مِنۢ بَعْدِ", Without(TajweedRuleIds.Iqlab)), s => s.Ghunna);
            Assert.DoesNotContain(TransliterationPipeline.Consonants("مَن يَقُولُ", Without(TajweedRuleIds.IdghamBiGhunna)), s => s.Ghunna);
        }

        [Fact]
        public void Qalqalah_IsSwitchedOffMinorAndMajorApart()
        {
            // يَلِدْ — малый отзвук в соединении, يُولَدْ на паузе — большой.
            const string arabic = "لَمْ يَلِدْ وَلَمْ يُولَدْ";

            Qalqalah[] Echoes(ReadingOptions reading) =>
                TransliterationPipeline.Consonants(arabic, reading).Where(s => s.Letter == "د").Select(s => s.Qalqalah).ToArray();

            Assert.Equal(new[] { Qalqalah.Minor, Qalqalah.Major }, Echoes(ReadingOptions.FullTajweed));
            Assert.Equal(new[] { Qalqalah.None, Qalqalah.Major }, Echoes(Without(TajweedRuleIds.QalqalahSughra)));
            Assert.Equal(new[] { Qalqalah.Minor, Qalqalah.None }, Echoes(Without(TajweedRuleIds.QalqalahKubra)));
        }

        // Одна фраза, где работают все шесть правил, и рядом — идгам без гунны
        // и мунфасиль, которые не выключаются. Каждое выключенное правило меняет
        // только своё место; гунну ихфы и отзвук кальканя Standard не пишет.
        private const string Phrase = "مِن شَرِّ مَا خَلَقَ مِنۢ بَعْدِ مَن يَقُولُ إِنَّهُۥٓ أَحَدٌ مِّن رَّبِّهِ يَقْطَعُ ٱلْفَلَقَ";

        private const string PhraseInFull =
            "мин щарри маа хъоляqо мим ба'ди май-йаqуулю иннаhууу ахIадум-мир-роббиhии йаqтIо'у-ль-фаляq";

        [Theory]
        [InlineData(TajweedRuleIds.IdghamBiGhunna,
                    "мин щарри маа хъоляqо мим ба'ди ман йаqуулю иннаhууу ахIадун мир-роббиhии йаqтIо'у-ль-фаляq")]
        [InlineData(TajweedRuleIds.Iqlab,
                    "мин щарри маа хъоляqо мин ба'ди май-йаqуулю иннаhууу ахIадум-мир-роббиhии йаqтIо'у-ль-фаляq")]
        [InlineData(TajweedRuleIds.MaddSila,
                    "мин щарри маа хъоляqо мим ба'ди май-йаqуулю иннаhу ахIадум-мир-роббиhи йаqтIо'у-ль-фаляq")]
        [InlineData(TajweedRuleIds.Ikhfa, PhraseInFull)]
        [InlineData(TajweedRuleIds.QalqalahSughra, PhraseInFull)]
        [InlineData(TajweedRuleIds.QalqalahKubra, PhraseInFull)]
        public void OffRule_DoesNotTouchItsNeighbours(string ruleId, string expected)
        {
            // Правило во фразе и правда работает: без него меняются сегменты.
            Assert.NotEqual(Sounds(Phrase, ReadingOptions.FullTajweed), Sounds(Phrase, Without(ruleId)));
            Assert.Equal(PhraseInFull, TransliterationPipeline.Transliterate(Phrase));
            Assert.Equal(expected, TransliterationPipeline.Transliterate(Phrase, reading: Without(ruleId)));
        }

        // ---- Пример правила ----

        public static IEnumerable<object[]> SwitchableExamples =>
            EmbeddedTajweedRuleCatalog.Catalog.Rules.Where(r => r.Switchable)
                .SelectMany(r => r.Examples.Select(e => new object[] { r.Id, e.Arabic }));

        [Theory]
        [MemberData(nameof(SwitchableExamples))]
        public void Example_ShowsItsRule_AndOnlyIt(string ruleId, string arabic)
        {
            // Пример стоит рядом с переключателем и пересчитывается под выбор (как у
            // счётов, I3). Выключенное правило оставляет в нём след — хотя бы в
            // сегментах: гунну ихфы и отзвук Standard не пишет. А выключение соседних
            // правил пример не меняет: он показывает своё правило, а не чужое.
            Assert.NotEqual(Sounds(arabic, ReadingOptions.FullTajweed), Sounds(arabic, Without(ruleId)));

            var others = TajweedRuleIds.Switchable.Where(id => id != ruleId).ToArray();
            Assert.Equal(TransliterationPipeline.Transliterate(arabic),
                         TransliterationPipeline.Transliterate(arabic, reading: Without(others)));
            Assert.Equal(TransliterationPipeline.Transliterate(arabic, reading: Without(ruleId)),
                         TransliterationPipeline.Transliterate(arabic, reading: Without(TajweedRuleIds.Switchable.ToArray())));
        }

        // ---- Проверка на корпусе ----

        public static IEnumerable<object[]> AyahsAndRules =>
            CorpusCases.Ayahs.SelectMany(ayah => TajweedRuleIds.Switchable.Select(rule => new[] { ayah[0], rule }));

        [Theory]
        [MemberData(nameof(AyahsAndRules))]
        public void OnTheCorpus_OffRuleChangesOnlyItsOwnPlaces(string ayah, string ruleId)
        {
            var arabic = CorpusCases.Ayah(ayah).Arabic;
            var full = TransliterationPipeline.Parse(arabic);
            var off = TransliterationPipeline.Parse(arabic, Without(ruleId));

            // Ни одна стадия не добавляет и не убирает сегментов из-за выключенного
            // правила: потоки сравниваются место в место.
            Assert.Equal(full.Count, off.Count);

            var sites = Sites(ruleId, full, off);

            for (int i = 0; i < full.Count; i++)
            {
                var (before, after) = (Snapshot(full[i]), Snapshot(off[i]));
                if (before == after)
                    continue;

                Assert.True(sites.Contains(i),
                            $"{ayah}: '{ruleId}' off changed segment {i} outside its places: {before} → {after}");
                AssertChangeIsTheRules(ruleId, full[i], off[i], ayah);
            }
        }

        [Theory]
        [InlineData(TajweedRuleIds.IdghamBiGhunna)]
        [InlineData(TajweedRuleIds.Ikhfa)]
        [InlineData(TajweedRuleIds.MaddSila)]
        [InlineData(TajweedRuleIds.QalqalahSughra)]
        [InlineData(TajweedRuleIds.QalqalahKubra)]
        public void OnTheCorpus_EveryRuleIsExercised(string ruleId)
        {
            // Проверка выше ничего не стоит, если правило в корпусе не срабатывает.
            // Икляба в корпусе нет (суры 1 и 109–114) — его держат примеры выше.
            var changed = CorpusCases.Ayahs.Select(a => CorpusCases.Ayah((string)a[0]).Arabic)
                .Count(arabic => !TransliterationPipeline.Parse(arabic).Select(Snapshot)
                    .SequenceEqual(TransliterationPipeline.Parse(arabic, Without(ruleId)).Select(Snapshot)));

            Assert.True(changed > 0, $"'{ruleId}' never fires on the corpus");
        }

        [Theory]
        [InlineData(TajweedRuleIds.Ikhfa)]
        [InlineData(TajweedRuleIds.QalqalahSughra)]
        [InlineData(TajweedRuleIds.QalqalahKubra)]
        public void OnTheCorpus_StandardDoesNotWriteWhatTheseRulesMark(string ruleId)
        {
            // Гунну ихфы и отзвук кальканя Standard не пишет (G1, G6), и их выключение
            // в тексте не видно — только в своём профиле, где у них есть графема.
            // Так и сказано на странице рядом с переключателями.
            foreach (var ayah in CorpusCases.Ayahs.Select(a => CorpusCases.Ayah((string)a[0]).Arabic))
                Assert.Equal(TransliterationPipeline.Transliterate(ayah),
                             TransliterationPipeline.Transliterate(ayah, reading: Without(ruleId)));
        }

        /// <summary>
        /// Места, где правило сработало бы: только здесь выключенное может что-то
        /// поменять. Места ищутся по признакам правила, а не по разнице потоков —
        /// иначе тест проверял бы сам себя.
        /// </summary>
        private static HashSet<int> Sites(string ruleId, List<Segment> full, List<Segment> off)
        {
            var sites = new HashSet<int>();

            for (int i = 0; i < full.Count; i++)
            {
                switch (ruleId)
                {
                    case TajweedRuleIds.QalqalahSughra when full[i].Qalqalah == Qalqalah.Minor:
                    case TajweedRuleIds.QalqalahKubra when full[i].Qalqalah == Qalqalah.Major:
                        sites.Add(i);
                        break;

                    case TajweedRuleIds.MaddSila when full[i].Letter == "ه" && IsLastInWord(full, i):
                        sites.Add(i);
                        break;

                    case TajweedRuleIds.IdghamBiGhunna or TajweedRuleIds.Iqlab or TajweedRuleIds.Ikhfa
                        when IsNunSakin(off[i]):
                        int next = NextSounding(off, i);
                        if (next >= 0 && NasalRuleFor(off[next].Letter[0]) == ruleId)
                            for (int j = i; j <= next; j++)
                                sites.Add(j);
                        break;
                }
            }

            return sites;
        }

        /// <summary>Что именно может поменять выключенное правило на своём месте.</summary>
        private static void AssertChangeIsTheRules(string ruleId, Segment full, Segment off, string ayah)
        {
            switch (ruleId)
            {
                case TajweedRuleIds.QalqalahSughra or TajweedRuleIds.QalqalahKubra:
                    // Только помета отзвука, и она снята.
                    Assert.Equal(Qalqalah.None, off.Qalqalah);
                    Assert.Equal(Snapshot(full) with { Qalqalah = Qalqalah.None }, Snapshot(off));
                    break;

                case TajweedRuleIds.MaddSila:
                    // Только долгота местоимения, и она стала краткой.
                    Assert.Equal(1, off.VowelLength);
                    Assert.True(full.VowelLength >= 2, $"{ayah}: sila was not long with the rule on");
                    Assert.Equal(Snapshot(full) with { VowelLength = 1 }, Snapshot(off));
                    break;

                default:
                    // Носовой и следующий за ним звук: назализации не остаётся, а буквы
                    // и огласовки — как в обычном чтении. Эмфаза, долгота и отзвук
                    // не меняются: их считают следующие стадии, и правило их не задевает.
                    Assert.False(off.Ghunna, $"{ayah}: nasalisation is left with '{ruleId}' off");
                    Assert.Equal(full.Emphasis, off.Emphasis);
                    Assert.Equal(full.VowelLength, off.VowelLength);
                    Assert.Equal(full.Qalqalah, off.Qalqalah);
                    break;
            }
        }

        private static string NasalRuleFor(char next) => next switch
        {
            'ي' or 'ن' or 'م' or 'و' => TajweedRuleIds.IdghamBiGhunna,
            'ب' => TajweedRuleIds.Iqlab,
            'ء' or 'ه' or 'ع' or 'ح' or 'غ' or 'خ' or 'ل' or 'ر' => "",
            _ => TajweedRuleIds.Ikhfa,
        };

        private static bool IsNunSakin(Segment s) =>
            s.Kind == SegmentKind.Consonant && !s.Silent && s.Letter == "ن"
            && (s.Vowel is Harakah.Sukun or Harakah.None || s.IsGeminateFirstHalf);

        private static int NextSounding(List<Segment> segments, int from)
        {
            for (int i = from + 1; i < segments.Count; i++)
            {
                if (segments[i].IsPause)
                    return -1;
                if (segments[i].Kind == SegmentKind.Consonant && !segments[i].Silent)
                    return i;
            }

            return -1;
        }

        private static bool IsLastInWord(List<Segment> segments, int index)
        {
            for (int i = index + 1; i < segments.Count && segments[i].Kind == SegmentKind.Consonant; i++)
                if (!segments[i].Silent)
                    return false;

            return true;
        }

        /// <summary>Всё, что правила решают о сегменте, — для сравнения место в место.</summary>
        private sealed record Sound(SegmentKind Kind, string Letter, Harakah Vowel, int VowelLength, bool Shadda,
                                    bool Ghunna, Emphasis Emphasis, Qalqalah Qalqalah, VowelVariant Variant,
                                    bool Silent, bool Geminate, bool Hyphen, string Literal, bool Pause);

        private static Sound Snapshot(Segment s) =>
            new(s.Kind, s.Letter, s.Vowel, s.VowelLength, s.Shadda, s.Ghunna, s.Emphasis, s.Qalqalah,
                s.VowelVariant, s.Silent, s.IsGeminateFirstHalf, s.HyphenAfter, s.Literal, s.IsPause);

        private static string Sounds(string arabic, ReadingOptions reading) =>
            string.Join(" ", TransliterationPipeline.Parse(arabic, reading).Select(Snapshot));
    }
}
