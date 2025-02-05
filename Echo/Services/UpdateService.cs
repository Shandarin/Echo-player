using System.Threading.Tasks;
using AutoUpdaterDotNET;
using System.Windows;
using Echo.Resources;
using System.Diagnostics;

namespace Echo.Services
{

    public class UpdateService 
    {
        private readonly string _updateXmlUrl;

        public UpdateService(string updateXmlUrl)
        {
            _updateXmlUrl = updateXmlUrl;
        }

        public void CheckForUpdates()
        {
            // 配置 AutoUpdater.NET 参数（隐藏“跳过”和“稍后提醒”按钮，实现“一键更新”）
            AutoUpdater.ShowSkipButton = false;
            AutoUpdater.ShowRemindLaterButton = false;
            AutoUpdater.RunUpdateAsAdmin = false; // 可根据需要设为true
            AutoUpdater.Mandatory = false;        // 非强制更新时

            // 订阅更新检查事件
            AutoUpdater.CheckForUpdateEvent += AutoUpdater_CheckForUpdateEvent;

            // 开始检查更新（更新描述XML文件的URL，请确保地址正确并已配置好更新信息）
            AutoUpdater.Start(_updateXmlUrl);
           
        }

        private void AutoUpdater_CheckForUpdateEvent(UpdateInfoEventArgs args)
        {
            // 取消事件订阅，防止重复调用
            AutoUpdater.CheckForUpdateEvent -= AutoUpdater_CheckForUpdateEvent;
            Debug.WriteLine($"args.CurrentVersion {args.CurrentVersion}");
            Debug.WriteLine(args.InstalledVersion);

            if (args == null)
            {
                MessageBox.Show(LangResx.Failed_to_connect,
                    LangResx.Failed_to_update, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (args.IsUpdateAvailable)
            {
                // 构造更新提示消息（这里展示了当前版本、服务器版本以及更新日志链接）
                string message = $"{LangResx.New_version_found}：{args.CurrentVersion}\n" +
                                 $"{LangResx.Installed_version}：{args.InstalledVersion}\n" +
                                 $"{LangResx.Change_log}：{args.ChangelogURL}\n\n" +
                                 LangResx.Update_now;
                // 弹出确认对话框
                var result = MessageBox.Show(message, LangResx.Check_for_Updates, MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        // 调用下载更新方法，若下载成功，则提示重启程序
                        if (AutoUpdater.DownloadUpdate(args))
                        {
                            MessageBox.Show(LangResx.Download__completed,
                                LangResx.Update_completeed, MessageBoxButton.OK, MessageBoxImage.Information);
                            System.Windows.Application.Current.Shutdown();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"{LangResx.Error_during_updating}：{ex.Message}",
                            LangResx.Error_updating, MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                // 没有更新时可以提示用户，也可以不提示（这里选择提醒）
                MessageBox.Show(LangResx.You_are_already_on_the_latest_version,
                    LangResx.Check_for_Updates, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
