using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace Lamp
{
    public class ReleaseFile
    {
        [JsonPropertyName("tag_name")]
        public string Version { get; set; }
        [JsonPropertyName("html_url")]
        public string ReleaseURL { get; set; }
        [JsonPropertyName("assets_url")]
        public string AssetsURL { get; set; }
        public Dictionary<string, AssetFile> Assets { get; set; }
        public bool HasAssets { get; set; }
        public void LoadAssets()
        {
            HasAssets = FileHandler.LoadReleaseAssets(this).Result;
        }

    }
}
