using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Echo.Managers
{
    public class APIKeyManager
    {
        private static readonly string RegistryPath = @"SOFTWARE\Echo\EchoPlayer";
        private static readonly string KeyName = "ApiKey";

        public static string GetOrCreateApiKey()

        {
            // 1. 打开注册表项
            using (var baseKey = Registry.CurrentUser.CreateSubKey(RegistryPath, true))
            {

                // 2. 读取键
                var existingKey = baseKey.GetValue(KeyName) as string;
                if (!string.IsNullOrEmpty(existingKey))
                {
                    return existingKey;
                }
                else
                {
                    // 生成新的 GUID
                    var newKey = Guid.NewGuid().ToString("D");
                    // 写入注册表
                    baseKey.SetValue(KeyName, newKey, RegistryValueKind.String);
                    return newKey;
                }
            }
        }
    }
}
