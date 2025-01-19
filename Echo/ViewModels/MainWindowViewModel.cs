﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using Microsoft.Win32;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using LibVLCSharp.Shared;

using Echo.Handlers;
using Echo.Services;
using Echo.Views;

namespace Echo.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly LibVLC _libVLC;
        private readonly LibVLCSharp.Shared.MediaPlayer _mediaPlayer;
        private readonly SubtitleHandler _subtitleHandler;
        private readonly WordClickHandler _wordClickHandler;
        private readonly WindowSizeHandler _windowSizeHandler = new();
        private readonly TranslationService _translationService;
        private readonly ScrollingSubtitleHandler _scrollingSubtitleHandler;
        private bool _hasAdjustedAspectRatio = false;
        

        private TextBlock _subtitleTextBlock;
        private Canvas _sentenceContainer;
        private SentencePanelView _sentencePanelView;
        
        private TextBlock _prevSubtitleBlock;
        private TextBlock _nextSubtitleBlock;

        //for double click detect
        private DateTime lastClickTime = DateTime.MinValue;
        private const double DOUBLE_CLICK_INTERVAL = 300; // 300ms for double click detection

        //for obtain actual screen resolution
        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        private const int SM_CXSCREEN = 0; // 主屏幕宽度
        private const int SM_CYSCREEN = 1; // 主屏幕高度


        #region Events
        public event EventHandler<bool> FullscreenChanged;
        #endregion

        #region Observable Properties
        [ObservableProperty]
        private bool _isFullScreen = false;

        [ObservableProperty]
        private bool isHoverSubtitleEnabled = true;

        [ObservableProperty]
        private VideoControlViewModel videoControlVM;

        [ObservableProperty]
        private MenuBarViewModel menuBarVM;

        [ObservableProperty]
        private WindowStyle mainWindowStyle = WindowStyle.SingleBorderWindow;

        [ObservableProperty]
        private WindowState mainWindowState = WindowState.Normal;

        [ObservableProperty]
        private bool _isScrollingEnabled;

        [ObservableProperty]
        private string _videoAreaContainerBackground = "Black"; //避免启动时背景为其他颜色

        [ObservableProperty]
        private string _videoViewHeight;

        [ObservableProperty]
        private string _videoViewWidth;

        [ObservableProperty]
        private uint _mainWindowLeft;

        [ObservableProperty]
        private uint _mainWindowTop;

        [ObservableProperty]
        private SizeToContent _windowSizeToContent = SizeToContent.WidthAndHeight;

        [ObservableProperty]
        private bool _isMouseHoverEnabled = true;

        [ObservableProperty]
        private bool _isSubtitleVisible = true;

        [ObservableProperty]
        private string subtitleText;

        [ObservableProperty]
        private string _subtitleOpacity = "0.5";

        [ObservableProperty]
        private string _subtitleBackgroud;

        [ObservableProperty]
        private string _subtitleFontSize = "20";
        #endregion

        #region Properties

        public FrameworkElement VideoViewElement { get; set; }
        public FrameworkElement SubtitleTextElement { get; set; }

        public LibVLCSharp.Shared.MediaPlayer MediaPlayer => _mediaPlayer;

        #endregion

        public MainWindowViewModel( )
        {
            
            Core.Initialize();
            _libVLC = new LibVLC();
            _mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(_libVLC);
            _mediaPlayer.EnableHardwareDecoding = true;
            
            VideoControlVM = new VideoControlViewModel(_mediaPlayer);
            MenuBarVM = new MenuBarViewModel();

            // Subscribe to events
            MenuBarVM.OnScreenshotRequested += HandleScreenshotRequested;
            MenuBarVM.OnAspectRatioChanged += HandleAspectRatioChanged;
            MenuBarVM.OnFullScreenToggled += HandleOnFullScreenToggled;
            MenuBarVM.OnSubtitleFileSelected += HandleSubtitleFileSelected;
            MenuBarVM.MouseHoverEnabledChanged += HandleMouseHoverEnabledChanged;
            MenuBarVM.SubtitleVisibleChanged += HandleSubtitleVisibleChanged;
            MenuBarVM.OnChangeOpacity += HandleOnChangeOpacity;
            MenuBarVM.OnFontSizeChanged += HandleOnFontSizeChanged;
            FullscreenChanged += HandleFullscreenChanged;
            MediaPlayer.Playing += OnMediaPlaying;

            // Initialize services and handlers
            _translationService = new TranslationService();
            _wordClickHandler = new WordClickHandler( _translationService);
            _subtitleHandler = new SubtitleHandler(UpdateSubtitleText, _mediaPlayer);
            _scrollingSubtitleHandler = new ScrollingSubtitleHandler();

            _subtitleHandler.SubtitlesLoaded += subtitles =>
            {
                _scrollingSubtitleHandler?.SetSubtitles(subtitles);
            };

            //_mediaPlayer.Playing += (sender, e) =>
            //{
            //    _subtitleHandler?.Start();
            //};

            //_mediaPlayer.Paused += (sender, e) =>
            //{
            //    _subtitleHandler?.Stop();
            //};

            //_mediaPlayer.Stopped += (sender, e) =>
            //{
            //    _subtitleHandler?.Stop();
            //};

            _mediaPlayer.TimeChanged += (sender, e) =>
            {
                if (_mediaPlayer.IsPlaying)
                {
                    _subtitleHandler.UpdateTime(e.Time);
                    _scrollingSubtitleHandler?.UpdateSubtitles(e.Time);
                }

            };
            //string text = Resources.LangResx.File;
            //MessageBox.Show(text);
        }

        #region Callbacks 

        partial void OnIsFullScreenChanged(bool value)
        {
            FullscreenChanged?.Invoke(this, value);
        }

        partial void OnIsSubtitleVisibleChanged(bool value)
        {
            if (!IsSubtitleVisible)
            {
                // 字幕不可见时
                if (_subtitleTextBlock != null)
                {
                    _subtitleTextBlock.Visibility = Visibility.Collapsed;
                    _subtitleHandler?.Stop();
                }
            }
            else
            {
                // 字幕可见时
                if (_subtitleTextBlock != null)
                {
                    _subtitleTextBlock.Visibility = Visibility.Visible;
                    if (!IsMouseHoverEnabled)
                    {
                        _subtitleHandler?.Start();
                    }
                }
            }
        }

        partial void OnSubtitleOpacityChanged(string op)
        {
            Debug.WriteLine(SubtitleOpacity);
            SetSubtitleBackground();
        }

        #endregion


        #region Event Handlers

        private void OnMediaPlaying(object? sender, EventArgs e)
        {
            if (!_hasAdjustedAspectRatio)
            {
                HandleAspectRatioChanged(sender, "Default");
                _hasAdjustedAspectRatio = true;
            }
            
            _mediaPlayer.SetSpu(-1);//turn off embedded subtitle

        }

        private void HandleFullscreenChanged(object? sender, bool www)
        {
            Debug.WriteLine($"fuul{www}");
        }

        #endregion

        #region Menubar

        private void HandleAspectRatioChanged(object sender, string ratio)
        {
            if (!MediaPlayer.IsPlaying) return;
            uint videoWidth = MediaPlayer.Media.Tracks.FirstOrDefault(t => t.TrackType == TrackType.Video).Data.Video.Width;
            uint videoHeight = MediaPlayer.Media.Tracks.FirstOrDefault(t => t.TrackType == TrackType.Video).Data.Video.Height;

            if (videoWidth == 0 || videoHeight == 0)
                return;

            (uint vvWidth, uint vvHeight, MainWindowLeft, MainWindowTop) =
                 _windowSizeHandler.CalculateWindowSize(MainWindowLeft, MainWindowTop, videoWidth, videoHeight, ratio);

            VideoViewWidth = vvWidth.ToString();
            VideoViewHeight = vvHeight.ToString();

        }

        private void HandleScreenshotRequested(object sender, EventArgs e)
        {
            var filePath = Path.Combine(
                  AppDomain.CurrentDomain.BaseDirectory,
                  "Storage", "Snapshots");
            if (!Directory.Exists(filePath))
            {
                Directory.CreateDirectory(filePath);
            }
            _mediaPlayer.TakeSnapshot(0, filePath, 0, 0);
        }

        private void HandleOnFullScreenToggled(object? sender, EventArgs e)
        {
            ToggleFullScreen();
        }

        private void HandleSubtitleFileSelected(object? sender, string filepath)
        {
            if (_mediaPlayer.IsPlaying)
            {
                LoadSubtitle(filepath);
                _subtitleHandler.Start();
            }
            //Debug.WriteLine($"file {filepath}");
        }

        private void HandleMouseHoverEnabledChanged(object? sender, bool value)
        {
            IsMouseHoverEnabled = value;

            if (!value)
            {
                // When hover is disabled, always show subtitles
                if (_subtitleTextBlock != null)
                {
                    ShowTextBlock();
                }
                _subtitleHandler?.Start();
            }
            else
            {
                // When hover is enabled, initially hide subtitles
                if (_subtitleTextBlock != null)
                {
                    HideTextBlock();
                }
                _subtitleHandler?.Stop();
            }
        }

        private void HandleSubtitleVisibleChanged(object? sender, bool value)
        {
            IsSubtitleVisible = value;
        }

        private void HandleOnChangeOpacity(object? sender, string opacity)
        {
            SubtitleOpacity = opacity;
        }

        private void HandleOnFontSizeChanged(object? sender, string size)
        {
            SubtitleFontSize = size;
        }
        #endregion



        #region commands

        [RelayCommand]
        private void LoadSubtitle(string subtitlePath)
        {
            _subtitleHandler.LoadSubtitle(subtitlePath);
            _subtitleHandler.Start();
        }

        private void UpdateSubtitleText(string newText)
        {
            SubtitleText = newText;
            _wordClickHandler.SetText(newText);
        }

        [RelayCommand]
        public void OpenFile()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Video Files|*.mp4;*.mkv;*.avi;*.mov;*.wmv|Subtitle Files|*.srt;*.ass|All Files|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                var filePath = dialog.FileName;
                var extension = Path.GetExtension(filePath).ToLower();

                if (extension == ".srt" || extension == ".ass")
                {
                    LoadSubtitle(filePath);
                }
                else
                {
                    _hasAdjustedAspectRatio = false;
                    // 打开视频文件
                    _mediaPlayer.Media?.Dispose();
                    _mediaPlayer.Media = new Media(_libVLC, new Uri(filePath));
                    _mediaPlayer.Play();

                    // 自动检查同名字幕
                    _subtitleHandler.Dispose();
                    var srtPath = Path.ChangeExtension(filePath, ".srt");
                    var assPath = Path.ChangeExtension(filePath, ".ass");
                    if (File.Exists(srtPath))
                    {
                        //MessageBox.Show($"Found subtitle: {srtPath}");
                        LoadSubtitle(srtPath);
                        _subtitleHandler.Start();
                    }
                    else if (File.Exists(assPath))
                    {
                        //MessageBox.Show($"Found subtitle: {assPath}");
                        LoadSubtitle(assPath);
                        _subtitleHandler.Start();
                    }
                }
                VideoAreaContainerBackground = "Transparent";
            }
        }

        [RelayCommand]
        private void OpenSubtitle()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Subtitle Files|*.srt;*.ass;*.ssa|All Files|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                var filePath = dialog.FileName;
                var extension = Path.GetExtension(filePath).ToLower();
                //MessageBox.Show($"Loaded subtitle: {dialog.FileName}");
                LoadSubtitle(filePath);
            }
        }


        [RelayCommand]
        private void HandleVideoAreaClick(Point clickPosition)
        {
            if (_translationService != null && !_translationService.IsClickInsidePanel(clickPosition))
            {
                _translationService.CloseTranslation();
            }

            if(_sentencePanelView !=null && !_sentencePanelView.IsClickInsidePanel(clickPosition))
            {
                _sentencePanelView.Close();
            }

            if (DateTime.Now.Subtract(lastClickTime).TotalMilliseconds <= DOUBLE_CLICK_INTERVAL)
            {
                ToggleFullScreen();
            }
            else
            {
                ToggleMediaPlay();
            }

            lastClickTime = DateTime.Now;
        }

        [RelayCommand]
        private void ToggleScrollingSubtitles(bool isEnabled)
        {
            IsScrollingEnabled = isEnabled;
            _scrollingSubtitleHandler?.EnableScrolling(isEnabled);
        }

        #endregion

        #region Public Methods 

        public void OnSubtitleAreaMouseEnter()
        {
            if (IsMouseHoverEnabled)
            {
                ShowTextBlock();
                _subtitleHandler?.Start();
            }
        }

        public void OnSubtitleAreaMouseLeave()
        {
            if (IsMouseHoverEnabled)
            {
                HideTextBlock();
                _subtitleHandler?.Stop();
            }
        }
        public void OnSubtitleAreaMouseLeftButtonDown(System.Windows.Input.MouseEventArgs e)
        {

            //Debug.WriteLine("SubtitleAreaMouseLeftButtonDown");
            if (e.LeftButton == MouseButtonState.Pressed && subtitleText.Length > 0)
            {
                if (_sentencePanelView != null)
                {
                    _sentencePanelView.Close();
                    _sentenceContainer.Children.Remove(_sentencePanelView);
                }

                _sentencePanelView = new SentencePanelView();
                _sentencePanelView.CloseRequested += (s, args) =>
                {
                    _sentenceContainer.Children.Remove(_sentencePanelView);
                    _sentencePanelView = null;
                };
                _sentenceContainer.Children.Add(_sentencePanelView);
                _sentencePanelView.Show(subtitleText, e.GetPosition(_sentenceContainer));
            }
        }



        public void SetSubtitleBlocks(TextBlock mainBlock, TextBlock prevBlock, TextBlock nextBlock)
        {
            if (mainBlock == null)
                throw new ArgumentNullException(nameof(mainBlock));

            _subtitleTextBlock = mainBlock;
            _wordClickHandler.SetTextBlock(mainBlock);
            _scrollingSubtitleHandler.Initialize(mainBlock, prevBlock, nextBlock);
        }


        public void SetTranslationContainer(Canvas translationContainer)
        {
            if (_translationService != null && translationContainer != null)
            {

                _translationService.SetContainer(translationContainer);
            }
        }

        //SetSentenceContainer
        public void SetSentenceContainer(Canvas sentenceContainer)
        {
            if (sentenceContainer != null)
            {
                _sentenceContainer = sentenceContainer;
            }
        }

        public void SetSubtitleTextBlock(TextBlock textBlock)
        {
            _subtitleTextBlock = textBlock;
            _wordClickHandler.SetTextBlock(textBlock);
        }

        private void ToggleMediaPlay()
        {
            //Debug.WriteLine();
            if (_mediaPlayer.IsPlaying)
            {
                _mediaPlayer.Pause();
            }
            else
            {
                _mediaPlayer.Play();
            }
        }

        public void DisposeMedia()
        {
            _mediaPlayer?.Dispose();
            _libVLC?.Dispose();
        }

        #endregion

        #region  Private Helpers

        public void ToggleFullScreen()
        {
            _isFullScreen = !_isFullScreen;

            if (_isFullScreen)
            {
                WindowSizeToContent = SizeToContent.Manual;
                MainWindowStyle = WindowStyle.None;
                MainWindowState = WindowState.Maximized;
                //_mediaPlayer.ToggleFullscreen();
            }
            else
            {
                WindowSizeToContent = SizeToContent.WidthAndHeight;
                MainWindowStyle = WindowStyle.SingleBorderWindow;
                MainWindowState = WindowState.Normal;
                //_mediaPlayer.ToggleFullscreen();
            }

            VideoViewHeight = "Auto";
            VideoViewWidth = "Auto";


            VideoControlVM.IsControlBarVisible = !_isFullScreen;
            MenuBarVM.IsMenuBarVisible = VideoControlVM.IsControlBarVisible;

            OnPropertyChanged(nameof(IsFullScreen));
        }


        private void HideTextBlock()
        {
            if (_subtitleTextBlock != null)
            {
                if (!IsSubtitleVisible)
                {
                    _subtitleTextBlock.Visibility = Visibility.Collapsed;
                }
                else
                {
                    _subtitleTextBlock.Background = new SolidColorBrush(Color.FromArgb(0x01, 0x00, 0x00, 0x00));
                }
            }
        }

        private void ShowTextBlock()
        {
            if (_subtitleTextBlock != null && IsSubtitleVisible)
            {
                _subtitleTextBlock.Visibility = Visibility.Visible;
                SetSubtitleBackground();

            }
        }
        private void SetSubtitleBackground()
        {
            string argbValue = "#80000000"; 
            switch (SubtitleOpacity)
            {
                case "0":
                    argbValue = "#00000000";
                    break;
                case "0.25":
                    argbValue = "#40000000";
                    break;
                case "0.5":
                    argbValue = "#80000000"; ;
                    break;
                case "0.75":
                    argbValue = "#C0000000";
                    break;
                case "1":
                    argbValue = "#FF000000";
                    break;
            }
            SubtitleBackgroud = argbValue;
            //Debug.WriteLine(argbValue);
        }



        #endregion
    }
}
