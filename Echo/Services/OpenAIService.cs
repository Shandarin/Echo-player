using Echo.Mappers;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

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

        public async Task<string?> AnalyzeSubtitleAsync(string subtitle,string sourceLang, string targetLang)
        {

            var prompt = GenerateTranslationPrompt(sourceLang, targetLang, subtitle);

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
                if(response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    MessageBox.Show("API key wrong or expired");
                }
                return null;
            }

            var responseJson = JsonSerializer.Deserialize<JsonElement>(responseString);
            return responseJson.GetProperty("choices")[0]
                             .GetProperty("message")
                             .GetProperty("content")
                             .GetString();
        }


        private string GenerateTranslationPrompt(string sourceLangCode, string targetLangCode, string sentence)
        {
            // 获取语言的显示名称
            var sourceLanguage = LanguageMapper.GetLanguageDisplayName(sourceLangCode);
            var targetLanguage = LanguageMapper.GetLanguageDisplayName(targetLangCode);
            // 获取小写形式用于判断
            var sourceLangLower = string.IsNullOrEmpty(sourceLangCode) ? "" : sourceLangCode.ToLowerInvariant();
            var targetLangLower = string.IsNullOrEmpty(targetLangCode) ? "" : targetLangCode.ToLowerInvariant();
            string prompt = string.Empty;

            // 如果目标语言为英语且来源语言不为英语
            if (targetLangLower.StartsWith("en") && !sourceLangLower.StartsWith("en"))
            {
                // 为了让模板更贴合语言学习场景，将来源语言名称转换为英文形式
                string learnerSourceLanguage = sourceLanguage;
                if (sourceLangLower.StartsWith("zh"))
                    learnerSourceLanguage = "Chinese";
                else if (sourceLangLower.StartsWith("fr"))
                    learnerSourceLanguage = "French";
                else if (sourceLangLower.StartsWith("es"))
                    learnerSourceLanguage = "Spanish";

                if (sourceLangLower.StartsWith("fr"))
                {
                    // 针对法语的场景：源句为法语，翻译成英语并作英文分析
                    prompt = "For native English speakers learning French, provide the following sections separated by a line break:\n" +
                             "1. Translate the sentence from French into English.\n" +
                             "2. Analyze the grammar, phrases, and vocabulary used in the sentence in English.\n\n" +
                             "Strictly use the following format for the output:\n\n" +
                             "Example Input:\n" +
                             "\"Il faut prendre les choses telles qu'elles viennent.\"\n\n" +
                             "Example Output, strictly follow the format, and be concise, from French into English:\n\n" +
                             "\"I have to take things as they come.\"\n\n" +
                             " 1. \"Il faut\"\n" +
                             "  - Denotes necessity or obligation.\n" +
                             "  - Implies that something is required.\n" +
                             " 2. \"Prendre les choses\"\n" +
                             "  - Means to take or handle.\n" +
                             "  - Refers to the matters being discussed.\n" +
                             " 3. \"Telles qu'elles viennent\"\n" +
                             "  - Implies \"as they come,\" meaning to deal with things as they occur.\n\n" +
                             "The given sentence:\n{sentence}";
                }
                else if (sourceLangLower.StartsWith("es"))
                {
                    // 针对西班牙语的场景：源句为西班牙语，翻译成英语并作英文分析
                    prompt = "For native English speakers learning Spanish, provide the following sections separated by a line break:\n" +
                             "1. Translate the sentence from Spanish into English.\n" +
                             "2. Analyze the grammar, phrases, and vocabulary used in the sentence in English.\n\n" +
                             "Strictly use the following format for the output:\n\n" +
                             "Example Input:\n" +
                             "\"Tenemos que enfrentar este asunto de frente.\"\n\n" +
                             "Example Output, strictly follow the format, and be concise, from Spanish into English:\n\n" +
                             "\"I must face this issue head on.\"\n\n" +
                             " 1. \"Tenemos que\"\n" +
                             "  - Indicates obligation, meaning \"I have to.\"\n" +
                             " 2. \"Enfrentar este asunto\"\n" +
                             "  - Means to face or confront.\n" +
                             "  - Refers to the issue at hand.\n" +
                             " 3. \"De frente\"\n" +
                             "  - Means directly or head on.\n\n" +
                             "The given sentence:\n{sentence}";
                }
                else if (sourceLangLower.StartsWith("zh"))
                {
                    // 针对中文的场景：源句为中文，翻译成英语并作英文分析
                    prompt = "For native English speakers learning Chinese, provide the following sections separated by a line break:\n" +
                             "1. Translate the sentence from Chinese into English.\n" +
                             "2. Analyze the grammar, phrases, and vocabulary used in the sentence in English.\n\n" +
                             "Strictly use the following format for the output:\n\n" +
                             "Example Input:\n" +
                             "\"我必须正面应对这件事。\"\n\n" +
                             "Example Output, strictly follow the format, and be concise, from Chinese into English:\n\n" +
                             "\"I must tackle this matter directly.\"\n\n" +
                             " 1. \"我必须\"\n" +
                             "  - Expresses necessity, equivalent to \"I must.\"\n" +
                             " 2. \"正面应对\"\n" +
                             "  - Means to confront or handle directly.\n" +
                             " 3. \"这件事\"\n" +
                             "  - Refers to \"this matter.\" \n\n" +
                             "The given sentence:\n{sentence}";
                }
                else
                {
                    // 非法语、西班牙语、中文的其他语言：使用默认模板
                    prompt = "For native English speakers learning " + learnerSourceLanguage + ", provide the following sections separated by a line break:\n" +
                             "1. Translate the sentence from " + learnerSourceLanguage + " into English.\n" +
                             "2. Analyze the grammar, phrases, and vocabulary used in the sentence in English.\n\n" +
                             "Strictly use the following format for the output:\n\n" +
                             "Example Input:\n" +
                             "\"[Foreign sentence]\"\n\n" +
                             "Example Output, strictly follow the format, and be concise, from " + learnerSourceLanguage + " into English:\n\n" +
                             "\"[English translation]\"\n\n" +
                             " 1. \"[Phrase]\"\n" +
                             "  - Explanation of phrase meaning and usage in English.\n" +
                             " 2. \"[Phrase]\"\n" +
                             "  - Explanation...\n" +
                             " 3. \"[Phrase]\"\n" +
                             "  - Explanation...\n\n" +
                             "The given sentence:\n{sentence}";
                }
            }
            else
            {
                // 非“学习外语”场景，按照目标语言提供对应模板

                if (!string.IsNullOrEmpty(targetLangCode) && targetLangLower.StartsWith("zh"))
                {
                    // 中文模板
                    prompt = "针对给定的句子，提供下列各部分，并以换行符分隔：\n" +
                             "1. 将该句子从 {source_lang} 翻译到 {target_lang}。\n" +
                             "2. 分析句子中使用的语法、短语及词汇。\n\n" +
                             "请严格按照以下格式输出：\n\n" +
                             "示例输入：\n" +
                             "\"I gotta take this thing head on.\"\n\n" +
                             "示例输出，严格遵循格式且简明扼要，从 {source_lang} 翻译到 {target_lang}：\n\n" +
                             "我必须正面应对这件事。\n\n" +
                             " 1. \"I gotta\"\n" +
                             "  - \"Gotta\" 是 \"got to\" 的口语缩写，表示“必须”或“需要”。\n" +
                             "  - 在非正式语境中常见，语气随意。\n" +
                             " 2. \"Take this thing\"\n" +
                             "  - \"Take\" 在此表示“处理”或“应对”。\n" +
                             "  - \"This thing\" 指代上下文中提到的问题、挑战或任务。\n" +
                             " 3. \"Head on\"\n" +
                             "  - \"Head on\" 是短语，意为“正面地”或“直接地”。\n\n" +
                             "给定的句子：\n{sentence}";
                }
                else if (!string.IsNullOrEmpty(targetLangCode) && targetLangLower.StartsWith("es"))
                {
                    // 西班牙语模板
                    prompt = "Para la siguiente oración, proporcione las siguientes secciones separadas por un salto de línea:\n" +
                             "1. Traduce la oración de {source_lang} a {target_lang}.\n" +
                             "2. Analiza la gramática, las frases y el vocabulario utilizados en la oración.\n\n" +
                             "Utilice estrictamente el siguiente formato para la salida:\n\n" +
                             "Ejemplo de entrada:\n" +
                             "\"I gotta take this thing head on.\"\n\n" +
                             "Ejemplo de salida, siguiendo estrictamente el formato y siendo conciso, de {source_lang} a {target_lang}:\n\n" +
                             "Debo enfrentar este asunto de manera directa.\n\n" +
                             " 1. \"I gotta\"\n" +
                             "  - \"Gotta\" es la forma contraída de \"got to\", que indica obligación o necesidad.\n" +
                             "  - Se usa comúnmente en contextos informales con tono casual.\n" +
                             " 2. \"Take this thing\"\n" +
                             "  - \"Take\" aquí significa abordar o tratar.\n" +
                             "  - \"This thing\" se refiere al asunto en cuestión.\n" +
                             " 3. \"Head on\"\n" +
                             "  - \"Head on\" es una frase que significa directamente o sin rodeos.\n\n" +
                             "La oración dada:\n{sentence}";
                }
                else if (!string.IsNullOrEmpty(targetLangCode) && targetLangLower.StartsWith("fr"))
                {
                    // 法语模板
                    prompt = "Pour la phrase donnée, fournissez les sections suivantes séparées par un saut de ligne :\n" +
                             "1. Traduisez la phrase de {source_lang} en {target_lang}.\n" +
                             "2. Analysez la grammaire, les expressions et le vocabulaire utilisés dans la phrase.\n\n" +
                             "Utilisez strictement le format suivant pour la sortie :\n\n" +
                             "Exemple d'entrée :\n" +
                             "\"I gotta take this thing head on.\"\n\n" +
                             "Exemple de sortie, en respectant strictement le format et de manière concise, de {source_lang} en {target_lang} :\n\n" +
                             "Je dois aborder directement cette question.\n\n" +
                             " 1. \"I gotta\"\n" +
                             "  - \"Gotta\" est une contraction informelle de \"got to\", indiquant la nécessité ou l'obligation.\n" +
                             "  - Utilisée dans des contextes informels, avec un ton décontracté.\n" +
                             " 2. \"Take this thing\"\n" +
                             "  - \"Take\" signifie aborder ou traiter.\n" +
                             "  - \"This thing\" fait référence au problème ou à la tâche en question.\n" +
                             " 3. \"Head on\"\n" +
                             "  - \"Head on\" signifie directement ou sans détour.\n\n" +
                             "La phrase donnée :\n{sentence}";
                }
                else
                {
                    // 默认英文模板
                    prompt = "For the given sentence, provide the following sections separated by a line break:\n" +
                             "1. Translate the sentence from {source_lang} into {target_lang}.\n" +
                             "2. Analyze the grammar, phrases, and vocabulary used in the sentence.\n\n" +
                             "Strictly use the following format for the output:\n\n" +
                             "Example Input:\n" +
                             "\"I gotta take this thing head on.\"\n\n" +
                             "Example Output, strictly follow the format, and be concise, from {source_lang} into {target_lang}:\n\n" +
                             "\"I must tackle this issue head on.\"\n\n" +
                             " 1. \"I gotta\"\n" +
                             "  - \"Gotta\" is an informal contraction of \"got to\", indicating necessity or obligation.\n" +
                             "  - Commonly used in informal contexts with a casual tone.\n" +
                             " 2. \"Take this thing\"\n" +
                             "  - \"Take\" means to handle or address.\n" +
                             "  - \"This thing\" refers to a particular issue, challenge, or task mentioned in context.\n" +
                             " 3. \"Head on\"\n" +
                             "  - \"Head on\" means directly or straightforwardly.\n\n" +
                             "The given sentence:\n{sentence}";
                }
            }

            // 替换模板中的占位符为实际值
            prompt = prompt.Replace("{source_lang}", sourceLanguage)
                           .Replace("{target_lang}", targetLanguage)
                           .Replace("{sentence}", sentence);

            return prompt;
        }
    }
}