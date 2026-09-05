# Transliterator

[Русская версия](README.ru.md)

**Transliterator** turns vocalised Arabic text into extended Russian Cyrillic,
applying the rules of tajweed.

Transliteration here is not letter-by-letter substitution. The text goes through
a ten-stage pipeline: orthography is normalised, parsed into a stream of
phonological segments, the tajweed rules operate on those segments, and only the
final stage maps the result to graphemes through a profile. The reverse order is
impossible: by the time you are replacing letters, sukun, shadda, the type of
hamza and the word boundaries are already gone — and those are exactly what
tajweed needs.

> Which rules are done and which are not is recorded in
> [docs/ROADMAP.md](docs/ROADMAP.md). That file is the single source of truth
> about the state of the pipeline; where any other text disagrees with it —
> including this one — the roadmap wins.

---

## Features

* Transliterates vocalised Arabic text into extended Russian Cyrillic.
* All ten pipeline stages are implemented, from orthographic normalisation
  through to qalqalah (see the table below).
* Rules operate on a segment stream, never on finished Cyrillic: there is not a
  single Cyrillic grapheme in the rule code — every one of them comes from
  a profile.
* **Transliteration profiles** are JSON files, edited by hand with no rebuild
  of the core.
* Profile selection via the second CLI argument.
* 121 xUnit tests; the tests run against the real profile from resources rather
  than a copy kept in code.

---

## Example

**Input (Arabic with diacritics):**

```
بِسْمِ ٱللَّهِ ٱلرَّحْمَـٰنِ ٱلرَّحِيمِ ١ ٱلْحَمْدُ لِلَّهِ رَبِّ ٱلْعَـٰلَمِينَ ٢ ٱلرَّحْمَـٰنِ ٱلرَّحِيمِ ٣ مَـٰلِكِ يَوْمِ ٱلدِّينِ
```

**Result (profile `Standard`):**

```
бисми-лляяhи-ррохIмаани-ррохIииим 1 аль-хIамду лилляяhи робби-ль-'аалямииин 2 ар-рохIмаани-ррохIииим 3 маалики йауми-ддииин
```

What this output shows:

* **Madd length is rendered by repeating the grapheme**: two harakat means the
  grapheme twice (`лляя`), four means three times (`ииим`), six means four
  times. All three lengths stay distinguishable in writing.
* **The sun lam assimilates** into the following letter (`ррохIмаани`), while
  the moon lam stays itself and is set off with a hyphen (`аль-хIамду`).
* **Emphasis colours the adjacent vowel**: `а` after an emphatic consonant is
  written `о` (`робби`), and after a soft lam it is written `я` (`лляя`).
* **Word-initial hamza is not written**: `аль-хIамду`, not `ъаль-хIамду`.

A few more runs:

| Input | Result |
|---|---|
| `قُلْ هُوَ ٱللَّهُ أَحَدٌ` | `qуль hууа-ллааhу ахIад` |
| `لَمْ يَلِدْ وَلَمْ يُولَدْ` | `лям йалид уалям йууляд` |
| `مِنْ بَعْدِ` | `мим ба'д` |

The last line is iqlab: a nun sakina before `ب` turns into a mim, and the words
do not merge while it happens.

---

## Usage

The project is a **CLI application**. The first argument is the Arabic text, the
second — optional — is the profile name, defaulting to `Standard`.

```bash
dotnet run --project Transliterator.Cli -- "بِسْمِ ٱللَّهِ ٱلرَّحْمَـٰنِ ٱلرَّحِيمِ"
```

Output:

```
бисми-лляяhи-ррохIмаани-ррохIииим
```

With an explicit profile:

```bash
dotnet run --project Transliterator.Cli -- "بِسْمِ ٱللَّهِ ٱلرَّحْمَـٰنِ ٱلرَّحِيمِ" Standard
```

Running with no arguments starts an interactive mode: the application offers to
take text typed by hand or the example from Al-Fatiha, then asks for a profile
name.

---

## The pipeline

Stages are numbered in their execution order in
`RulesService.ApplyTajweedRules`. The number is not decoration: each stage
builds on the decisions of the ones before it, and they cannot be reordered.

| # | Stage | Class |
|---|-------|-------|
| 1 | Orthographic normalisation | `ArabicNormalizer` |
| 2 | Parsing into a segment stream | `ArabicParser` |
| 3 | Pause marking (waqf) | `WaqfRule` |
| 4 | Hamzat al-wasl | `WaslRule` |
| 5 | The article lam | `ArticleRule` |
| 6 | Nun sakina, tanwin, mim sakina | `NasalRule` |
| 7 | Tafkhim and tarqiq | `EmphasisRule` |
| 8 | Madd length | `MaddRule` |
| 9 | Qalqalah | `QalqalahRule` |
| 10 | Rendering through a profile | `CyrillicRenderer` |

Each stage is broken down with its acceptance criteria in
[docs/ROADMAP.md](docs/ROADMAP.md).

---

## Profiles

A profile is a JSON file in `Transliterator.Core/Resources/Profiles/`. The
profile name is the file name without its extension; the repository reads the
whole folder.

One profile ships today — `Standard` (extended Cyrillic). The roadmap item
"profiles other than `Standard`" is still open.

### Key format

A key is either the letter itself or `"letter|variant"`. The variant overrides
the base grapheme in one particular position. Lookup order is
**variant → base key → empty**.

| Variant | When it applies | Example from `Standard` |
|---|---|---|
| `heavy` | under emphasis (tafkhim) | `"َ\|heavy": "о"` — `робби` |
| `soft` | after a soft lam | `"َ\|soft": "я"` — `лляя` |
| `sukun` | in a closed syllable | `"ل\|sukun": "ль"` — `аль-` |
| `waqf` | at a pause | `"ة\|waqf": "h"` |
| `initial` | word-initially | `"ء\|initial": ""` — hamza is not written |
| `ghunna` | under nasalisation | `"ن\|ghunna": "н"` |
| `qalqalah` | the echo when an unvowelled stop is released | `"ب\|qalqalah": ""` |
| `qalqalah-strong` | the same at a pause; falls back to `qalqalah` | not set in `Standard` |

An empty value is a legitimate entry rather than an omission: this is how the
qalqalah echo and the initial hamza are deliberately set in `Standard`. The
reasoning is under "Открытые решения" in the roadmap.

### What a profile does not contain

A profile only decides **how to write** a sound that has already been
identified. What the sound *is* was decided by stages 1–9, and much of the
original text never reaches the profile at all:

* **Hamza carriers** (`أ إ ؤ ئ آ ٱ`) — stage 2 reduces them to the single
  consonant `ء` with its own vowel. There are no separate keys for them.
* **Tanwin** (`ً ٌ ٍ`) is expanded into "short vowel + nun sakin" and handled
  from there as an ordinary nun sakina.
* **The superscript alif** (`ٰ`), maddah and tatweel are removed by
  normalisation or turned into vowel length.
* **Length** is stored in harakat (2/4/6) and only becomes a repeated grapheme
  at the final stage; there are no doubled vowels in a profile.

### Example profile

An excerpt from `Transliterator.Core/Resources/Profiles/Standard.json`:

```json
{
  "Name": "Standard",
  "Description": "Расширенная кириллица. Ключ вида \"буква|вариант\" переопределяет базовую графему...",
  "Rules": {
    "ء": "ъ",
    "ء|initial": "",
    "ب": "б",
    "ب|qalqalah": "",
    "ة": "т",
    "ة|waqf": "h",
    "ح": "хI",
    "ل": "л",
    "ل|sukun": "ль",
    "ن": "н",
    "ن|ghunna": "н",

    "َ": "а",
    "َ|heavy": "о",
    "َ|soft": "я",
    "ُ": "у",
    "ِ": "и",

    "١": "1"
  }
}
```

### Profiles from code

`ITransliterationService` exposes profiles outside the pipeline:

```csharp
await service.GetAvailableProfilesAsync();          // profile names, alphabetically
await service.GetRulesAsync("Standard");            // the rules, as a copy
await service.UpdateRuleAsync("ر|heavy", "р", "Standard");  // edit a single entry
```

`GetRulesAsync` deliberately returns a copy: the repository hands out profiles
from a cache, so editing the returned dictionary would silently change the
profile for everyone already holding it.

---

## Unit tests

* 121 **xUnit** tests covering phonology, every tajweed stage, and the profile
  API.
* The tests load the real `Standard.json` from resources rather than a copy in
  code — the previous hardcoded copy had drifted from the original, and the
  tests were verifying behaviour the application no longer had.

From the command line:

```bash
dotnet test
```

In Visual Studio: `Test → Test Explorer`, build the solution, then `Run All`.

---

## Further development

The current list lives in [docs/ROADMAP.md](docs/ROADMAP.md): the open pipeline
items, the "Открытые решения" section (unsettled questions about the writing
system itself), and the "Не в конвейере" section.

---
