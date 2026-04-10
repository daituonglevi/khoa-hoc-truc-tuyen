namespace ELearningWebsite.Models
{
    public class BlobStorageSettings
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string ContainerName { get; set; } = "private-media";
        public int ReadSasMinutes { get; set; } = 30;
        public int MaxFileSizeMb { get; set; } = 500;
        public List<string> AllowedExtensions { get; set; } = new();
    }
}