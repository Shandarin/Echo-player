using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Echo.Services
{
    public class OnlineAudioPlayService
    {

        public static async Task PlayAudioAsync(string audioUrl)
        {
            // 如果URL无效，直接返回
            if (string.IsNullOrEmpty(audioUrl)) return;

            try
            {
                // 创建MediaPlayer对象（WPF命名空间：System.Windows.Media）
                var mediaPlayer = new MediaPlayer();

                // 打开指定URL（需要是可访问的URL，或者本地文件绝对路径）
                mediaPlayer.Open(new Uri(audioUrl, UriKind.RelativeOrAbsolute));

                // 播放
                mediaPlayer.Play();

                // 如果需要在播放完毕后做一些操作，可以订阅MediaEnded事件：
                mediaPlayer.MediaEnded += (s, e) =>
                {
                    // 播放完成后的处理逻辑
                    mediaPlayer.Close();
                };
            }
            catch (Exception ex)
            {
                // 处理异常，比如提示用户或记录日志
                Console.WriteLine($"播放音频失败: {ex.Message}");
            }
        }
    }


}
