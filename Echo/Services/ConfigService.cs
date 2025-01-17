using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Echo.Models;

namespace Echo.Services
{
    public class ConfigService
    {
        private static readonly Lazy<ConfigService> _instance =
            new Lazy<ConfigService>(() => new ConfigService());

        public static ConfigService Instance => _instance.Value;

        private AppConfig _config;

        private ConfigService()
        {
            LoadConfiguration();
        }

        private void LoadConfiguration()
        {
            try
            {
                var configPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "config.json");

                if (!File.Exists(configPath))
                {
                    throw new FileNotFoundException("Configuration file not found", configPath);
                }

                var jsonString = File.ReadAllText(configPath);
                _config = JsonSerializer.Deserialize<AppConfig>(jsonString);
            }
            catch (Exception ex)
            {
                // 在实际应用中应该使用proper logging
                Console.WriteLine($"Error loading configuration: {ex.Message}");
                _config = new AppConfig
                {
                    OxfordDictionary = new OxfordDictionaryConfig()
                };
            }
        }

        public OxfordDictionaryConfig GetOxfordDictionaryConfig()
        {
            return _config?.OxfordDictionary ?? new OxfordDictionaryConfig();
        }

        public OpenAIConfig GetOpenAIConfig()
        {

           return _config?.OpenAI ?? new OpenAIConfig();
        }

    }
}