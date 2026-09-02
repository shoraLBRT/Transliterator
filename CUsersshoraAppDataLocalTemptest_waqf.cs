using Transliterator.Core.Services.Phonology;
using Transliterator.Domain.Phonology;
using Transliterator.Tests;

var segments = TransliterationPipeline.Parse("مُسْلِمٌۘ");

foreach (var s in segments)
{
    if (s.Kind == SegmentKind.Consonant)
    {
        Console.WriteLine($"{s.Letter}: vowel={s.Vowel}, length={s.VowelLength}, waqf={s.WaqfAfter}, fromTanwin={s.FromTanwin}, silent={s.Silent}");
    }
}
