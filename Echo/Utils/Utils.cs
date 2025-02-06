using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Echo.Utils
{
    public class Utils
    {
        public static void PrintModel<T>(T model, string? message = null)
        {
            string json = JsonSerializer.Serialize(model, new JsonSerializerOptions
            {
                WriteIndented = true   // 缩进美化输出
            });

            Debug.WriteLine($"{message} {json}");
        }
    }
}
