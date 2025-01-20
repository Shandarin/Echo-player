using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Echo.Models;
using Newtonsoft.Json.Linq;
using System.Linq;
using Newtonsoft.Json;

namespace Echo.Services
{
    public partial class OxfordDictService : ObservableObject
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl = "https://od-api-sandbox.oxforddictionaries.com/api/v2";

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _errorMessage;

        [ObservableProperty]
        private WordModel wordModel;

        public OxfordDictService()
        {
            var config = ConfigService.Instance.GetOxfordDictionaryConfig();

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("app_id", config.AppId);
            _httpClient.DefaultRequestHeaders.Add("app_key", config.AppKey);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            //Debug.WriteLine($"OxfordDictService initialized:{config.AppId}");
        }

        public async Task<(string headword, JObject details)> GetWordDetailsAsync(string word, string sourceLang = "en")
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;

                var url = $"{_baseUrl}/words/{sourceLang}?q={word}";
                var response = await _httpClient.GetStringAsync(url);
                var json = JObject.Parse(response);

                
                var headword = json["results"]?[0]?["id"]?.ToString();
                var wordModel = new WordModel
                {
                    Word = headword,
                    Inflections = json["results"]?[0]?["lexicalEntries"]?[0]?["entries"]?[0]?["inflections"]
                    ?.Select(inflection =>
                    {
                        return inflection["inflectedForm"]?.ToString();
                    })
                    .Where(inflectedForm => !string.IsNullOrEmpty(inflectedForm))
                    .ToList()
                    ?? new List<string>()
                };


                if (string.IsNullOrEmpty(headword))
                {
                    throw new Exception("Word not found");
                }

                //wordModel = 

                return (headword, json);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return (null, null);
            }
            finally
            {
                IsLoading = false;
            }
        }


        public async Task<WordModel> GetTranslationsAsync(
            string headword,
            string sourceLang = "en",
            string targetLang = "zh")
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;

                var url = $"{_baseUrl}/translations/{sourceLang}/{targetLang}/{headword}";
                var response = await _httpClient.GetStringAsync(url);
                var json = JObject.Parse(response);

                // Create WordModel instance
                //var wordModel = new WordModel
                /
                
                    //Word = headword,
                    Pronounciations = json["results"]?[0]?["lexicalEntries"]?[0]?["entries"]?[0]?["pronunciations"]
                    ?.Select(pronunciation =>
                    {
                        // 获取原始 dialect
                        var originalDialect = pronunciation["dialects"]?[0]?.ToString() ?? "Unknown";

                        // 转成我们需要的 "British" 或 "US" 或 "Unknown"
                        var dialect = originalDialect switch
                        {
                            "British English" => "British",
                            "American English" => "US",
                            _ => "Unknown"
                        };

                        // 音频地址
                        var audioFile = pronunciation["audioFile"]?.ToString() ?? string.Empty;

                        // 音标
                        var phoneticSpelling = pronunciation["phoneticSpelling"]?.ToString() ?? string.Empty;

                        // 返回 PronunciationModel
                        return new PronunciationModel
                        {
                            Dialect = dialect,
                            AudioFile = audioFile,
                            PhoneticSpelling = phoneticSpelling
                        };
                    })
                    .ToList()
                    ?? new List<PronunciationModel>(),

                    Definitions = json["results"]?[0]?["lexicalEntries"]?
                        .ToDictionary(
                            lexicalEntry => lexicalEntry["lexicalCategory"]?["text"]?.ToString() ?? "Unknown",
                            lexicalEntry => string.Join("；", lexicalEntry["entries"]?
                                .SelectMany(entry => entry["senses"]?
                                    .Select(sense => sense["translations"]?[0]?["text"]?.ToString())
                                    .Where(translation => !string.IsNullOrEmpty(translation)) ?? Enumerable.Empty<string>()
                                )
                            ) ?? string.Empty
                        ) ?? new Dictionary<string, string>(),
                    //Inflections = json["results"]?[0]?["lexicalEntries"]?[0]?["entries"]?[0]?["grammaticalFeatures"]
                    //    ?.ToDictionary(
                    //        feature => feature["id"]?.ToString() ?? "Unknown",
                    //        feature => feature["text"]?.ToString() ?? string.Empty
                    //    ),
                    Synonyms = json["results"]?[0]?["lexicalEntries"]?[0]?["entries"]?[0]?["senses"]
                        ?.SelectMany(sense => sense["synonyms"]?
                            .Select(synonym => synonym["text"]?.ToString())
                            .Where(synonym => !string.IsNullOrEmpty(synonym)) ?? Enumerable.Empty<string>()
                        ).ToList(),
                    Senses = json["results"]?[0]?["lexicalEntries"]?[0]?["entries"]?[0]?["senses"]
                        ?.Select(sense => new SenseModel
                        {
                            //Word = headword,
                            Category = json["results"]?[0]?["lexicalEntries"]?[0]?["lexicalCategory"]?["text"]?.ToString() ?? "Unknown",
                            Definition = sense["translations"]?[0]?["text"]?.ToString() ?? "No definition available",
                            Description = sense["notes"]?.FirstOrDefault()?["text"]?.ToString() ?? "No description available",
                            Examples = sense["examples"]
                                ?.ToDictionary(
                                    example => example["text"]?.ToString() ?? "Unknown",
                                    example => example["translations"]?[0]?["text"]?.ToString() ?? "No translation available"
                                )
                        }).ToList() ?? new List<SenseModel>()
                };


                JObject jsonOut = JObject.FromObject(wordModel);

                Debug.WriteLine(jsonOut.ToString(Formatting.Indented));
                return wordModel;
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                throw new Exception($"Error fetching translations: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}