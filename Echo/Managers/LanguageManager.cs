using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;

namespace Echo.Managers
{
    public static class LanguageManager
    {
        private static ResourceManager _resourceManager =
            new ResourceManager("Echo.Resources.LangResx", typeof(LanguageManager).Assembly);

        /// <summary>
        /// 获取本地化字符串
        /// </summary>
        public static string GetString(string key)
        {
            return _resourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? key;
        }

        /// <summary>
        /// 切换语言
        /// </summary>
        public static void SetLanguage(string cultureCode)
        {
            CultureInfo.CurrentUICulture = new CultureInfo(cultureCode);
        }
    }
}
