using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Echo.Models;
using Newtonsoft.Json.Linq;
using System.Linq;
using Newtonsoft.Json;
using System.Windows;
using Echo.Properties;

namespace Echo.Services
{
    public partial class OxfordDictService : ObservableObject
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl = "https://od-api.oxforddictionaries.com/api/v2";

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _errorMessage;

        [ObservableProperty]
        private WordModel wordModel;

        public OxfordDictService()
        {
            //var config = ConfigService.Instance.GetOxfordDictionaryConfig();

            //var ApiId = Settings.Default.OxfordApiId;
            //var ApiKey = Settings.Default.OxfordApiKey;

            _httpClient = new HttpClient();
            //_httpClient.DefaultRequestHeaders.Add("app_id", ApiId);
            //_httpClient.DefaultRequestHeaders.Add("app_key", ApiKey);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            //Debug.WriteLine($"OxfordDictService initialized:{config.AppId}");
        }

        private async Task<JObject?> GetRequestAsync(string url)
        {
            if (_httpClient.DefaultRequestHeaders.Contains("app_id"))
            {
                _httpClient.DefaultRequestHeaders.Remove("app_id");
            }
            if (_httpClient.DefaultRequestHeaders.Contains("app_key"))
            {
                _httpClient.DefaultRequestHeaders.Remove("app_key");
            }

            // 从配置中获取最新的 API 信息
            _httpClient.DefaultRequestHeaders.Add("app_id", Settings.Default.OxfordApiId);
            _httpClient.DefaultRequestHeaders.Add("app_key", Settings.Default.OxfordApiKey);

            try
            {
                var response = await _httpClient.GetAsync(url);
                string responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JObject.Parse(responseString);
                }
                else
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                        response.StatusCode == System.Net.HttpStatusCode.Forbidden
                        )
                    {
                        MessageBox.Show("API key wrong or expired.\n Consider using Echo API");
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError ||
                             response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable ||
                             response.StatusCode == System.Net.HttpStatusCode.GatewayTimeout)
                    {
                        MessageBox.Show("Server error, please try later");
                    }
                    else
                    {
                        MessageBox.Show("Request failed: " + response.StatusCode);
                    }
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Request failed: {ex.Message}");
                return null;
            }
        }

        public async Task<(string headword, JObject details)> GetWordDetailsAsync(string word, string sourceLang)
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;

                var url = $"{_baseUrl}/words/{sourceLang}?q={word}";
                var json = await GetRequestAsync(url);
                if (json == null)
                {
                    ErrorMessage = "Eorro requesting word";
                    return (null, null);
                }

                //var headwords = new List<string>();
                var results = json["results"] as JArray;
                string selectedHeadword = string.Empty;
                if (results != null && results.Count > 0)
                {
                    var selectedResult = results
                        .OrderByDescending(result => result.ToString().Length)
                        .First();
                    selectedHeadword = selectedResult["id"]?.ToString() ?? string.Empty;
                }

                // 遍历所有 results 聚合 Inflections 与 OriginalSenses
                var inflections = new List<string>();
                var originalSenses = new List<SenseModel>();

                if (results != null)
                {
                    foreach (var result in results)
                    {
                        var lexicalEntries = result["lexicalEntries"] as JArray;
                        if (lexicalEntries == null)
                            continue;

                        foreach (var lexicalEntry in lexicalEntries)
                        {
                            // 获取词性信息（用于后续 sense 分类）
                            string category = lexicalEntry["lexicalCategory"]?["text"]?.ToString() ?? "Other";

                            var entriesArray = lexicalEntry["entries"] as JArray;
                            if (entriesArray == null)
                                continue;

                            foreach (var entry in entriesArray)
                            {
                                // 处理 inflections
                                var infls = entry["inflections"] as JArray;
                                if (infls != null)
                                {
                                    foreach (var infl in infls)
                                    {
                                        var form = infl["inflectedForm"]?.ToString();
                                        if (!string.IsNullOrEmpty(form) && !inflections.Contains(form))
                                        {
                                            inflections.Add(form);
                                        }
                                    }
                                }

                                // 处理 senses
                                var senses = entry["senses"] as JArray;
                                if (senses != null)
                                {
                                    foreach (var s in senses)
                                    {
                                        // 获取释义的第一条 definition
                                        var defArray = s["definitions"] as JArray;
                                        string firstDef = defArray != null && defArray.Count > 0 ? defArray[0]?.ToString() : string.Empty;

                                        // 同义词
                                        var synonyms = new List<string>();
                                        var synArray = s["synonyms"] as JArray;
                                        if (synArray != null)
                                        {
                                            foreach (var syn in synArray)
                                            {
                                                var synText = syn["text"]?.ToString();
                                                if (!string.IsNullOrEmpty(synText) && !synonyms.Contains(synText))
                                                {
                                                    synonyms.Add(synText);
                                                }
                                            }
                                        }

                                        // 示例句
                                        var examples = new Dictionary<string, string>();
                                        var exArr = s["examples"] as JArray;
                                        if (exArr != null)
                                        {
                                            foreach (var ex in exArr)
                                            {
                                                var exText = ex["text"]?.ToString() ?? "";
                                                if (!string.IsNullOrEmpty(exText) && !examples.ContainsKey(exText))
                                                {
                                                    examples.Add(exText, ""); // 翻译信息可之后补充
                                                }
                                            }
                                        }

                                        var senseModel = new SenseModel
                                        {
                                            Category = category,
                                            Definition = firstDef,
                                            Examples = examples,
                                            //Synonyms = synonyms
                                        };

                                        originalSenses.Add(senseModel);
                                    }
                                }
                            }
                        }
                    }
                }

                // 更新 wordModel 中相应的字段（可继续扩展其它字段）
                wordModel.Word = selectedHeadword;
                wordModel.Inflections = inflections;
                wordModel.OriginalSenses = originalSenses;

                return (selectedHeadword, json);
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task<WordModel> GetTranslationsAsync(
            string headword,
            string sourceLang,
            string targetLang)
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;

                var url = $"{_baseUrl}/translations/{sourceLang}/{targetLang}/{headword}";
                var json = await GetRequestAsync(url);

                if (json == null)
                {
                    ErrorMessage = "Error Requesting translation";
                    return wordModel;
                }

                wordModel.SourceLanguageCode = sourceLang;
                wordModel.TargetLanguageCode = targetLang;

                var pronunciations = new List<PronunciationModel>();
                var synonyms = new List<string>();
                var senses = new List<SenseModel>();

                var results = json["results"] as JArray;
                if (results != null)
                {
                    foreach (var result in results)
                    {
                        var lexicalEntries = result["lexicalEntries"] as JArray;
                        if (lexicalEntries == null)
                            continue;

                        foreach (var lexicalEntry in lexicalEntries)
                        {
                            // 处理发音信息
                            var entriesArray = lexicalEntry["entries"] as JArray;
                            if (entriesArray != null)
                            {
                                foreach (var entry in entriesArray)
                                {
                                    var prons = entry["pronunciations"] as JArray;
                                    if (prons != null)
                                    {
                                        foreach (var p in prons)
                                        {
                                            var originalDialect = p["dialects"]?[0]?.ToString() ?? "Other";
                                            var dialect = originalDialect switch
                                            {
                                                "British English" => "British",
                                                "American English" => "US",
                                                _ => "Other"
                                            };

                                            var audioFile = p["audioFile"]?.ToString() ?? string.Empty;
                                            var phoneticSpelling = p["phoneticSpelling"]?.ToString() ?? string.Empty;

                                            pronunciations.Add(new PronunciationModel
                                            {
                                                Dialect = dialect,
                                                AudioFile = audioFile,
                                                PhoneticSpelling = phoneticSpelling
                                            });
                                        }
                                    }

                                    // 处理同义词及翻译释义
                                    var sensesArray = entry["senses"] as JArray;
                                    if (sensesArray != null)
                                    {
                                        foreach (var s in sensesArray)
                                        {
                                            // 同义词
                                            var synArray = s["synonyms"] as JArray;
                                            if (synArray != null)
                                            {
                                                foreach (var syn in synArray)
                                                {
                                                    var synText = syn["text"]?.ToString();
                                                    if (!string.IsNullOrEmpty(synText) && !synonyms.Contains(synText))
                                                        synonyms.Add(synText);
                                                }
                                            }

                                            // 获取翻译释义（Definition）
                                            string defText = s["translations"]?[0]?["text"]?.ToString() ?? "";

                                            // 示例句
                                            var examples = new Dictionary<string, string>();
                                            var exArr = s["examples"] as JArray;
                                            if (exArr != null)
                                            {
                                                foreach (var ex in exArr)
                                                {
                                                    var exText = ex["text"]?.ToString() ?? "";
                                                    var transText = ex["translations"]?[0]?["text"]?.ToString() ?? "";
                                                    if (!string.IsNullOrEmpty(exText) && !examples.ContainsKey(exText))
                                                        examples.Add(exText, transText);
                                                }
                                            }

                                            senses.Add(new SenseModel
                                            {
                                                Category = lexicalEntry["lexicalCategory"]?["text"]?.ToString() ?? "Other",
                                                Definition = defText,
                                                Examples = examples
                                            });
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // 更新 wordModel 的相关字段
                wordModel.Pronounciations = pronunciations;
                wordModel.Synonyms = synonyms;
                wordModel.Senses = senses;


                var definitions = new Dictionary<string, string>();

                // 1. 对 Senses 按照词性进行分组，提取每个词性的所有非空释义
                var groupedSenses = wordModel.Senses.GroupBy(sense => sense.Category);
                foreach (var group in groupedSenses)
                {
                    // 取出该组中所有非空释义，并去重、去前后空白
                    var defs = group
                        .Select(sense => sense.Definition?.Trim())
                        .Where(def => !string.IsNullOrWhiteSpace(def))
                        .Distinct()
                        .ToList();

                    // 如果当前词性下 Senses 中没有有效释义，
                    // 则尝试从 OriginalDefinitions 中进行补充
                    if (defs.Count == 0 &&
                        wordModel.OriginalDefinitions.ContainsKey(group.Key) &&
                        !string.IsNullOrWhiteSpace(wordModel.OriginalDefinitions[group.Key]))
                    {
                        defs.Add(wordModel.OriginalDefinitions[group.Key].Trim());
                    }

                    if (defs.Count > 0)
                    {
                        definitions[group.Key] = string.Join(", ", defs);
                    }
                }

                // 2. 对于 OriginalDefinitions 中存在但 Senses 中没有的词性，直接添加原始释义
                var missingCategories = wordModel.OriginalDefinitions.Keys.Except(definitions.Keys);
                foreach (var category in missingCategories)
                {
                    if (!string.IsNullOrWhiteSpace(wordModel.OriginalDefinitions[category]))
                    {
                        definitions[category] = wordModel.OriginalDefinitions[category].Trim();
                    }
                }

                wordModel.Definitions = definitions;


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