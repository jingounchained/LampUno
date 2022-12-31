using System.Text.Json.Serialization;
namespace Lamp
{
    public class AssetFile
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("browser_download_url")]
        public string DownloadURL { get; set; }
        public string LocalFilepath
        {
            get
            {
                return FileHandler.LocalDirectory + "\\" + Name;
            }
        }
    }
}