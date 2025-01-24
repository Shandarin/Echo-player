using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibVLCSharp.Shared;
using System.Windows.Threading;
using System;
using System.Diagnostics;
using System.Windows;

namespace Echo.ViewModels
{
    public partial class VideoControlViewModel : BaseViewModel
    {
        private MediaPlayer _mediaPlayer;
        private readonly DispatcherTimer _timer;

        [ObservableProperty]
        private double progress;

        [ObservableProperty]
        private int volume;

        [ObservableProperty]
        private bool isMuted;

        [ObservableProperty]
        private bool isPlaying;

        [ObservableProperty]
        private TimeSpan currentTime;

        [ObservableProperty]
        private TimeSpan totalTime;

        [ObservableProperty]
        private bool isControlBarVisible = true;

        [ObservableProperty]
        private string _playButtonImage = "▶";

        public VideoControlViewModel(MediaPlayer mediaPlayer)
        {
            _mediaPlayer = mediaPlayer;
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(200)
            };
            _timer.Tick += UpdateProgress;

            InitializeMediaPlayer();
        }


        private void Initialize()
        {
            Volume = 100;
            IsMuted = false;
            IsPlaying = false;
            Progress = 0;
            CurrentTime = TimeSpan.Zero;
            TotalTime = TimeSpan.Zero;
        }

        private void InitializeMediaPlayer()
        {
            //Debug.WriteLine("InitializeMediaPlayer");

            _mediaPlayer.Playing += (s, e) =>
            {
                PlayButtonImage = "▶";
                IsPlaying = true;
                UpdateTimeDisplay();
            };

            _mediaPlayer.Paused += (s, e) =>
            {
                PlayButtonImage = "⏸";
                IsPlaying = false;
            };

            _mediaPlayer.LengthChanged += (s, e) =>
            {
                UpdateTimeDisplay();
            };
        }

        [RelayCommand]
        private void PlayOrPause()
        {
            Debug.WriteLine("PlayOrPause");
            if (_mediaPlayer == null) return;

            if (_mediaPlayer.IsPlaying)
            {
                _mediaPlayer.Pause();
                IsPlaying = false;
            }
            else
            {
                _mediaPlayer.Play();
                IsPlaying = true;
            }
        }

        [RelayCommand]
        private void SkipForward()
        {
            //Debug.WriteLine("SkipForward");
            if (_mediaPlayer?.Media == null) return;
            long newTime = _mediaPlayer.Time + 10000;
            if (newTime > _mediaPlayer.Length) newTime = _mediaPlayer.Length;
            _mediaPlayer.Time = newTime;
        }

        [RelayCommand]
        private void SkipBackward()
        {
            if (_mediaPlayer?.Media == null) return;
            long newTime = _mediaPlayer.Time - 10000;
            if (newTime < 0) newTime = 0;
            _mediaPlayer.Time = newTime;
        }

        [RelayCommand]
        private void ToggleMute()
        {
            if (_mediaPlayer == null) return;
            IsMuted = !IsMuted;
            _mediaPlayer.Mute = IsMuted;
        }

        [RelayCommand]
        private void FullScreen()
        {
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow?.DataContext is MainWindowViewModel vm)
            {
                vm.ToggleFullScreen();
            }
        }

        private void UpdateProgress(object sender, EventArgs e)
        {
            if (_mediaPlayer?.Media == null) return;
            Progress = _mediaPlayer.Position * 100;
            UpdateTimeDisplay();
        }

        private void UpdateTimeDisplay()
        {
            if (_mediaPlayer?.Media == null) return;
            CurrentTime = TimeSpan.FromMilliseconds(_mediaPlayer.Time);
            TotalTime = TimeSpan.FromMilliseconds(_mediaPlayer.Length);
        }

        partial void OnVolumeChanged(int value)
        {
            if (_mediaPlayer != null)
            {
                _mediaPlayer.Volume = value;
                IsMuted = value == 0;
            }
        }

        partial void OnProgressChanged(double value)
        {
            if (_mediaPlayer?.Media != null)
            {
                _mediaPlayer.Position = (float)(value / 100);
                UpdateTimeDisplay();
            }
        }
    }
}
