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
            if (string.IsNullOrEmpty(audioUrl))
                return;

            try
            {
                var mediaPlayer = new MediaPlayer();

                // 设置音量（如果需要的话）
                mediaPlayer.Volume = 1.0;

                // 订阅 MediaOpened 事件，媒体加载完成后等待一段时间再播放
                mediaPlayer.MediaOpened += async (s, e) =>
                {
                    // 延时300毫秒，等待蓝牙耳机唤醒，以免开头播放不出声音
                    await Task.Delay(300);
                    mediaPlayer.Play();
                };

                // 打开指定URL
                mediaPlayer.Open(new Uri(audioUrl, UriKind.RelativeOrAbsolute));

                mediaPlayer.MediaEnded += (s, e) =>
                {
                    mediaPlayer.Close();
                };

                // 注意：确保 mediaPlayer 的引用在播放期间不会被GC回收
            }
            catch (Exception ex)
            {
                Console.WriteLine($"播放音频失败: {ex.Message}");
            }
        }
    }

    }
