using Echo.Managers;
using Microsoft.Win32;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Threading; 
using System.Windows;
using System.Windows.Media.Animation;

namespace Echo
{
    public partial class App : Application
    {
        public App()
        {
            // 程序启动默认语言（简体中文）
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("zh-Hans");
            Thread.CurrentThread.CurrentCulture = new CultureInfo("zh-Hans");
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 获取或生成 API Key
            string apiKey = APIKeyManager.GetOrCreateApiKey();
        }

        public void ChangeLanguage(string cultureCode)
        {
            // 1. 切换线程文化
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(cultureCode);
            Thread.CurrentThread.CurrentCulture = new CultureInfo(cultureCode);

            // 2. 重新加载主窗口
            if (Application.Current.MainWindow is MainWindow oldWindow)
            {
                // 如果你需要保留一些状态，也可以先把状态取出来，然后再传给新的窗口
                oldWindow.Content = null; // 清空一下原窗口的内容

                // 用新的语言重新创建并赋给 Content
                var newWindow = new MainWindow();
                oldWindow.Content = newWindow.Content;
            }
        }

      
    }
}