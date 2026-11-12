# Transliterator

[Русская версия](README.ru.md)

**Try it in the browser:** 
https://shoralbrt.github.io/Transliterator/ — the
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
* **Transliteration profiles** are JSON files. Two ship with the core —
  `Standard` and `Latin`; on the page you can copy one and rewrite its graphemes.
* **Web version**: one input field, the result under it and ready-made surahs —
  a static page with no server.
* **CLI**: text as an argument, from a file, from stdin or a ready-made surah
  from the corpus; the profiles are listed before choosing.
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
бисми-лляяhи-ррохIмаани-ррохIиим 1 аль-хIамду лилляяhи робби-ль-'аалямиин 2 ар-рохIмаани-ррохIиим 3 маалики йауми-ддиин 4
```

What this output shows:

* **Madd length is rendered by repeating the grapheme**: two harakat means the
  grapheme twice (`лляя`), four means three times (`ииим`), five means four
  times, six means five times (`дIдIоооооллиин`). The number of letters is not
  the number of harakat, and that is not a mistake: the scale puts the lengths
  in order and keeps every count you can choose distinguishable; it does not
  count harakat as letters. One letter per haraka would stretch madd lazim to
  six identical letters.
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
| `ٱلْقُرْءَانُ` | `аль-qур-аан` |
| `جُزْءٌ` | `джуз` |

`مِنْ بَعْدِ` is iqlab: a nun sakina before `ب` turns into a mim, and the words
do not merge while it happens. The last two lines are hamza: inside a word it is
written as a hyphen, a break (`аль-qур-аан`), and at the end of a word it is not
written at all (`джуз`), since nothing sounds after it. The hard sign it used to
be written with clashed with the digraphs: "джузъ" read as if the word had `ذ`
"зъ".

---

## Web version

https://shoralbrt.github.io/Transliterator/

A single page built with Blazor WebAssembly: the core is compiled to wasm and does
all the work in the browser. There is no server, and the page is published from
`master` to GitHub Pages automatically.

* **One field.** Arabic text goes into the field in the centre, and the result
  appears under it, recalculated on every edit. A button copies it. Text without
  diacritics is accepted too, although without harakat the reading is incomplete.
* **Ready-made surahs** from the corpus open with the "Выбрать суру" button. The
  text goes in one ayah per line, and the result keeps the lines.
* **How to read** — the button next to it picks a reading tier: full tajweed
  (the default), partial tajweed — "Неполный таджвид" on the page (how long to
  hold madd arid, madd lin, muttasil and munfasil, and which rules to leave out:
  qalqalah, the ghunnah rules — idgham with ghunnah, iqlab, ikhfa — and madd
  silah; a rule left out reads as in plain reading) or plain Arabic reading
  without tajweed. The output changes at once, every rule is listed with its
  description from the rule catalog, and the choice is remembered in the
  browser.
* **Feedback** — an icon in the corner of the page (a footer line on a phone).
  The message goes to a small Cloudflare Workers function
  ([`feedback-worker/`](feedback-worker/README.md)) that checks Turnstile and
  opens an issue in this repository, so no GitHub account is needed. Until the
  function is configured, and as a fallback when sending fails, the page opens a
  pre-filled new-issue form on GitHub that the person submits with their own
  account. The text from the field and its transliteration are attached only
  when the person ticks the box.
* **Profile** — the third button under the field. It says what it picks and
  what is picked now: "Профиль: Standard" by default, your own profile under its
  own name ("Профиль: Мой"); a long name is cut with an ellipsis, the caption
  stays. It picks a built-in profile (`Standard`,
  `Latin`) or your own; "Создать копию" makes your own copy at once, and its
  graphemes can be rewritten in a searchable table, for example `ع` as `ʿ`
  instead of `'`. The panel repeats the output, so a change is seen while typing.
  A grapheme that can no longer be told apart — the same as another letter's,
  or made of two others, like "къ" for `ق` = "к" + "ъ" when hamza is set to "ъ" — gets a
  warning next to the letter with a corpus word as an example; the profile is
  saved anyway.
  Your profiles and the choice live in the browser's localStorage. JSON export
  and import (`ProfileJson`) stay in the core and are not on the page yet.

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
бисми-лляяhи-ррохIмаани-ррохIиим
```

With an explicit profile:

```bash
dotnet run --project Transliterator.Cli -- "بِسْمِ ٱللَّهِ ٱلرَّحْمَـٰنِ ٱلرَّحِيمِ" Latin
```

A ready-made surah from the corpus by its number, with the profile as an option:

<!-- readme-example:cli-surah -->
```bash
dotnet run --project Transliterator.Cli -- --surah 112 --profile Latin
```

<!-- readme-example:cli-surah-output -->
```
qul huwa-llaahu aḥad 1
al-laahu-ṣṣomad 2
lam yalid walam yuulad 3
walam yakul-lahuu kufuwan aḥad 4
```

Long text is easier to take from a file or from stdin:

```bash
dotnet run --project Transliterator.Cli -- --file text.txt
dotnet run --project Transliterator.Cli -- --stdin < text.txt
```

* `--profiles` and `--surahs` show what there is to choose from; `--help` shows
  every option.
* Only the result goes to stdout; messages and errors go to stderr, so the output
  can be redirected to a file.
* The file and stdin are read as UTF-8. A file in another encoding is rejected
  with an explanation instead of transliterating garbled text. Windows PowerShell 5
  does not pipe text (`Get-Content … |`) as UTF-8 — use `--file` there.

Running with no arguments starts an interactive mode: the application asks what
to transliterate — typed text, a surah from the corpus or a file — then shows the
profiles and asks which one to write with.

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
| `final` | unvowelled hamza at the end of a word | `"ء\|final": ""` — `джуз`; inside a word the base `"ء": "-"` applies, `йу-минуун` |
| `ghunna` | under nasalisation | `"ن\|ghunna": "н"` |
| `qalqalah` | the echo when an unvowelled stop is released | `"ب\|qalqalah": ""` |
| `qalqalah-strong` | the same at a pause; falls back to `qalqalah` | not set in `Standard` |

An empty value is a legitimate entry rather than an omission: this is how the
qalqalah echo, the initial hamza and the word-final hamza are deliberately set
in `Standard`. The reasoning is under "Открытые решения" in the roadmap.

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
* **Length** is stored in harakat (2/4/5/6) and only becomes a repeated grapheme
  at the final stage; there are no doubled vowels in a profile.

### Example profile

An excerpt from `Transliterator.Core/Resources/Profiles/Standard.json`:

<!-- readme-example:profile -->
```json
{
  "Name": "Standard",
  "Description": "Расширенная кириллица. Ключ вида \"буква|вариант\" переопределяет базовую графему...",
  "Rules": {
    "ء": "-",
    "ء|initial": "",
    "ء|hiatus": "-",
    "ء|final": "",
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
