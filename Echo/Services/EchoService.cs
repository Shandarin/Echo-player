﻿using Echo.Managers;
using Echo.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Packaging;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace Echo.Services
{
    public class EchoService
    {
        private static readonly string BaseUrl = "http://echo-player.com";
        private static readonly string ApiKey = APIKeyManager.GetOrCreateApiKey();


        public static async Task<string> OpenAIRequest(string sentence, string sourcelanguagecode, string targetlanguagecode)
        {
            string endpoint = $"{BaseUrl}/api/openai";
            var payload = new
            {
                sentence = sentence,
                sourcelanguagecode = sourcelanguagecode,
                targetlanguagecode = targetlanguagecode
            };
            string result = await PostRequestAsync(endpoint, payload);
            return result;
        }

        public static async Task<string> OxfordAPIRequest(string word,string sourcelanguagecode,string targetlanguagecode)
        {
            string endpoint = $"{BaseUrl}/api/oxford";
            var payload = new
            {
                word = word,
                sourcelanguagecode = sourcelanguagecode,
                targetlanguagecode = targetlanguagecode
            };


            string result = await PostRequestAsync(endpoint, payload);

            return result;
        }

        private static async Task<string> PostRequestAsync(string url, object payload)
        {
            using (var client = new HttpClient())
            {
                // 设置请求头
                client.DefaultRequestHeaders.Add("X-Api-Key", ApiKey);

                // 序列化请求体
                var jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                try
                {
                    // 发送 POST 请求
                    var response = await client.PostAsync(url, content);

                    //response.Content.

                    // 读取响应
                    string responseString = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"responseString {responseString}");

                    if (response.IsSuccessStatusCode)
                    {
                        return responseString;
                    }
                    else
                    { 
                        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)//401
                        {
                            MessageBox.Show(responseString);
                        }else if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError | response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable | response.StatusCode == System.Net.HttpStatusCode.GatewayTimeout)//500,502,504
                        {
                            MessageBox.Show("Server error, please try later");
                        }
                        else
                        {
                            //MessageBox.Show("Request failed: " + response.StatusCode);
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
        }

        //以后改成强类型解析
        public static async Task<WordModel?> ParseOxfordAsync(string json)
        {
            var root = JObject.Parse(json);

            // 构造最终返回的 WordModel
            var model = new WordModel
            {
                IsSuccess = true,
                Pronounciations = new List<PronunciationModel>(),
                Definitions = new Dictionary<string, string>(),
                Inflections = new List<string>(),
                Synonyms = new List<string>(),
                Senses = new List<SenseModel>(),
                OriginalSenses = new List<SenseModel>(),
                OriginalDefinitions = new Dictionary<string, string>()
            };

            // 1. 先读最外层
            model.Word = root["headword"]?.ToString() ?? string.Empty;
            model.SourceLanguageCode = root["source_lang"]?.ToString() ?? "";
            model.TargetLanguageCode = root["target_lang"]?.ToString() ?? "";

            // 如果解析失败，model.IsSuccess = false; 视情况而定
            if (string.IsNullOrEmpty(model.Word) || string.IsNullOrEmpty(model.SourceLanguageCode) || string.IsNullOrEmpty(model.TargetLanguageCode))
            {
                model.IsSuccess = false;
                return null;
            }


            // 2. 解析 entry_json (原文的 Oxford 信息)，准备填充 OriginalSenses / OriginalDefinitions / Inflections / Pronounciations
            var entryJson = root["entry_json"] as JObject;
            if (entryJson != null)
            {
                // 2.1 取 results 数组
                var results = entryJson["results"] as JArray;
                if (results != null)
                {
                    foreach (var result in results)
                    {
                        // lexicalEntries
                        var lexicalEntries = result["lexicalEntries"] as JArray;
                        if (lexicalEntries == null) continue;

                        foreach (var lexEntry in lexicalEntries)
                        {
                            // 词性
                            string category = lexEntry["lexicalCategory"]?["text"]?.ToString() ?? "";

                            // entries
                            var entriesArray = lexEntry["entries"] as JArray;
                            if (entriesArray == null) continue;

                            foreach (var entry in entriesArray)
                            {
                                // inflections
                                var infls = entry["inflections"] as JArray;
                                if (infls != null)
                                {
                                    foreach (var infl in infls)
                                    {
                                        var form = infl["inflectedForm"]?.ToString();
                                        if (!string.IsNullOrEmpty(form))
                                        {
                                            model.Inflections.Add(form);
                                        }
                                    }
                                }

                                // pronunciations
                                var prons = entry["pronunciations"] as JArray;
                                if (prons != null)
                                {
                                    foreach (var p in prons)
                                    {
                                        var dialArr = p["dialects"] as JArray;
                                        string dial = dialArr != null && dialArr.Count > 0 ? dialArr[0]?.ToString() : "";

                                        var audio = p["audioFile"]?.ToString() ?? "";
                                        var spelling = p["phoneticSpelling"]?.ToString() ?? "";

                                        model.Pronounciations.Add(new PronunciationModel
                                        {
                                            Dialect = dial,
                                            AudioFile = audio,
                                            PhoneticSpelling = spelling
                                        });
                                    }
                                }

                                // senses
                                var senses = entry["senses"] as JArray;
                                if (senses != null)
                                {
                                    foreach (var s in senses)
                                    {
                                        // definitions
                                        var defArray = s["definitions"] as JArray;
                                        string firstDef = defArray != null && defArray.Count > 0 ? defArray[0]?.ToString() : "";

                                        // synonyms
                                        var synArray = s["synonyms"] as JArray;
                                        if (synArray != null)
                                        {
                                            foreach (var synItem in synArray)
                                            {
                                                var synText = synItem["text"]?.ToString();
                                                if (!string.IsNullOrEmpty(synText))
                                                {
                                                    model.Synonyms.Add(synText);
                                                }
                                            }
                                        }

                                        // examples
                                        var examplesDict = new Dictionary<string, string>();
                                        var exArr = s["examples"] as JArray;
                                        if (exArr != null)
                                        {
                                            foreach (var ex in exArr)
                                            {
                                                var exText = ex["text"]?.ToString() ?? "";
                                                // 原文释义里没有给出翻译，可以留空
                                                examplesDict[exText] = "";
                                            }
                                        }

                                        // 构造一个 SenseModel
                                        var senseModel = new SenseModel
                                        {
                                            ExplanationLanguageCode = model.SourceLanguageCode,
                                            Category = category,
                                            Definition = firstDef,
                                            Examples = examplesDict
                                        };

                                        model.OriginalSenses.Add(senseModel);

                                        // 也可以把“动词: definition”之类放到 OriginalDefinitions 里
                                        if (!model.OriginalDefinitions.ContainsKey(category))
                                        {
                                            model.OriginalDefinitions[category] = firstDef;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // 3. 解析 translation_json (翻译信息)，准备填充 Senses / Definitions 等
            var translationJson = root["translation_json"] as JObject;
            if (translationJson != null)
            {
                var results = translationJson["results"] as JArray;
                if (results != null)
                {
                    foreach (var result in results)
                    {
                        var lexicalEntries = result["lexicalEntries"] as JArray;
                        if (lexicalEntries == null) continue;

                        foreach (var lexEntry in lexicalEntries)
                        {
                            string category = lexEntry["lexicalCategory"]?["text"]?.ToString() ?? "";

                            var entriesArray = lexEntry["entries"] as JArray;
                            if (entriesArray == null) continue;

                            foreach (var entry in entriesArray)
                            {
                                // 可能也会有 pronunciations
                                var prons = entry["pronunciations"] as JArray;
                                if (prons != null)
                                {
                                    // 有些情况下，translation_json 里也可能有更多发音信息
                                    foreach (var p in prons)
                                    {
                                        var dialArr = p["dialects"] as JArray;
                                        string dial = dialArr != null && dialArr.Count > 0 ? dialArr[0]?.ToString() : "";
                                        var audio = p["audioFile"]?.ToString() ?? "";
                                        var spelling = p["phoneticSpelling"]?.ToString() ?? "";

                                        // 如果想把翻译侧的发音也存起来，可以加进去
                                        model.Pronounciations.Add(new PronunciationModel
                                        {
                                            Dialect = dial,
                                            AudioFile = audio,
                                            PhoneticSpelling = spelling
                                        });
                                    }
                                }

                                // senses
                                var senses = entry["senses"] as JArray;
                                if (senses != null)
                                {
                                    foreach (var s in senses)
                                    {
                                        // translations
                                        // eg: "translations": [ { "language": "zh", "text": "使大为惊奇"} ]
                                        var transArr = s["translations"] as JArray;
                                        string firstTranslation = "";
                                        if (transArr != null && transArr.Count > 0)
                                        {
                                            var firstObj = transArr[0];
                                            if (firstObj != null)
                                            {
                                                firstTranslation = firstObj["text"]?.ToString() ?? "";
                                            }
                                        }

                                        // examples
                                        var examplesDict = new Dictionary<string, string>();
                                        var exArr = s["examples"] as JArray;
                                        if (exArr != null)
                                        {
                                            foreach (var ex in exArr)
                                            {
                                                var exText = ex["text"]?.ToString() ?? "";

                                                // 这里翻译在 ex["translations"] 里
                                                var exTranslations = ex["translations"] as JArray;
                                                string zhTrans = "";
                                                if (exTranslations != null && exTranslations.Count > 0)
                                                {
                                                    // 取第一个翻译
                                                    zhTrans = exTranslations[0]?["text"]?.ToString() ?? "";
                                                }

                                                if (!string.IsNullOrEmpty(exText))
                                                {
                                                    examplesDict[exText] = zhTrans;
                                                }
                                            }
                                        }

                                        var senseModel = new SenseModel
                                        {
                                            ExplanationLanguageCode = model.TargetLanguageCode,
                                            Category = category,
                                            Definition = firstTranslation,
                                            Examples = examplesDict
                                        };

                                        model.Senses.Add(senseModel);

                                        //construct definitions
                                        string definitionToAdd = string.Empty;
                                        if (!string.IsNullOrWhiteSpace(firstTranslation))
                                        {
                                            // 如果Senses中的释义不为空，则使用翻译中的释义
                                            definitionToAdd = firstTranslation;
                                        }
                                        else if (model.OriginalDefinitions.ContainsKey(category) && !string.IsNullOrWhiteSpace(model.OriginalDefinitions[category]))
                                        {
                                            // 如果Senses中的释义为空，则尝试使用 OriginalDefinitions 中的释义作为替代
                                            definitionToAdd = model.OriginalDefinitions[category];
                                        }

                                        if (!string.IsNullOrWhiteSpace(definitionToAdd))
                                        {
                                            if (!model.Definitions.ContainsKey(category))
                                            {
                                                // 如果该词性还未添加释义，则直接添加
                                                model.Definitions[category] = definitionToAdd;
                                            }
                                            else
                                            {
                                                // 如果该词性已有释义，则将新的释义追加在后面（注意避免重复添加）
                                                var existingDefs = model.Definitions[category]
                                                                    .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                                    .Select(x => x.Trim())
                                                                    .ToList();
                                                if (!existingDefs.Contains(definitionToAdd))
                                                {
                                                    model.Definitions[category] += ", " + definitionToAdd;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            // 对于 translation_json 中未出现但存在于 OriginalDefinitions 的词性，补充释义
            foreach (var kvp in model.OriginalDefinitions)
            {
                if (!model.Definitions.ContainsKey(kvp.Key) || string.IsNullOrWhiteSpace(model.Definitions[kvp.Key]))
                {
                    model.Definitions[kvp.Key] = kvp.Value;
                }
            }

            Utils.Utils.PrintModel(model,"echo: ");
            return model;
        }
    }
}
