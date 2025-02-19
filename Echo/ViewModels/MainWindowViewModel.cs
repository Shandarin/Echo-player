using System.Diagnostics;
using System.IO;
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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Echo.Models;
using LanguageDetection.Models;

namespace Echo.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        #region 字段
        private readonly LibVLC _libVLC;
        private readonly LibVLCSharp.Shared.MediaPlayer _mediaPlayer;
        private readonly SubtitleHandler _subtitleHandler;
        private readonly WordClickHandler _wordClickHandler;
        private readonly WordClickHandler _previousWordClickHandler;
        private readonly WindowSizeHandler _windowSizeHandler = new();
        private readonly TranslationService _translationService;

        private bool _hasAdjustedAspectRatio = false;
        private bool _isSentenceAnalysisEnabled;
        private bool _isVideoLoading = false;
        private bool _isChangingFullScreen = false;
        private bool _isStopped = false;
        private bool _isWordQueryEnabled;
        private long _sizeChangingTimer;
        private long _clearSubtitleTimestamp = 0;

        private TextBlock _subtitleTextBlock;
        private Canvas _sentenceContainer;
        private SentencePanelView _sentencePanelView;
        private TextBlock _prevSubtitleBlock;
        private DateTime _lastClickTime = DateTime.MinValue; //for double click detect
        private const double DOUBLE_CLICK_INTERVAL = 300; // 单位：毫秒

        //obtain actual screen resolution
        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);
        private const int SM_CXSCREEN = 0; // 主屏幕宽度
        private const int SM_CYSCREEN = 1; // 主屏幕高度

        private MessageManager _messageManager;
        #endregion

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
        private string previousSubtitleText;

        [ObservableProperty]
        private string _subtitleOpacity = "0.5";

        [ObservableProperty]
        private string _subtitleBackgroud;

        [ObservableProperty]
        private double _subtitleFontSize = 20;

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

        [ObservableProperty]
        private string _subtitleDisplayMode;

        [ObservableProperty]
        private bool _isBackwardBySentence;

        [ObservableProperty]
        private bool _isForwardBySentence;

        [ObservableProperty]
        private ObservableCollection<AudioTrackInfo> _audioTracks = new();

        [ObservableProperty]
        private AudioTrackInfo _selectedAudioTrack;


        public SubtitleItem CurrentSubtitleItem => _subtitleHandler?.CurrentSubtitleItem;
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
            _mediaPlayer.EnableHardwareDecoding = false;
            _mediaPlayer.FileCaching = 500;//太大会造成暂停后声音短暂消失

            //_mediaPlayer.AspectRatio = "1:1";

            VideoControlVM = new VideoControlViewModel(_mediaPlayer);
            MenuBarVM = new MenuBarViewModel();

            YourLanguage = Properties.Settings.Default.YourLanguage;
            //_learningLanguage = Properties.Settings.Default.LearningLanguage;
            _isSentenceAnalysisEnabled = Properties.Settings.Default.IsSentenceAnalysisEnabled;
            _isWordQueryEnabled = Properties.Settings.Default.IsWordQueryEnabled;

            // Subscribe to events
            MenuBarVM.OnScreenshotRequested += HandleScreenshotRequested;
            MenuBarVM.OnAspectRatioChanged += HandleAspectRatioChanged;
            MenuBarVM.OnFullScreenToggled += HandleOnFullScreenToggled;
            MenuBarVM.OnSubtitleFileSelected += HandleSubtitleFileSelected;
            MenuBarVM.MouseHoverEnabledChanged += HandleMouseHoverEnabledChanged;
            MenuBarVM.SubtitleVisibleChanged += HandleSubtitleVisibleChanged;
            MenuBarVM.OnChangeOpacity += HandleOnChangeOpacity;
            MenuBarVM.OnFontSizeChanged += HandleOnFontSizeChanged;
            //MenuBarVM.YourLanguageChanged += HandleYourLanguageChanged;
            //MenuBarVM.LearningLanguageChanged += HandleLearningLanguageChanged;
            //MenuBarVM.BackwardTimeChanged += (s, e) => BackwardTime = e;
            //MenuBarVM.ForwardTimeChanged += (s, e) => ForwardTime = e;
            MenuBarVM.OnSubtitleTrackSelected += HandleSubtitleTrackSelected;
            MenuBarVM.SubtitleDisplayModeChangedEvent += HandleSubtitleDisplayModeChanged;
            // MenuBarVM.IsSentenceAnalysisEnabledChanged += HandleIsSentenceAnalysisEnabledChanged;
            //MenuBarVM.IsWordQueryEnabledChanged += HandleIsWordQueryEnabledChanged;
            MenuBarVM.OnLearningLanguageChanged += HandleLearningLanguageChanged;
            MenuBarVM.OnAudioTrackChanged += HandleAudioTrackChanged;
          

            IsBackwardBySentence = true;
            IsForwardBySentence = true;

            MediaPlayer.Playing += OnMediaPlaying;

            // Initialize services and handlers
            _translationService = new TranslationService();
            _wordClickHandler = new WordClickHandler( _translationService);
            _previousWordClickHandler = new WordClickHandler(_translationService);
            _subtitleHandler = new SubtitleHandler(UpdateSubtitleText, _mediaPlayer,IsSubtitleVisible);
            //_scrollingSubtitleHandler = new ScrollingSubtitleHandler();

            //_subtitleHandler.SubtitlesLoaded += (s,e) =>
            //{
            //    Debug.WriteLine($"SubtitlesLoaded ");
            //    MenuBarVM.DetectedLanguage = _subtitleHandler.Language;
            //    _learningLanguage = _subtitleHandler.Language;
            //    MessageBox.Show("ok");
            //};//无效

            _mediaPlayer.Paused += (sender, e) =>
            {
                //PlayImage = "▶";
            };

            _mediaPlayer.Stopped += (sender, e) =>
            {
                VideoAreaContainerBackground = "Black";
                _isStopped = true;
            };

            _mediaPlayer.TimeChanged += (sender, e) =>
            {
                //Debug.WriteLine($"_mediaPlayer.Time {_mediaPlayer.Time}");
                //Debug.WriteLine($"_changPlayer.Time {_clearSubtitleTimestamp}");
                if (_mediaPlayer.IsPlaying)
                {
                    //_scrollingSubtitleHandler?.UpdateSubtitles(e.Time);
                }
                if (_clearSubtitleTimestamp > 0 && _mediaPlayer.Time >= _clearSubtitleTimestamp)
                {
                    PreviousSubtitleText = string.Empty;
                    _clearSubtitleTimestamp = 0;
                    _previousWordClickHandler.SetText(PreviousSubtitleText);
                }
            };
            //LanguageManager.SetLanguage("zh-Hans");

            BackwardTime = MenuBarVM.BackwardTime;
            ForwardTime = MenuBarVM.ForwardTime;

            SubtitleDisplayMode = MenuBarVM.SubtitleDisplayMode;
            _isSentenceAnalysisEnabled = MenuBarVM.IsSentenceAnalysisEnabled;
            _wordClickHandler.OnWordClickEvent += HandleWordClickEvent;
            _previousWordClickHandler.OnWordClickEvent += HandleWordClickEvent;

            Properties.Settings.Default.PropertyChanged += OnPropertyChanged;


        }


        #region Callbacks 

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
            _isVideoLoading = false;
            _sizeChangingTimer = _mediaPlayer.Time;
            if (_isStopped)
            {
                _isStopped = false;
            }

            // 更新音轨列表
            UpdateAudioTracks();
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
            //Debug.WriteLine($"vvWidth:{vvWidth} vvHeight:{vvHeight} MainWindowLeft:{MainWindowLeft} MainWindowTop:{MainWindowTop}");

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
                    HideTextBlocks();
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

        private void HandleOnFontSizeChanged(object? sender, double size)
        {
            SubtitleFontSize = size;
        }


        private void HandleLearningLanguageChanged(object? sender, string language)
        {
            if(language == "Auto")
            {
                _learningLanguage = _subtitleHandler.Language;
            } 
            _learningLanguage = language;
            Debug.WriteLine($"_learningLanguage {_learningLanguage}");
        }

        private void HandleSubtitleTrackSelected(object? sender, string track)
        {
            LoadSubtitle(track);
        }
        

        private void HandleSubtitleDisplayModeChanged(object? sender, string mode)
        {
            SubtitleDisplayMode = mode;

            if (SubtitleDisplayMode == "Always" || SubtitleDisplayMode == "Hover")
            {
                SetSubtitleBackground();
                ShowSubtitle();
            }
            else if (SubtitleDisplayMode == "Hover")
            {
                SetSubtitleBackground();
                HideTextBlocks();
            }
            else if ( SubtitleDisplayMode == "Hide")
            {
                HideSubtitle();
            }
        }

        private void HandleIsSentenceAnalysisEnabledChanged(object? sender, bool value)
        {
            _isSentenceAnalysisEnabled = value;
        }

        private void HandleIsWordQueryEnabledChanged(object? sender, bool value)
        {
            _isWordQueryEnabled = value;
        }


        #endregion

        #region commands

        [RelayCommand]
        private void LoadSubtitle(string subtitlePath)
        {
            _subtitleHandler.LoadSubtitle(subtitlePath);
            if (SubtitleDisplayMode == "Always")
            {
                SetSubtitleBackground();
                ShowSubtitle();
            }
            else if(SubtitleDisplayMode == "Hover")
            {
                SetSubtitleBackground();
                HideTextBlocks();
            }
            else if (SubtitleDisplayMode == "Hide")
            {
                HideSubtitle();
            }
            MenuBarVM.DetectedLanguage = _subtitleHandler.Language;
            _learningLanguage = _subtitleHandler.Language;
        }


        // 修改后的 UpdateSubtitleText 方法
        private async void UpdateSubtitleText(string newText, string prevText)
        {
            
            if (SubtitleDisplayMode == "Hide")
            {
                // Hide 模式下：清空字幕并隐藏所有 textblock
                SubtitleText = string.Empty;
                PreviousSubtitleText = string.Empty;
                HideTextBlocks();
                _wordClickHandler.SetText(string.Empty);
                _previousWordClickHandler.SetText(string.Empty);
            }
            else if (SubtitleDisplayMode == "Hover")
            {
                // Hover 模式下：仅更新字幕文本（显示由鼠标进入事件控制）
                SubtitleText = newText;
                PreviousSubtitleText = prevText;
                _wordClickHandler.SetText(newText);
                _previousWordClickHandler.SetText(prevText);
;
            }
            else if (SubtitleDisplayMode == "Always")
            {
                // Always 模式下分别根据内容显示或隐藏对应的 textblock
                SubtitleText = newText;
                if (!string.IsNullOrEmpty(newText))
                {
                    ShowTextBlock(true); // 显示主字幕 textblock
                }
                else
                {
                    if (string.IsNullOrEmpty(prevText))//如果主字幕和上一句字幕都为空，则隐藏主字幕textblock，防止上一句字幕显示在主字幕textblock上
                    {
                        HideMainTextBlock();
                    }
                }

                PreviousSubtitleText = prevText;
                if (!string.IsNullOrEmpty(prevText))
                {
                    ShowTextBlock(false); // 显示上一句字幕 textblock
                }
                else
                {
                    HidePrevTextBlock();
                }

                _wordClickHandler.SetText(newText);
                _previousWordClickHandler.SetText(prevText);
            }
        }

        private void HideMainTextBlock()
        {
            if (_subtitleTextBlock != null)
            {
                _subtitleTextBlock.Dispatcher.Invoke(() =>
                {
                    _subtitleTextBlock.Visibility = Visibility.Collapsed;
                });
            }
        }

        private void HidePrevTextBlock()
        {
            if (_prevSubtitleBlock != null)
            {
                _prevSubtitleBlock.Dispatcher.Invoke(() =>
                {
                    _prevSubtitleBlock.Visibility = Visibility.Collapsed;
                });
            }
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
                
                await HandleFileOpenAsync(filePath);
            }
        }

        //private async Task WaitForMediaPlayerStoppedAsync()
        //{
        //    // 轮询直到播放器状态为 Stopped
        //    while (!_mediaPlayer.)
        //    {
        //        await Task.Delay(50);
        //    }
        //}


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

            if (DateTime.Now.Subtract(_lastClickTime).TotalMilliseconds <= DOUBLE_CLICK_INTERVAL)
            {
                ToggleFullScreen();
            }
            else
            {
                ToggleMediaPlay();
            }

            _lastClickTime = DateTime.Now;
        }

        [RelayCommand]
        private void ToggleScrollingSubtitles(bool isEnabled)
        {
            IsScrollingEnabled = isEnabled;
            //_scrollingSubtitleHandler?.EnableScrolling(isEnabled);
        }

        //[RelayCommand]
        //private void WindowSizeChanged(SizeChangedEventArgs e)
        //{
        //    Debug.WriteLine($"size {e}");
        //}

        public void OnWindowSizeChanged(Size newSize)
        {
            //防止视频载入的尺寸变化触发
            if (_isVideoLoading || _isChangingFullScreen) {
                return;
                    };
            var currentTime = _mediaPlayer.Time;
            if (currentTime - _sizeChangingTimer > 500)
            {
                if (_mediaPlayer.Media != null & !IsFullScreen & _hasAdjustedAspectRatio)
                {
                    var result = _windowSizeHandler.CalculateResizedVideoSize(newSize.Width, newSize.Height, double.Parse(VideoViewWidth), double.Parse(VideoViewHeight));

                    VideoViewWidth = (result.Item1).ToString();
                    VideoViewHeight = (result.Item2).ToString();
                }
            }
        }

        //public void OnSubtitleAreaMouseEnter()
        //{
        //        if (SubtitleDisplayMode != "Hide")
        //        {
        //            //ShowTextBlock();

        //            ShowSubtitle();
        //            SetSubtitleBackground();
        //    }
        //}

        public void OnSubtitleAreaMouseEnter()
        {
            if (SubtitleDisplayMode == "Hover")
            {
                // 主字幕 textblock：有内容则显示
                if (!string.IsNullOrEmpty(SubtitleText) && _subtitleTextBlock != null)
                {
                    _subtitleTextBlock.Dispatcher.Invoke(() =>
                    {
                        //ShowTextBlock(true);
                        ShowSubtitle();
                    });
                }
                // 上一句字幕 textblock：有内容则显示
                if (!string.IsNullOrEmpty(PreviousSubtitleText) && _prevSubtitleBlock != null)
                {
                    _prevSubtitleBlock.Dispatcher.Invoke(() =>
                    {
                        _prevSubtitleBlock.Visibility = Visibility.Visible;
                    });
                }
                SetSubtitleBackground();
            }
            else if (SubtitleDisplayMode == "Always")
            {
                // Always 模式下直接刷新背景即可（UpdateSubtitleText 已经根据内容做过显示/隐藏处理）
                SetSubtitleBackground();
            }
        }

        public void OnSubtitleAreaMouseLeave()
        {
            if (SubtitleDisplayMode == "Hover")
            {
                HideTextBlocks();
            }
        }

        public void OnSubtitleAreaMouseLeftButtonDown(System.Windows.Input.MouseEventArgs e)
        {
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

                if (_isSentenceAnalysisEnabled)
                {
                    _sentencePanelView = new SentencePanelView();
                    _sentencePanelView.CloseRequested += (s, args) =>
                    {
                        _sentenceContainer.Children.Remove(_sentencePanelView);
                        _sentencePanelView = null;
                    };
                    _sentenceContainer.Children.Add(_sentencePanelView);
                    _sentencePanelView.Show(_subtitleHandler.GetFullSentence(false), e.GetPosition(_sentenceContainer));
                }
            }
        }

        public void OnPrevSubtitleAreaMouseLeftButtonDown(System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && previousSubtitleText.Length > 0)
            {
                if (_translationService != null)
                {
                    _translationService.CloseTranslation();
                }
                if (_sentencePanelView != null)
                {
                    _sentencePanelView.Close();
                    _sentenceContainer.Children.Remove(_sentencePanelView);
                }

                if (_isSentenceAnalysisEnabled)
                {
                    _sentencePanelView = new SentencePanelView();
                    _sentencePanelView.CloseRequested += (s, args) =>
                    {
                        _sentenceContainer.Children.Remove(_sentencePanelView);
                        _sentencePanelView = null;
                    };
                    _sentenceContainer.Children.Add(_sentencePanelView);
                    _sentencePanelView.Show(_subtitleHandler.GetFullSentence(true), e.GetPosition(_sentenceContainer));
                }
            }
        }

        public void SetSubtitleBlocks(TextBlock mainBlock, TextBlock prevBlock, TextBlock nextBlock)
        {
            if (mainBlock == null)
                throw new ArgumentNullException(nameof(mainBlock));

            _subtitleTextBlock = mainBlock;
            _prevSubtitleBlock = prevBlock;
            _wordClickHandler.SetTextBlock(mainBlock);
            _previousWordClickHandler.SetTextBlock(prevBlock);
            // 如有需要，可将 nextBlock 保存供后续使用
            // _nextSubtitleBlock = nextBlock;
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

        private async void ToggleMediaPlay()
        {
            if(_mediaPlayer.Time <= 1000)//防止视频载入时误操作
            {
                return;
            }
            //Debug.WriteLine();
            if (_mediaPlayer.IsPlaying)
            {
                //_mediaPlayer.Pause();
                _mediaPlayer.SetPause(true);
            }
            else
            {
                //_mediaPlayer.Play();
                _mediaPlayer.SetPause(false);
            }
        }

        public void DisposeMedia()
        {
            _mediaPlayer?.Dispose();
            _libVLC?.Dispose();
        }

        #endregion

        #region  Private Helpers
        private async Task WaitForMediaPlayerStoppedAsync()
        {
            // 轮询直到播放器状态为 Stopped
            while (!_isStopped)
            {
                await Task.Delay(50);
            }
        }

        public void ToggleFullScreen()
        {
            _isChangingFullScreen = true;
            _isFullScreen = !_isFullScreen;
            ToggleFullScreenRequested?.Invoke(this, _isFullScreen);
            if (IsFullScreen)
            {
                FullVideoView(null, null);
                MenuBarVM.IsMenuBarVisible = false;
            }
            else
            {
                NormalVideoView(null, null);
                MenuBarVM.IsMenuBarVisible = true;
            }

            _sizeChangingTimer = _mediaPlayer.Time;
            _isChangingFullScreen = false;
        }


        public async Task HandleFileOpenAsync(string filePath)
        {
            Debug.WriteLine($"filepath {filePath}");
            var extension = Path.GetExtension(filePath).ToLower();

            if (extension == ".srt" || extension == ".ass")
            {
                LoadSubtitle(filePath);
            }
            else
            {
                _hasAdjustedAspectRatio = false;
                _isVideoLoading = true;
                // 打开视频文件

                // 如果当前正在播放，先停止播放保证能安全释放媒体资源
                if (!_isStopped)
                {
                    _mediaPlayer.Stop();
                    WaitForMediaPlayerStoppedAsync();
                }

                _mediaPlayer.Media?.Dispose();
                _mediaPlayer.Media = new Media(_libVLC, new Uri(filePath));
                _mediaPlayer.Play();
                //_isStopped = false;

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
                if (!EmbeddedSubtitleFiles.Any() || EmbeddedSubtitleFiles is null)
                {
                    subtitleFiles.Clear();

                    subtitleFiles = await SubtitleExtractHandler.ExtractEmbeddedSubtitlesAsync(filePath);
                    EmbeddedSubtitleFiles = new ObservableCollection<string>(subtitleFiles);
                }

                if (EmbeddedSubtitleFiles.Any())
                {
                    foreach (var file in EmbeddedSubtitleFiles)
                    {
                        if (file.Contains("OriginalLang") && !file.Contains("SDH"))
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

        private void ShowSubtitle()
        {
            if (_subtitleTextBlock != null)
            {
                _subtitleTextBlock.Dispatcher.Invoke(() =>
                {
                    _subtitleTextBlock.Visibility = string.IsNullOrEmpty(SubtitleText) ? Visibility.Collapsed : Visibility.Visible;
                });
            }
            if (_prevSubtitleBlock != null)
            {
                _prevSubtitleBlock.Dispatcher.Invoke(() =>
                {
                    _prevSubtitleBlock.Visibility = string.IsNullOrEmpty(PreviousSubtitleText) ? Visibility.Collapsed : Visibility.Visible;
                });
            }
            _subtitleHandler.Show();
        }

        private void HideSubtitle()
        {
            _subtitleHandler.Hide();
            HideTextBlocks();
        }

        private void HideTextBlocks()
        {
            HideMainTextBlock();
            HidePrevTextBlock();
        }



        private void ShowTextBlock(bool isMain = true)
        {
            if (!_subtitleHandler.IsAnySubtitle())
            {
                return;
            }
            if (isMain)
            {
                _subtitleTextBlock.Dispatcher.Invoke(() =>
                {
                    _subtitleTextBlock.Visibility = Visibility.Visible;
                });
            }
            else
            {
                _prevSubtitleBlock.Dispatcher.Invoke(() =>
                {
                    _prevSubtitleBlock.Visibility = Visibility.Visible;
                });
            }
        }

        private void ShowTextBlocks()
        {
            ShowTextBlock(true);
            ShowTextBlock(false);
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

        private void HandleWordClickEvent(object sender, WordClickEventArgs e)
        {
            if(_sentencePanelView != null)
            {
                _sentencePanelView.Close();
                _sentenceContainer.Children.Remove(_sentencePanelView);
            }

            if (_translationService != null & _isWordQueryEnabled)
            {
                _translationService?.ShowTranslation(e.Word, e.Position);
            }
        }


        #endregion

        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // 根据 setting 对象更新 ViewModel 中对应的值
            if (Properties.Settings.Default == null)
            {
                return;
            }

            switch (e.PropertyName)
            {
                case "YourLanguage":
                    // 更新你的语言设置
                    YourLanguage = Properties.Settings.Default.YourLanguage;
                    break;
                case "LearningLanguage":
                    // 更新学习语言设置
                    LearningLanguage = Properties.Settings.Default.LearningLanguage;
                    break;
                case "IsWordQueryEnabled":
                    // 更新是否启用单词查询的标志
                    _isWordQueryEnabled = Properties.Settings.Default.IsWordQueryEnabled;
                    break;
                case "BackwardTime":
                    // 更新向后跳转的时间（秒）
                    BackwardTime = Properties.Settings.Default.BackwardTime;
                    break;
                case "ForwardTime":
                    // 更新向前跳转的时间（秒）
                    ForwardTime = Properties.Settings.Default.ForwardTime;
                    break;
                //Is sentence analysis enabled
                case "IsSentenceAnalysisEnabled":
                    _isSentenceAnalysisEnabled = Properties.Settings.Default.IsSentenceAnalysisEnabled;
                    break;
                default:
                    break;
            }
        }

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
                        if (IsBackwardBySentence)
                        {
                            // 获取当前字幕起始时间
                            long currentStart = _subtitleHandler.GetCurrentSubtitleStartTime();
                            // 如果播放时间距当前字幕起始时间不超过 0.3 秒，则尝试跳到上一条字幕的起始时间
                            if (MediaPlayer.Time - currentStart <= 300)
                            {
                                long prevStart = _subtitleHandler.GetPreviousSubtitleStartTime();
                                MediaPlayer.Time = prevStart;
                            }
                            else
                            {
                                MediaPlayer.Time = currentStart;
                            }
                        }
                        else
                        {
                            long newTime = MediaPlayer.Time - BackwardTime * 1000; // 例如 10 秒钟后
                            if (newTime < 0)
                                newTime = 0;
                            MediaPlayer.Time = newTime;
                        }
                    }
                    break;

                case Key.Right:
                    // Skip forward
                    // 向前跳转：跳转到下一句的开始
                    if (MediaPlayer?.Media != null)
                    {
                        if (IsForwardBySentence)
                        {
                            long nextStart = _subtitleHandler.GetNextSubtitleStartTime();
                            MediaPlayer.Time = nextStart;
                        }
                        else
                        {
                            long newTime = MediaPlayer.Time + ForwardTime * 1000;
                            if (newTime > MediaPlayer.Length)
                                newTime = MediaPlayer.Length;
                            MediaPlayer.Time = newTime;
                        }
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

        private void UpdateAudioTracks()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                AudioTracks.Clear();
                var trackDescriptions = _mediaPlayer.AudioTrackDescription;
                if (trackDescriptions != null)
                {
                    foreach (var desc in trackDescriptions)
                    {
                        Debug.WriteLine($"desc {desc.Id} {desc.Name}");
                        if (desc.Id != -1)
                        {
                            AudioTracks.Add(new AudioTrackInfo(desc.Id, desc.Name));
                        }
                    }
                    if (AudioTracks.Any())
                    {
                        SelectedAudioTrack = AudioTracks.First();
                    }
                }
                menuBarVM.UpdateAudioTracks(AudioTracks);
            });
        }

        private void HandleAudioTrackChanged(object sender, AudioTrackInfo track)
        {

            if (track != null)
            {
                // 更新选中音轨属性
                SelectedAudioTrack = track;
                // 调用 LibVLCSharp 音频接口切换音轨
                if (_mediaPlayer != null && _mediaPlayer.AudioTrack != -1)
                {
                    _mediaPlayer.SetAudioTrack(track.Id);
                }
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
