using System.Text.Json.Serialization;

namespace Echo.Models
{
    public class AppConfig
    {
        [JsonPropertyName("oxfordDictionary")]
        public OxfordDictionaryConfig OxfordDictionary { get; set; }

        [JsonPropertyName("openAI")]
        public OpenAIConfig OpenAI { get; set; }
    }

    public class OxfordDictionaryConfig
    {
        [JsonPropertyName("appId")]
        public string AppId { get; set; }

        [JsonPropertyName("appKey")]
        public string AppKey { get; set; }
    }

    public class OpenAIConfig
    {
        [JsonPropertyName("key")]
        public string Key { get; set; }

        [JsonPropertyName("model")]
        public string Model { get; set; }
    }
}