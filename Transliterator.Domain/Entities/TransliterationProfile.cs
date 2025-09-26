namespace Transliterator.Domain.Entities
{
    public class TransliterationProfile
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Dictionary<string, string> Rules { get; set; } = new();

        public TransliterationProfile() { }

        public TransliterationProfile(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }
}
