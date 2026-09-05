namespace Transliterator.Core.Models
{
    public class StorageSettings
    {
        public string ProfilesPath { get; set; } = "Resources/Profiles";

        /// <summary>Корпус примеров — такой же ресурс ядра, как и профили.</summary>
        public string CorpusPath { get; set; } = "Resources/Corpus";
    }
}
