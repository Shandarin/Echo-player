using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibVLCSharp.Shared;
using System.Windows.Threading;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;

namespace Echo.ViewModels
{
    public partial class VideoControlViewModel : BaseViewModel
    {
        private LibVLCSharp.Shared.MediaPlayer _mediaPlayer;
        private readonly DispatcherTimer _timer;

        [ObservableProperty]
        private double progress = 0;

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

        [ObservableProperty]
        private string _volumeImage = "🔊";
        

        public VideoControlViewModel(LibVLCSharp.Shared.MediaPlayer mediaPlayer)
        {
            _mediaPlayer = mediaPlayer;

            _mediaPlayer.TimeChanged += async (s, e) =>
            {
                UpdateTimeDisplay();
                UpdateProgress(s,e);

            };

            InitializeMediaPlayer();
            Initialize();
        }


        private void Initialize()
        {
            Volume = 80;
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
                PlayButtonImage = "⏸";
                IsPlaying = true;
                TotalTime = TimeSpan.FromMilliseconds(_mediaPlayer.Length);
            };

            _mediaPlayer.Paused += (s, e) =>
            {
                PlayButtonImage = "▶";
                IsPlaying = false;
            };

            _mediaPlayer.Stopped += (s, e) =>
            {
                PlayButtonImage = "▶";
                IsPlaying = false;
                _mediaPlayer.Time = 0;
            };

            _mediaPlayer.LengthChanged += (s, e) =>
            {
                //UpdateTimeDisplay();
            };
        }

        [RelayCommand]
        private void PlayOrPause()
        {
            //Debug.WriteLine("PlayOrPause");
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

        private async Task UpdateProgress(object sender, EventArgs e)
        {
            if (_mediaPlayer?.Media == null) return;
            Progress = _mediaPlayer.Position * 100;
            //Debug.WriteLine(Progress);
            //UpdateTimeDisplay();
        }

        private void UpdateTimeDisplay()
        {
            if (_mediaPlayer?.Media == null) return;
            CurrentTime = TimeSpan.FromMilliseconds(_mediaPlayer.Time);
            
        }

        private void ChangeVolumeImage()
        {
            if(Volume == 0)
            {
                VolumeImage = "🔇";
            }
            else if (Volume < 20 )
            {
                VolumeImage = "🔈";
            }
            else if(Volume< 60)
            {
                VolumeImage = "🔉";
            }
            else
            {
                VolumeImage = "🔊";
            }
        }

        partial void OnVolumeChanged(int value)
        {
            if(value > 100 | value <0) return;
            if (_mediaPlayer != null)
            {
                _mediaPlayer.Volume = value;
                IsMuted = value == 0;
                Volume = value;
            }
            ChangeVolumeImage();
        }

        partial void OnProgressChanged(double value)
        {
            if (_mediaPlayer?.Media != null)
            {
                float newPos = (float)(value / 100);
                // 若差距非常小，不必重新写回
                if (Math.Abs(_mediaPlayer.Position - newPos) > 0.001f)
                {
                    _mediaPlayer.Position = newPos;
                }
                //UpdateTimeDisplay();
            }
        }

        partial void OnIsMutedChanged(bool value)
        {
            if (IsMuted)
            {
                VolumeImage = "🔇";
            }
            else
            {
                ChangeVolumeImage();
            }
        }
    }
}
