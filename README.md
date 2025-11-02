# Transliterator

[Русская версия](README.ru.md)

**Transliterator** is a tool for transliterating Arabic text with diacritics into extended Russian Cyrillic. The project is designed with future expansion in mind: support for various Tajweed rules, custom transliteration profiles, and potentially other target languages.

---

## Features

* Transliterates Arabic text with diacritics into extended Russian Cyrillic.
* Handles certain Tajweed rules, including Wasla, Alif Maqsura, and emphatic letters.
* Supports **custom transliteration profiles**: change letter mappings for specific needs.
* Built as a **CLI application**, with future plans for API support.
* Unit tests for individual rules ensure transliteration correctness.

> Currently, the main rules are implemented, and functionality will expand as additional Tajweed rules and profiles are added.

---

## Example

**Input (Arabic text with diacritics):**

```
بِسْمِ ٱللَّهِ ٱلرَّحْمَـٰنِ ٱلرَّحِيمِ ١ ٱلْحَمْدُ لِلَّهِ رَبِّ ٱلْعَـٰلَمِينَ ٢ ٱلرَّحْمَـٰنِ ٱلرَّحِيمِ ٣ مَـٰلِكِ يَوْمِ ٱلدِّينِ
```

**Transliteration result:**

```
бисми-лляhи-ррохIмаани-ррохIийм 1 аль-хIамду лилляhи робби-ль'аалямиин 2 ар-рохIмаани-ррохIиим 3 маалики йауми-ддиин
```

---

## Usage

The project is currently a **CLI application** that accepts a string of Arabic text and returns the transliterated result.

Example:

```bash
dotnet run "بِسْمِ ٱللَّهِ ٱلرَّحْمَـٰنِ ٱلرَّحِيمِ"
```

Output:

```
бисми-лляhи-ррохIмаани-ррохIийм
```

> In the future, profile selection, file input, and API support will be added.

---

## Custom Profiles

* Profiles allow defining how each Arabic letter and diacritic is transliterated.
* Profiles can be created for individual user needs or other target languages.
* Profiles enable extending functionality without modifying the core transliterator code.

Example profile:

```csharp
var profile = new TransliterationProfile
{
    Name = "Standard",
    Rules = new Dictionary<string, string>
    {
        {"ا", "а"},
        {"أ", "а"},
        {"إ", "и"},
        {"آ", "а̄"},
        {"ٱ", "á"},
        {"ى", "а"},
        {"ٰ", "а"},
        {"ۤ", ""},
        {"ٲ", "а"},
        {"ٳ", "а"},
        {"ب", "б"},
        {"ت", "т"},
        {"ث", "с́"},
        {"ج", "дж"},
        {"ح", "хI"},
        {"خ", "хъ"},
        {"د", "д"},
        {"ذ", "зъ"},
        {"ر", "р"},
        {"ز", "з"},
        {"س", "с"},
        {"ش", "щ"},
        {"ص", "сI"},
        {"ض", "дI"},
        {"ط", "тI"},
        {"ظ", "зI"},
        {"ع", "'"},
        {"غ", "гъ"},
        {"ف", "ф"},
        {"ق", "q"},
        {"ك", "к"},
        {"ل", "л"},
        {"م", "м"},
        {"ن", "н"},
        {"ه", "h"},
        {"و", "у"},
        {"ي", "й"},
        {"َ", "а"},
        {"ُ", "у"},
        {"ِ", "и"},
        {"ٌ", "ун"},
        {"ٍ", "ин"},
        {"ً", "ан"},
        {"ـٰ", "а"},
        {"٠", "0"},
        {"١", "1"},
        {"٢", "2"},
        {"٣", "3"},
        {"٤", "4"},
        {"٥", "5"},
        {"٦", "6"},
        {"٧", "7"},
        {"٨", "8"},
        {"٩", "9"}
    }
};
```

> This dictionary specifies how each Arabic letter and diacritic is transliterated into Cyrillic. Numbers remain unchanged.

---

## Unit Tests

* Partial **unit tests for individual transliteration rules** are implemented to verify correct handling of Wasla, Alif Maqsura, and other cases.
* Tests use **xUnit**.
* To run tests in Visual Studio:

  1. Open the `Test Explorer` (`Test → Test Explorer`).
  2. Build the solution (`Build → Build Solution`).
  3. Click `Run All` or select individual tests to run.

> Unit tests ensure that changes to transliteration rules do not break existing functionality.

---

## Future Development

* Addition of all major Tajweed rules for more accurate transliteration.
* Expansion of profiles for different languages and user customization.
* Support for file input and API integration for external applications.

---