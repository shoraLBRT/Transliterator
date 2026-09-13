# Transliterator

[Русская версия](README.ru.md)

**Try it in the browser:** https://shoralbrt.github.io/Transliterator/ — the
pipeline runs right on the page (Blazor WebAssembly), nothing is sent to a server.

**Transliterator** turns vocalised Arabic text into extended Russian Cyrillic,
applying the rules of tajweed.

Transliteration here is not letter-by-letter substitution. The text goes through
an eleven-stage pipeline: orthography is normalised, parsed into a stream of
phonological segments, the tajweed rules operate on those segments, and only the
final stage maps the result to graphemes through a profile. The reverse order is
impossible: by the time you are replacing letters, sukun, shadda, the type of
hamza and the word boundaries are already gone — and those are exactly what
tajweed needs.

---

## Features

* Transliterates vocalised Arabic text into extended Russian Cyrillic — or into
  Latin with academic diacritics, through the same pipeline.
* All eleven pipeline stages are implemented, from orthographic normalisation
  through to qalqalah (see the table below).
* Rules operate on a segment stream, never on finished Cyrillic: there is not a
  single Cyrillic grapheme in the rule code — every one of them comes from
  a profile.
* **Transliteration profiles** are JSON files. Two ship with the core; your own
  can be made and edited in the browser.
* **Web version**: input with a live result, ready-made surahs, the profile rules
  as a table and a profile editor — a static page with no server.
* **CLI** with profile selection via the second argument.
* xUnit tests run the whole corpus of worked examples through the pipeline; the
  build and the tests run on every pull request.

---

## Example

**Input (Al-Fatiha 1–4, Arabic with diacritics):**

<!-- readme-example:fatiha-input -->
```
بِسْمِ ٱللَّهِ ٱلرَّحْمَٰنِ ٱلرَّحِيمِ ١ ٱلْحَمْدُ لِلَّهِ رَبِّ ٱلْعَٰلَمِينَ ٢ ٱلرَّحْمَٰنِ ٱلرَّحِيمِ ٣ مَٰلِكِ يَوْمِ ٱلدِّينِ ٤
```

**Result (profile `Standard`):**

<!-- readme-example:fatiha-output -->
```
бисми-лляяhи-ррохIмаани-ррохIииим 1 аль-хIамду лилляяhи робби-ль-'аалямииин 2 ар-рохIмаани-ррохIииим 3 маалики йауми-ддииин 4
```

What this output shows:

* **Madd length is rendered by repeating the grapheme**: two harakat means the
  grapheme twice (`лляя`), four means three times (`ииим`), six means four
  times. All three lengths stay distinguishable in writing.
* **The sun lam assimilates** into the following letter (`ррохIмаани`), while
  the moon lam stays itself and is set off with a hyphen (`аль-хIамду`).
* **Emphasis colours the consonant's own vowel**: `а` after a heavy consonant is
  written `о` (`робби`), and after a soft lam it is written `я` (`лляя`). The
  vowel before a heavy consonant is not coloured.
* **Word-initial hamza is not written**: `аль-хIамду`, not `ъаль-хIамду`.

These four ayahs are taken from the corpus
(`Transliterator.Core/Resources/Corpus/001.json`), and a test checks that this
file shows exactly what the corpus expects and the pipeline gives.

A few more runs:

<!-- readme-example:runs -->
| Input | Result |
|---|---|
| `قُلْ هُوَ ٱللَّهُ أَحَدٌ` | `qуль hууа-ллааhу ахIад` |
| `لَمْ يَلِدْ وَلَمْ يُولَدْ` | `лям йалид уалям йууляд` |
| `مِنْ بَعْدِ` | `мим ба'д` |

The last line is iqlab: a nun sakina before `ب` turns into a mim, and the words
do not merge while it happens.

---

## Web version

https://shoralbrt.github.io/Transliterator/

A single page built with Blazor WebAssembly: the core is compiled to wasm and does
all the work in the browser. There is no server, and the page is published from
`master` to GitHub Pages automatically.

* **Input and result.** Arabic text is typed right to left, and the result is
  recalculated on every edit. Text without diacritics is accepted too, although
  without harakat the reading is incomplete.
* **Profiles.** `Standard` and `Latin` are built in; the chosen profile is
  remembered across reloads.
* **Ready-made surahs** from the corpus are one click away. The text goes in one
  ayah per line, and the result keeps the lines.
* **Profile rules** as a table: variants stand under their letter, with a search
  by key.

### Custom profiles

* **Create** a profile as a copy of any existing one and change graphemes right in
  the rules table — the result updates as you type.
* **Rename and delete** your own profiles. Built-in profiles are only copied,
  never changed: saving an edit of `Standard` creates `Standard (копия)`.
* Profiles live in the browser's **localStorage**: they survive a reload and are
  never sent anywhere. A damaged entry is skipped with a message instead of
  breaking the page.
* **Export and import** a profile as JSON in the same format as `Standard.json` —
  this is how a profile is shared or brought into
  `Transliterator.Core/Resources/Profiles/`. Import lists everything that is wrong
  with a file and never overwrites an existing profile: a taken name gets a copy.

To run the page locally:

```bash
dotnet run --project Transliterator.Web
```

---

## Usage (CLI)

The first argument is the Arabic text, the second — optional — is the profile
name, defaulting to `Standard`.

<!-- readme-example:cli -->
```bash
dotnet run --project Transliterator.Cli -- "بِسْمِ ٱللَّهِ ٱلرَّحْمَـٰنِ ٱلرَّحِيمِ"
```

The result it prints:

<!-- readme-example:cli-output -->
```
бисми-лляяhи-ррохIмаани-ррохIииим
```

With an explicit profile:

```bash
dotnet run --project Transliterator.Cli -- "بِسْمِ ٱللَّهِ ٱلرَّحْمَـٰنِ ٱلرَّحِيمِ" Latin
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
| 6 | Idgham of homorganic and close consonants | `AssimilationRule` |
| 7 | Nun sakina, tanwin, mim sakina | `NasalRule` |
| 8 | Tafkhim and tarqiq | `EmphasisRule` |
| 9 | Madd length | `MaddRule` |
| 10 | Qalqalah | `QalqalahRule` |
| 11 | Rendering through a profile | `CyrillicRenderer` |

Each stage is broken down with its acceptance criteria in
[docs/ROADMAP.md](docs/ROADMAP.md).

---

## Profiles

A profile is a JSON file in `Transliterator.Core/Resources/Profiles/`. The same
files are embedded in the core assembly, which is how the browser reads them.

Two profiles ship today:

* `Standard` — extended Cyrillic;
* `Latin` — Latin with academic diacritics (`ṯ ǧ ḥ ḫ ḏ š ṣ ḍ ṭ ẓ ġ`, `ʾ` and `ʿ`).

No rule knows which script it writes for: the pipeline is the same, only the
graphemes differ.

### Key format

A key is either the letter itself or `"letter|variant"`. The variant overrides
the base grapheme in one particular position. Lookup order is
**variant → base key**; a base key the text needs but the profile lacks is an
error that names the key.

| Variant | When it applies | Example from `Standard` |
|---|---|---|
| `heavy` | under emphasis (tafkhim) | `"َ\|heavy": "о"` — `робби` |
| `soft` | after a soft lam | `"َ\|soft": "я"` — `лляя` |
| `sukun` | in a closed syllable | `"ل\|sukun": "ль"` — `аль-` |
| `waqf` | at a pause | `"ة\|waqf": "h"` |
| `initial` | word-initially | `"ء\|initial": ""` — hamza is not written |
| `hiatus` | hamza between vowels | `"ء\|hiatus": "-"` — `уа-иййаака` |
| `ghunna` | under nasalisation | `"ن\|ghunna": "н"` |
| `qalqalah` | the echo when an unvowelled stop is released | `"ب\|qalqalah": ""` |
| `qalqalah-strong` | the same at a pause; falls back to `qalqalah` | not set in `Standard` |

An empty value is a legitimate entry rather than an omission: this is how the
qalqalah echo and the initial hamza are deliberately set in `Standard`. The
reasoning is under "Открытые решения" in the roadmap.

### What a profile does not contain

A profile only decides **how to write** a sound that has already been
identified. What the sound *is* was decided by stages 1–10, and much of the
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

<!-- readme-example:profile -->
```json
{
  "Name": "Standard",
  "Description": "Расширенная кириллица. Ключ вида \"буква|вариант\" переопределяет базовую графему...",
  "Rules": {
    "ء": "ъ",
    "ء|initial": "",
    "ء|hiatus": "-",
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
service.Transliterate(text, profile);               // a profile that is not in storage
```

`GetRulesAsync` deliberately returns a copy: the repository hands out profiles
from a cache, so editing the returned dictionary would silently change the
profile for everyone already holding it.

`Transliterate(text, profile)` takes a profile that is not saved anywhere — the
path the browser editor uses. The profile is validated before the calculation.

---

## Unit tests

* **xUnit** tests cover phonology, every tajweed stage, profiles and their
  storages, the corpus and the web-facing services.
* The tests load the real profiles from resources rather than copies in code —
  a hardcoded copy once drifted from the original, and the tests were verifying
  behaviour the application no longer had.
* A data-driven run takes the whole corpus of worked examples through the
  pipeline: every ayah in both spellings, and every surah as a single line.
  The cases are built from the corpus files, so adding a surah adds its tests.
* The examples in this file are checked too: the Al-Fatiha excerpt against the
  corpus, the runs and the CLI output against the pipeline, and the profile
  excerpt against `Standard.json`.
* GitHub Actions builds the solution and runs the tests on every pull request to
  `master`.

From the command line:

```bash
dotnet test
```

In Visual Studio: `Test → Test Explorer`, build the solution, then `Run All`.

---

## Further development

The pipeline — open items, the "Открытые решения" section (unsettled questions
about the writing system itself) and the "Не в конвейере" section — lives in
[docs/ROADMAP.md](docs/ROADMAP.md). Everything else — the corpus, the web version,
profiles and infrastructure — lives in [docs/BACKLOG.md](docs/BACKLOG.md).

---
