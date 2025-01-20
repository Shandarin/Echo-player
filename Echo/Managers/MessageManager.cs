using Echo.ViewModels;
using LibVLCSharp.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Echo.Managers
{
    internal class MessageManager
    {
        MainWindowViewModel MainWindowVM = Application.Current.MainWindow.DataContext as MainWindowViewModel;
        private MediaPlayer _mediaPlayer;

        public MessageManager()
        {
            _mediaPlayer = MainWindowVM.MediaPlayer;

            _mediaPlayer.SetMarqueeInt(VideoMarqueeOption.Timeout, 3);
            _mediaPlayer.SetMarqueeInt(VideoMarqueeOption.Size, 10);
            _mediaPlayer.SetMarqueeInt(VideoMarqueeOption.Position, 4);
            _mediaPlayer.SetMarqueeInt(VideoMarqueeOption.Color, (int)2);

        }

        public void ShowMessage(string message)
        {
            _mediaPlayer.SetMarqueeString(VideoMarqueeOption.Text, message);
            _mediaPlayer.SetMarqueeInt(VideoMarqueeOption.Enable, 1);
        }
    }
}
