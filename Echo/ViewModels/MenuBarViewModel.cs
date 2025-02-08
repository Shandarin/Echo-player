using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Echo.Properties;
using Echo.Services;
using Echo.Views;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;

namespace Echo.ViewModels
{
    public partial class MenuBarViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isSubtitleVisible;

        [ObservableProperty]
        private bool _isMouseHoverEnabled;

        [ObservableProperty]
        private bool _isSentenceAnalysisEnabled;

        [ObservableProperty]
        private bool _isWordQueryEnabled;

        //[ObservableProperty]
        //private bool _isScrollSubtitleEnabled = true;

        [ObservableProperty]
        private double _subtitleOpacity;

        //[ObservableProperty]
        //private string _softwareLanguage;

        //[ObservableProperty]
        //private string currentFontFamily = "Arial";

        //[ObservableProperty]
        //private double currentFontSize = 20;

        //[ObservableProperty]
        //private string currentSubtitleTrack = "Track 1";

        //[ObservableProperty]
        //private double currentScale = 1.0;

        [ObservableProperty]
        private string _selectedAspectRatio;

        [ObservableProperty]
        private bool isMenuBarVisible = true;

        //[ObservableProperty]
        //private double textBlockMarginBottom = 10;

        [ObservableProperty]
        private string _selectedSoftwareLanguage;

        [ObservableProperty]
        private string _selectedYourLanguage;

        [ObservableProperty]
        private string _selectedLearningLanguage;

        [ObservableProperty]
        private uint _backwardTime;

        [ObservableProperty]
        private uint _forwardTime;

        [ObservableProperty]
        private string _subtitleDisplayMode;

        [ObservableProperty]
        private ObservableCollection<string> _embeddedSubtitleFiles = new();

        [ObservableProperty]
        private string _detectedLanguage ;

        [ObservableProperty]
        private bool _isUseThirdPartyAPI;

        [ObservableProperty]
        private bool _isUseEchoAPI;

        private readonly UpdateService _updateService = new("https://echo-player.com/updates/update.xml");

        public MenuBarViewModel() 

        {
            IsSubtitleVisible = Properties.Settings.Default.IsSubtitleVisible;
            IsMouseHoverEnabled = Properties.Settings.Default.IsMouseHoverEnabled;
            IsSentenceAnalysisEnabled =  Properties.Settings.Default.IsSentenceAnalysisEnabled;
            IsWordQueryEnabled = Properties.Settings.Default.IsWordQueryEnabled;
            SubtitleOpacity =  Properties.Settings.Default.SubtitleOpacity;
            SelectedSoftwareLanguage = Properties.Settings.Default.SoftwareLanguage;
            SelectedAspectRatio = Properties.Settings.Default.AspectRatio;
            SelectedYourLanguage = Properties.Settings.Default.YourLanguage;
            SelectedLearningLanguage = Properties.Settings.Default.LearningLanguage;

            BackwardTime = Properties.Settings.Default.BackwardTime;
            ForwardTime = Properties.Settings.Default.ForwardTime;
            SubtitleDisplayMode = Properties.Settings.Default.SubtitleDisplayMode;

            IsUseEchoAPI= Properties.Settings.Default.IsUseEchoAPI;
            IsUseThirdPartyAPI = Properties.Settings.Default.IsUseThirdPartyAPI;

            DetectedLanguage = null;
        }


        partial void OnDetectedLanguageChanged(string value)
        {
            if (SelectedLearningLanguage == "Auto")
            {
                OnLearningLanguageChanged?.Invoke(this, value);
            }
        }

        // File Menu Commands
        [RelayCommand]
        private void OpenFile()
        {
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow?.DataContext is MainWindowViewModel vm)
            {
                 vm.OpenFileAsync();
            }
        }

        [RelayCommand]
        private void ChangeAspectRatio(string ratio)
        {
            SelectedAspectRatio = ratio;
            OnAspectRatioChanged?.Invoke(this, ratio);
        }

        [RelayCommand]
        private void Exit()
        {
            Application.Current.Shutdown();
        }

        // Video Menu Commands
        [RelayCommand]
        private void Screenshot()
        {
            OnScreenshotRequested?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void ChangeScale(double scale)
        {
            OnScaleChanged?.Invoke(this, scale);
        }

        [RelayCommand]
        private void ToggleFullScreen()
        {
            OnFullScreenToggled?.Invoke(this, EventArgs.Empty);
        }

        // Subtitle Menu Commands
        [RelayCommand]
        private void OpenSubtitle()
        {
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow?.DataContext is MainWindowViewModel vm)
            {
                vm.OpenSubtitle();
            }
        }

        [RelayCommand]
        private void SelectSubtitleTrack(string track)
        {
            OnSubtitleTrackSelected?.Invoke(this, track);
        }

        [RelayCommand]
        private void ChangeOpacity(string opacity)
        {
            OnChangeOpacity?.Invoke(this, opacity);
        }

        [RelayCommand]
        private void ChangeFontSize(string size)
        {
            //Debug.WriteLine(size);
            OnFontSizeChanged?.Invoke(this, size);
        }

        [RelayCommand]
        private void ChangeFontFamily(string fontFamily)
        {
            OnFontFamilyChanged?.Invoke(this, fontFamily);
        }

        // Dictionary Menu Commands
        [RelayCommand]
        private void ConfigureWordAPI()
        {
            string currentApiId = Settings.Default.OxfordApiId;
            string currentApiKey = Settings.Default.OxfordApiKey;

            OxfordConfigDialog dialog = new OxfordConfigDialog(currentApiId, currentApiKey);
            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                // 用户点击确定后，更新 API 信息（此处可根据实际需求调整保存逻辑）
                Settings.Default.OxfordApiId = dialog.ApiId;
                Settings.Default.OxfordApiKey = dialog.ApiKey;
                Settings.Default.Save();
            }
        }

        [RelayCommand]
        private void ConfigureSentenceAPI()
        {
            string savedKey = Properties.Settings.Default.OpenAIApiKey;
            var dialog = new InputDialog("OpenAI API Key：", "API", savedKey);
            if (dialog.ShowDialog() == true)
            {
                string apiKey = dialog.ResponseText;

                if (string.IsNullOrEmpty(apiKey))
                {
                    return;
                }
                Properties.Settings.Default.OpenAIApiKey = apiKey;
                Properties.Settings.Default.Save();
            }
        }

        [RelayCommand]
        private void ToggleScrollSubtitle()
        {
            var mainWindow = Application.Current.MainWindow as MainWindow;


            if (mainWindow?.DataContext is MainWindowViewModel vm)
            {
                //IsScrollSubtitleEnabled = !IsScrollSubtitleEnabled;
                //Debug.WriteLine(IsScrollSubtitleEnabled);
                //vm.ToggleScrollingSubtitlesCommand.Execute(IsScrollSubtitleEnabled);

            }
        }

        [RelayCommand]
        private void OpenNote()
        {
            var window = new NoteWindowView();
            //window.AllowsTransparency = true;
            //window.WindowStyle = WindowStyle.None;
            window.Show();
        }

        [RelayCommand]
        private void SoftwareLanguageChange(string lang)
        {
            string selectedLanguage = lang; 
            ((App)Application.Current).ChangeLanguage(selectedLanguage);
            SelectedSoftwareLanguage = selectedLanguage;
        }

        [RelayCommand]
        private void ChangeYourLanguage(string lang)
        {
            SelectedYourLanguage = lang;
            Properties.Settings.Default.YourLanguage = SelectedYourLanguage;
            //YourLanguageChanged?.Invoke(this, lang);
        }

        [RelayCommand]
        private void ChangeLearningLanguage(string lang)
        {
            SelectedLearningLanguage = lang;
            Properties.Settings.Default.LearningLanguage = SelectedLearningLanguage;
            //LearningLanguageChanged?.Invoke(this, lang);
        }

        [RelayCommand]
        public void SetSubtitleDisplayMode(string mode)
        {
            SubtitleDisplayModeChangedEvent?.Invoke(this, mode);
        }

        [RelayCommand]
        public void CheckUpdates()
        {
           _updateService.CheckForUpdates();
        }




        public void UpdateSubtitle(ObservableCollection<string> ES,bool HasES)
        {
            //HasEmbeddedSubtitles = HasES;
            EmbeddedSubtitleFiles = ES;
        }


        public void SaveSettings()
        {
            Properties.Settings.Default.IsSubtitleVisible = IsSubtitleVisible;
            Properties.Settings.Default.IsMouseHoverEnabled = IsMouseHoverEnabled;
            Properties.Settings.Default.IsSentenceAnalysisEnabled = IsSentenceAnalysisEnabled;
            Properties.Settings.Default.IsWordQueryEnabled = IsWordQueryEnabled;
            Properties.Settings.Default.SubtitleOpacity = SubtitleOpacity;
            Properties.Settings.Default.SoftwareLanguage = SelectedSoftwareLanguage;
            Properties.Settings.Default.AspectRatio = SelectedAspectRatio;
            
            
            Properties.Settings.Default.BackwardTime = BackwardTime;
            Properties.Settings.Default.ForwardTime = ForwardTime;
            Properties.Settings.Default.SubtitleDisplayMode = SubtitleDisplayMode;

            Properties.Settings.Default.Save();
        }

        partial void OnIsMouseHoverEnabledChanged(bool value)
        {
            MouseHoverEnabledChanged?.Invoke(this,value);
        }

        partial void OnIsSubtitleVisibleChanged(bool value)
        {


            SubtitleVisibleChanged?.Invoke(this, value);
        }

        partial void OnBackwardTimeChanged(uint value)
        {
            BackwardTimeChanged?.Invoke(this, value);
        }

        partial void OnForwardTimeChanged(uint value)
        {
            ForwardTimeChanged?.Invoke(this, value);
        }

        partial void OnIsSentenceAnalysisEnabledChanged(bool value)
        {
            IsSentenceAnalysisEnabledChanged?.Invoke(this,value);
        }

        partial void OnIsWordQueryEnabledChanged(bool value)
        {
            IsWordQueryEnabledChanged?.Invoke(this, value);
        }

        partial void OnIsUseThirdPartyAPIChanged(bool value)
        {
            Properties.Settings.Default.IsUseThirdPartyAPI = IsUseThirdPartyAPI;
            Properties.Settings.Default.Save();
            Debug.WriteLine($"IsUseThirdPartyAPI: {IsUseThirdPartyAPI}");
            if (IsUseThirdPartyAPI)
            {
                IsUseEchoAPI = false;
            }
        }

        partial void OnIsUseEchoAPIChanged(bool value)
        {
            Properties.Settings.Default.IsUseEchoAPI = IsUseEchoAPI;
            Properties.Settings.Default.Save();
            Debug.WriteLine($"IsUseEchoAPI: {IsUseEchoAPI}");
            if (IsUseEchoAPI)
            {
                IsUseThirdPartyAPI = false;
            }
        }


        // Events
        public event EventHandler<string> OnFileSelected;
        public event EventHandler OnScreenshotRequested;
        public event EventHandler<string> OnAspectRatioChanged;
        public event EventHandler<double> OnScaleChanged;
        public event EventHandler OnFullScreenToggled;
        public event EventHandler<string> OnSubtitleFileSelected;
        public event EventHandler<string> OnSubtitleTrackSelected;
        public event EventHandler<string> OnFontSizeChanged;
        public event EventHandler<string> OnFontFamilyChanged;
        public event EventHandler<string> OnSoftwareLanguageChanged;
        public event EventHandler<string> OnChangeOpacity;
        public event EventHandler<string> YourLanguageChanged;
        public event EventHandler<string> LearningLanguageChanged;

        public event EventHandler<bool> SubtitleVisibleChanged;
        public event EventHandler<bool> MouseHoverEnabledChanged;
        public event EventHandler<uint> BackwardTimeChanged;
        public event EventHandler<uint> ForwardTimeChanged;

        public event EventHandler<string> SubtitleDisplayModeChangedEvent;
        public event EventHandler<bool> IsSentenceAnalysisEnabledChanged;
        public event EventHandler<bool> IsWordQueryEnabledChanged;

        public event EventHandler<string> OnLearningLanguageChanged;

        // Additional commands can be added for other menu items
    }
}