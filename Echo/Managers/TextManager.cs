using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Echo.Managers
{
    public static class TextManager
    {
        public static string RemoveHtmlTags(string input)
        {
            // 匹配 HTML 标签的正则表达式
            //匹配所有形如 <tag> 的 HTML 标签，包括闭合标签，如 </tag>。
            string pattern = "<[^>]+>";
            return Regex.Replace(input, pattern, string.Empty);
        }

        // 将一个多行字符串转换成行集合
        public static List<string> SplitRows(string content)
        {
            content = content.Replace("\t", " ");

            var ContentLines = new List<string>();
            var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            foreach (var line in lines)
            {
                ContentLines.Add(line);
            }
            return ContentLines;
        }

    }
}
