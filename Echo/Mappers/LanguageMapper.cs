using Echo.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Echo.Mappers
{
    public static class LanguageMapper
    {
        private static readonly Dictionary<string, string> _languageMap = new Dictionary<string, string>
        {
            { "en", "English" },
            { "zh", "中文" },
            { "zh-hans", "中文" },
            { "zh-hant", "繁體中文" },
            { "ja", "日本語" },
            { "ja-jp", "日本語" },
            { "ko", "한국어" },
            { "ko-kr", "한국어" },
            { "fr", "Français" },
            { "de", "Deutsch" },
            { "de-de", "Deutsch" },
            { "es", "Español" },
            { "ar", "العربية" },
            { "ru", "Русский" },
            { "pt", "Português" },
            { "it", "Italiano" },
            { "it-it", "Italiano" },
        };

        public static string GetLanguageDisplayName(string code)
        {
            if (string.IsNullOrEmpty(code))
                return string.Empty;

            code = code.ToLowerInvariant();
            if (_languageMap.TryGetValue(code, out string displayName))
            {
                return displayName;
            }
            return code;
        }
    }
}
