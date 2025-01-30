using System;
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
using Echo.Managers;
using SubtitlesParser.Classes;
using System.Reflection;
using LibVLCSharp.WPF;
using System.Collections.ObjectModel;

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
        //private readonly ScrollingSubtitleHandler _scrollingSubtitleHandler;

        private bool _hasAdjustedAspectRatio = false;
        private TextBlock _subtitleTextBlock;
        private Canvas _sentenceContainer;
        private SentencePanelView _sentencePanelView;
        
        //private TextBlock _prevSubtitleBlock;
        //private TextBlock _nextSubtitleBlock;

        //for double click detect
        private DateTime lastClickTime = DateTime.MinValue;
        private const double DOUBLE_CLICK_INTERVAL = 300; // 300ms for double click detection

        //obtain actual screen resolution
        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        private const int SM_CXSCREEN = 0; // 主屏幕宽度
        private const int SM_CYSCREEN = 1; // 主屏幕高度

        #region Events
        public event EventHandler<bool> FullscreenChanged;
        public event EventHandler<string> VideoFilePathChanged;
        public event EventHandler<bool> ToggleFullScreenRequested;
        
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
        private string _videoViewHeight = "300";

        [ObservableProperty]
        private string _videoViewWidth = "580";

        [ObservableProperty]
        private string _previousVideoViewHeight;

        [ObservableProperty]
        private string _previousVideoViewWidth;

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

        [ObservableProperty]
        private string _videoFilePath;

        [ObservableProperty]
        private string _yourLanguage;

        [ObservableProperty]
        private string _learningLanguage;

        [ObservableProperty]
        private uint _backwardTime ;

        [ObservableProperty]
        private uint _forwardTime;

        [ObservableProperty]
        private string _playImage;

        [ObservableProperty]
        private string _aspectRatio = "Default";

        [ObservableProperty]
        private ObservableCollection<string> _embeddedSubtitleFiles = new();


        public SubtitleItem CurrentSubtitleItem => _subtitleHandler?.CurrentSubtitleItem;
        #endregion

        private MessageManager _messageManager;

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

            //_mediaPlayer.AspectRatio = "1:1";

            VideoControlVM = new VideoControlViewModel(_mediaPlayer);
            MenuBarVM = new MenuBarViewModel();

            _yourLanguage = MenuBarVM.SelectedYourLanguage;
            _learningLanguage = MenuBarVM.SelectedLearningLanguage;

            // Subscribe to events
            MenuBarVM.OnScreenshotRequested += HandleScreenshotRequested;
            MenuBarVM.OnAspectRatioChanged += HandleAspectRatioChanged;
            MenuBarVM.OnFullScreenToggled += HandleOnFullScreenToggled;
            MenuBarVM.OnSubtitleFileSelected += HandleSubtitleFileSelected;
            MenuBarVM.MouseHoverEnabledChanged += HandleMouseHoverEnabledChanged;
            MenuBarVM.SubtitleVisibleChanged += HandleSubtitleVisibleChanged;
            MenuBarVM.OnChangeOpacity += HandleOnChangeOpacity;
            MenuBarVM.OnFontSizeChanged += HandleOnFontSizeChanged;
            MenuBarVM.YourLanguageChanged += HandleYourLanguageChanged;
            MenuBarVM.LearningLanguageChanged += HandleLearningLanguageChanged;
            MenuBarVM.BackwardTimeChanged += (s, e) => BackwardTime = e;
            MenuBarVM.ForwardTimeChanged += (s, e) => ForwardTime = e;
            MenuBarVM.OnSubtitleTrackSelected += HandleSubtitleTrackSelected;

            MediaPlayer.Playing += OnMediaPlaying;


            // Initialize services and handlers
            _translationService = new TranslationService();
            _wordClickHandler = new WordClickHandler( _translationService);
            _subtitleHandler = new SubtitleHandler(UpdateSubtitleText, _mediaPlayer,IsSubtitleVisible);
            //_scrollingSubtitleHandler = new ScrollingSubtitleHandler();

            _subtitleHandler.SubtitlesLoaded += subtitles =>
            {
                //_scrollingSubtitleHandler?.SetSubtitles(subtitles);
            };

            _mediaPlayer.Paused += (sender, e) =>
            {
                //PlayImage = "▶";
            };

            _mediaPlayer.Stopped += (sender, e) =>
            {
                VideoAreaContainerBackground = "Black";
            };

            _mediaPlayer.TimeChanged += (sender, e) =>
            {
                if (_mediaPlayer.IsPlaying)
                {
                    //_scrollingSubtitleHandler?.UpdateSubtitles(e.Time);
                }
            };
            LanguageManager.SetLanguage("zh-Hans");

            BackwardTime = MenuBarVM.BackwardTime;
            ForwardTime = MenuBarVM.ForwardTime;

        }

        #region Callbacks 

        //partial void OnIsFullScreenChanged(bool value)
        //{
        //    FullscreenChanged?.Invoke(this, value);
        //    //MediaPlayer.ToggleFullscreen();
        //}

        partial void OnIsSubtitleVisibleChanged(bool value)
        {
            if (!IsSubtitleVisible)
            {
                // 字幕不可见时
                if (_subtitleTextBlock != null)
                {
                    _subtitleTextBlock.Visibility = Visibility.Collapsed;

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
                    }
                }
            }
        }

        partial void OnSubtitleOpacityChanged(string op)
        {
            SetSubtitleBackground();
        }

        partial void OnVideoFilePathChanged(string value)
        {
            VideoFilePathChanged?.Invoke(this, value);
        }

        private void OnMediaPlaying(object? sender, EventArgs e)
        {
            

            VideoAreaContainerBackground = "Transparent";

            PlayImage = "⏸";

            if (!_hasAdjustedAspectRatio)
            {

                HandleAspectRatioChanged(sender, "Default");
                _hasAdjustedAspectRatio = true;
            }  
            _mediaPlayer.SetSpu(-1);//turn off embedded subtitle
        }

        #endregion

        #region Menubar

        private void HandleAspectRatioChanged(object sender, string ratio)
        {
            if (!MediaPlayer.IsPlaying) return;
            WindowSizeToContent = SizeToContent.WidthAndHeight;
            uint videoWidth = MediaPlayer.Media.Tracks.FirstOrDefault(t => t.TrackType == TrackType.Video).Data.Video.Width;
            uint videoHeight = MediaPlayer.Media.Tracks.FirstOrDefault(t => t.TrackType == TrackType.Video).Data.Video.Height;

            if (videoWidth == 0 || videoHeight == 0)
                return;

            (uint vvWidth, uint vvHeight, MainWindowLeft, MainWindowTop) =
                 _windowSizeHandler.CalculateWindowSize(MainWindowLeft, MainWindowTop, videoWidth, videoHeight, ratio);

            VideoViewWidth = vvWidth.ToString();
            VideoViewHeight = vvHeight.ToString();
            Debug.WriteLine($"vvWidth:{vvWidth} vvHeight:{vvHeight} MainWindowLeft:{MainWindowLeft} MainWindowTop:{MainWindowTop}");

            AspectRatio = ratio;

        }

        private void FullVideoView(object? sender, EventArgs e)
        {
            if (IsFullScreen)
            {
                uint videoWidth = MediaPlayer.Media.Tracks.FirstOrDefault(t => t.TrackType == TrackType.Video).Data.Video.Width;
                uint videoHeight = MediaPlayer.Media.Tracks.FirstOrDefault(t => t.TrackType == TrackType.Video).Data.Video.Height;
                if (videoWidth == 0 || videoHeight == 0)
                    return;

                (uint vvWidth, uint vvHeight) =
                _windowSizeHandler.CalculateFullWindowSize(videoWidth, videoHeight, AspectRatio);

                VideoViewWidth = vvWidth.ToString();
                VideoViewHeight = vvHeight.ToString();

            }
        }

        private void NormalVideoView(object? sender, EventArgs e)
        {
            if (!IsFullScreen)
            {
                uint videoWidth = MediaPlayer.Media.Tracks.FirstOrDefault(t => t.TrackType == TrackType.Video).Data.Video.Width;
                uint videoHeight = MediaPlayer.Media.Tracks.FirstOrDefault(t => t.TrackType == TrackType.Video).Data.Video.Height;
                if (videoWidth == 0 || videoHeight == 0)
                    return;

                (uint vvWidth, uint vvHeight) =
                _windowSizeHandler.CalculateNormalWindowSize(videoWidth, videoHeight, AspectRatio);

                VideoViewWidth = vvWidth.ToString();
                VideoViewHeight = vvHeight.ToString();
            }
        }

        private void HandleScreenshotRequested(object sender, EventArgs e)
        {
            var filePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Echo", "Data", "Snapshots");
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
            }
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
            }
            else
            {
                // When hover is enabled, initially hide subtitles
                if (_subtitleTextBlock != null)
                {
                    HideTextBlock();
                }
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

        private void HandleYourLanguageChanged(object? sender, string language)
        {
            _yourLanguage = language;
        }

        private void HandleLearningLanguageChanged(object? sender, string language)
        {
            _learningLanguage = language;
        }

        private void HandleSubtitleTrackSelected(object? sender, string track)
        {
            LoadSubtitle(track);
        }




        #endregion

        #region commands

        [RelayCommand]
        private void LoadSubtitle(string subtitlePath)
        {
            _subtitleHandler.LoadSubtitle(subtitlePath);
        }

        private void UpdateSubtitleText(string newText)
        {
            SubtitleText = newText;
            _wordClickHandler.SetText(newText);
        }

        [RelayCommand]
        public async Task OpenFileAsync()
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

                    //_isMouseLoaded = true;

                    VideoFilePath = filePath;

                    // step 1: check same dir for subtitles
                    _subtitleHandler.Dispose();
                    var srtPath = Path.ChangeExtension(filePath, ".srt");
                    var assPath = Path.ChangeExtension(filePath, ".ass");
                    if (File.Exists(srtPath))
                    {
                        //MessageBox.Show($"Found subtitle: {srtPath}");
                        LoadSubtitle(srtPath);
                    }
                    else if (File.Exists(assPath))
                    {
                        //MessageBox.Show($"Found subtitle: {assPath}");
                        LoadSubtitle(assPath);
                    }

                    //var EmbeddedSubtitleFiles = new List<string>();
                    //step 2: check extracted embedded subtitles

                    var subtitleFiles = SubtitleExtractHandler.FindEmbeddedSubtitleFiles(filePath);
                    EmbeddedSubtitleFiles = new ObservableCollection<string>(subtitleFiles);
                    //EmbeddedSubtitleFiles = SubtitleExtractHandler.FindEmbeddedSubtitleFiles(filePath);

                    //step 3: extract embedded subtitles(not always there)
                    if (!EmbeddedSubtitleFiles.Any() | EmbeddedSubtitleFiles is null)
                    {
                        subtitleFiles.Clear();

                        subtitleFiles = await SubtitleExtractHandler.ExtractEmbeddedSubtitlesAsync(filePath);
                        EmbeddedSubtitleFiles = new ObservableCollection<string>(subtitleFiles);
                    }

                    if (EmbeddedSubtitleFiles.Any())
                    {
                        foreach (var file in EmbeddedSubtitleFiles)
                        {
                            Debug.WriteLine($"file {file}");
                            if (file.Contains("OriginalLang") & !file.Contains("SDH"))
                            {
                                LoadSubtitle(file);
                                
                            }
                        }
                        if (!_subtitleHandler.IsLoaded)
                            LoadSubtitle(EmbeddedSubtitleFiles[0]);
                    }
                    MenuBarVM.UpdateSubtitle(EmbeddedSubtitleFiles, EmbeddedSubtitleFiles.Any());
                }
            }
        }

        [RelayCommand]
        public void OpenSubtitle()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Subtitle Files|*.srt;*.ass;"
            };
            if (dialog.ShowDialog() == true)
            {
                var filePath = dialog.FileName;
                var extension = Path.GetExtension(filePath).ToLower();
                Debug.WriteLine($"extension {extension}");
                if (extension != ".srt" & extension != ".ass")
                {
                    MessageBox.Show("This is not a subtitle file");
                }
                else
                {
                    LoadSubtitle(filePath);
                }
                
            }
        }


        [RelayCommand]
        private void HandleVideoAreaClick(Point clickPosition)
        {
            if (_translationService != null && !_translationService.IsClickInsidePanel(clickPosition))
            {
                _translationService.CloseTranslation();
                //_wordPanelView.Close();
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
            //_scrollingSubtitleHandler?.EnableScrolling(isEnabled);
        }

        [RelayCommand]
        private void WindowSizeChanged(SizeChangedEventArgs e)
        {
            Debug.WriteLine($"size {e}");
        }

        //public void OnWindowSizeChanged(Size newSize)
        //{
        //    if (_mediaPlayer.Media != null & !IsFullScreen & _hasAdjustedAspectRatio & _mediaPlayer.IsPlaying)
        //    {
        //        var result = _windowSizeHandler.CalculateResizedVideoSize(newSize.Width, newSize.Height, double.Parse(VideoViewWidth), double.Parse(VideoViewHeight));

        //        VideoViewWidth = result.Item1.ToString();
        //        VideoViewHeight = result.Item2.ToString();
        //    }
        //}

        public void OnSubtitleAreaMouseEnter()
        {
                if (IsSubtitleVisible)
                {
                    ShowTextBlock();
                    ShowSubtitle();
                }
        }

        public void OnSubtitleAreaMouseLeave()
        {
            if (IsMouseHoverEnabled)
            {
                HideTextBlock();
                HideSubtitle();
            }
        }

        public void OnSubtitleAreaMouseLeftButtonDown(System.Windows.Input.MouseEventArgs e)
        {

            //Debug.WriteLine("SubtitleAreaMouseLeftButtonDown");
            if (e.LeftButton == MouseButtonState.Pressed && subtitleText.Length > 0)
            {
                if(_translationService != null)
                {
                    _translationService.CloseTranslation();
                }
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
            //_scrollingSubtitleHandler.Initialize(mainBlock, prevBlock, nextBlock);
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
            ToggleFullScreenRequested?.Invoke(this, _isFullScreen);
            if (IsFullScreen)
            {
                FullVideoView(null, null);
                ///HideControlView();
                MenuBarVM.IsMenuBarVisible = false;
                //PreviousVideoViewHeight = VideoViewHeight;
                //PreviousVideoViewWidth = VideoViewWidth;
            }
            else
            {
                NormalVideoView(null, null);
                //ShowVideoControlView();
                MenuBarVM.IsMenuBarVisible = true;
                //VideoViewHeight = PreviousVideoViewHeight;
                //VideoViewWidth = PreviousVideoViewWidth;
                Debug.WriteLine($"VideoViewWidth {VideoViewWidth}");
            }
            
           // _mediaPlayer.ToggleFullscreen();

        }

        private void ShowSubtitle()
        {
            _subtitleHandler.Show();
        }

        private void HideSubtitle()
        {
            _subtitleHandler.Hide();
        }

        private void HideTextBlock()
        {
            if (_subtitleTextBlock != null)
            {
                _subtitleTextBlock.Background = new SolidColorBrush(Color.FromArgb(0x01, 0x00, 0x00, 0x00));
            }
        }

        private void ShowTextBlock()
        {
            if (_subtitleTextBlock != null)
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
        }

        #endregion

        public void OnPreviewKeyDown(KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    if (MainWindowState == WindowState.Maximized && MainWindowStyle == WindowStyle.None)
                    {
                        ToggleFullScreen();
                    }
                    break;

                case Key.Left:
                    // Skip backward
                    if (MediaPlayer?.Media != null)
                    {
                        long newTime = MediaPlayer.Time - BackwardTime*1000; // 10 seconds in milliseconds
                        if (newTime < 0) newTime = 0;
                        MediaPlayer.Time = newTime;
                    }
                    break;

                case Key.Right:
                    // Skip forward
                    if (MediaPlayer?.Media != null)
                    {
                        long newTime = MediaPlayer.Time + ForwardTime*1000;
                        if (newTime > MediaPlayer.Length) newTime = MediaPlayer.Length;
                        MediaPlayer.Time = newTime;
                        Debug.WriteLine($"MediaPlayer.Time {ForwardTime}");
                    }
                    break;

                case Key.Up:
                    // Increase volume 
                    if (MediaPlayer != null)
                    {
                        int newVolume = MediaPlayer.Volume + 5;
                        if (newVolume > 100) newVolume = 100;
                        MediaPlayer.Volume = newVolume;
                        VideoControlVM.Volume = newVolume;
                    }
                    break;

                case Key.Down:
                    // Decrease volume 
                    if (MediaPlayer != null)
                    {
                        int newVolume = MediaPlayer.Volume - 5;
                        if (newVolume < 0) newVolume = 0;
                        MediaPlayer.Volume = newVolume;
                        VideoControlVM.Volume = newVolume;
                    }
                    break;
                case Key.Space:
                    //payorpause
                    if (MediaPlayer != null)
                    {
                        ToggleMediaPlay();
                    }
                    break;
            }
  
        }

        public void ShowVideoControlView()
        {

            MenuBarVM.IsMenuBarVisible = true;
            VideoControlVM.IsControlBarVisible = true;
            
        }
        public void HideControlView()
        {
            VideoControlVM.IsControlBarVisible = false;
            MenuBarVM.IsMenuBarVisible = false;
        }

        public void Closed()
        {
            MenuBarVM?.SaveSettings();
            DisposeMedia();
        }
    }
}
