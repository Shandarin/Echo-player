using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Echo.Services
{
    public class OpenAIService
    {
        private readonly HttpClient _httpClient;

        private string _apiKey;
        private string _apiModel ;

        public OpenAIService()
        {
            var config = ConfigService.Instance.GetOpenAIConfig();
            _httpClient = new HttpClient();

            _apiKey = config.Key;
            _apiModel = config.Model;


            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://api.openai.com/v1/");
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        }

        public async Task<string> AnalyzeSubtitleAsync(string subtitle, string targetLang)
        {

            var prompt = $"For the given sentence, provide the following sections separated by ###:\r\n\t1. Translate the sentence into {targetLang}.\r\n\t2. Analyze the grammar, phrases, and vocabulary used in the sentence.\r\n\t3.  Explain the potential context and emotion conveyed by the sentence.\r\nStrictly use the following format for the output:\r\n\r\nExample Input:\r\n\"I gotta take this thing head on.\"\r\nExample Output:\r\n\r\n###我必须正面应对这件事。### **I gotta: -\"Gotta\" 是 \"got to\" 的口语缩写，表示“必须”或“需要”。-在非正式语境中很常见，语气随意。**Take this thing: -\"Take\" 在这里表示“处理”或“应对”。-\"This thing\" 指代上下文中提到的某个问题、挑战或任务。3. **Head on: -\"Head on\" 是一个短语，意思是“正面地”或“直接地”。-表示一种不回避、不逃避的态度，直接面对问题或挑战。###这句话通常用在描述面对困难或挑战时的决心，语气中带有一种积极和不退缩的态度。适合表达勇气和承担责任的场景。\n" +
                $"The given sentence\n{subtitle}";

            var requestBody = new
            {
                model = _apiModel,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                temperature = 0.7
            };

            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync("chat/completions", content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"OpenAI API error: {responseString}");
            }

            var responseJson = JsonSerializer.Deserialize<JsonElement>(responseString);
            return responseJson.GetProperty("choices")[0]
                             .GetProperty("message")
                             .GetProperty("content")
                             .GetString();
        }
    }
}