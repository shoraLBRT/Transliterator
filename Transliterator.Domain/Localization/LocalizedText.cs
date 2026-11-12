using System.Collections;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Transliterator.Domain.Localization
{
    /// <summary>
    /// Текст данных на каждом языке страницы (D7): название правила таджвида, его
    /// описание, имя суры. В JSON — объект «код языка: текст»:
    /// <c>{ "ru": "Мадд лязим", "en": "Madd lazim" }</c>. Строки интерфейса живут
    /// в ресурсах (<c>.resx</c>), а тексты данных — рядом с самими данными, чтобы
    /// перевод вычитывался там же, где оригинал.
    /// </summary>
    [JsonConverter(typeof(LocalizedTextConverter))]
    public sealed class LocalizedText : IReadOnlyDictionary<string, string>
    {
        private readonly Dictionary<string, string> _texts;

        public LocalizedText(IDictionary<string, string>? texts = null) =>
            _texts = new Dictionary<string, string>(texts ?? new Dictionary<string, string>(), StringComparer.OrdinalIgnoreCase);

        /// <summary>Текст на языке культуры интерфейса.</summary>
        public string Current => For(CultureInfo.CurrentUICulture);

        /// <summary>
        /// Текст на языке культуры: <c>en-US</c> берёт <c>en</c>. Нет такого языка —
        /// текст языка по умолчанию, нет и его — любой: лучше русский текст, чем пустое
        /// место. Каталог без текста на каком-нибудь языке страницы не загружается,
        /// так что откат — на случай языка, которого у страницы нет.
        /// </summary>
        public string For(CultureInfo culture) => For(PageLanguages.Of(culture).Code);

        public string For(string language)
        {
            if (Has(language))
                return _texts[language];

            if (Has(PageLanguages.Default.Code))
                return _texts[PageLanguages.Default.Code];

            return _texts.Values.FirstOrDefault(text => !string.IsNullOrWhiteSpace(text)) ?? string.Empty;
        }

        /// <summary>Есть непустой текст на этом языке.</summary>
        public bool Has(string language) =>
            _texts.TryGetValue(language, out var text) && !string.IsNullOrWhiteSpace(text);

        /// <summary>Языки страницы, на которых текста нет.</summary>
        public IReadOnlyList<string> MissingLanguages() =>
            PageLanguages.All.Select(language => language.Code).Where(code => !Has(code)).ToList();

        public override string ToString() => Current;

        public string this[string key] => _texts[key];
        public IEnumerable<string> Keys => _texts.Keys;
        public IEnumerable<string> Values => _texts.Values;
        public int Count => _texts.Count;
        public bool ContainsKey(string key) => _texts.ContainsKey(key);
        public bool TryGetValue(string key, out string value) => _texts.TryGetValue(key, out value!);
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _texts.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>Объект «язык: текст» и обратно.</summary>
    public sealed class LocalizedTextConverter : JsonConverter<LocalizedText>
    {
        public override LocalizedText? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("A localized text must be an object of language codes and texts, e.g. { \"ru\": \"…\", \"en\": \"…\" }.");

            var texts = JsonSerializer.Deserialize<Dictionary<string, string>>(ref reader, options);
            return new LocalizedText(texts);
        }

        public override void Write(Utf8JsonWriter writer, LocalizedText value, JsonSerializerOptions options) =>
            JsonSerializer.Serialize(writer, value.ToDictionary(pair => pair.Key, pair => pair.Value), options);
    }
}
