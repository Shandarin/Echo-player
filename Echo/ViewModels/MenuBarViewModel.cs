using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Echo.Views;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Windows;

namespace Echo.ViewModels
{
    public partial class MenuBarViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool isSubtitleVisible = true;

        [ObservableProperty]
        private bool isMouseHoverEnabled = true;

        [ObservableProperty]
        private bool isSentenceAnalysisEnabled = true;

        [ObservableProperty]
        private bool isWordQueryEnabled = true;

        [ObservableProperty]
        private bool isScrollSubtitleEnabled = true;

        [ObservableProperty]
        private double subtitleOpacity = 0.5;

        [ObservableProperty]
        private string currentLanguage = "zh";

        [ObservableProperty]
        private string currentFontFamily = "Arial";

        [ObservableProperty]
        private double currentFontSize = 20;

        [ObservableProperty]
        private string currentSubtitleTrack = "Track 1";

        [ObservableProperty]
        private double currentScale = 1.0;

        [ObservableProperty]
        private string selectedAspectRatio = "Default";

        [ObservableProperty]
        private bool isMenuBarVisible = true;

        [ObservableProperty]
        private double textBlockMarginBottom = 10;

        [ObservableProperty]
        private string _selectedSoftwareLanguage = "zh-Hans";

        [ObservableProperty]
        private string _selectedYourLanguage = "zh";

        [ObservableProperty]
        private string _selectedLearningLanguage = "en";

        [ObservableProperty]
        private int _backwardTime = 10;

        [ObservableProperty]
        private int _forwardTime = 10;

        public MenuBarViewModel() 

        {
            
        }

        // File Menu Commands
        [RelayCommand]
        private void OpenFile()
        {
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow?.DataContext is MainWindowViewModel vm)
            {
                vm.OpenFile();
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
            var dialog = new OpenFileDialog
            {
                Filter = "Subtitle Files|*.srt;*.ass;*.ssa|All Files|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                OnSubtitleFileSelected?.Invoke(this, dialog.FileName);
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

        [RelayCommand]
        private void ChangeSystemLanguage(string language)
        {
            //CurrentLanguage = language;
            OnSystemLanguageChanged?.Invoke(this, language);
        }

        // Dictionary Menu Commands
        [RelayCommand]
        private void ConfigureWordAPI()
        {
            // Implement API configuration logic
        }

        [RelayCommand]
        private void ConfigureSentenceAPI()
        {
            // Implement API configuration logic
        }

        [RelayCommand]
        private void ToggleScrollSubtitle()
        {
            var mainWindow = Application.Current.MainWindow as MainWindow;


            if (mainWindow?.DataContext is MainWindowViewModel vm)
            {
                IsScrollSubtitleEnabled = !IsScrollSubtitleEnabled;
                //Debug.WriteLine(IsScrollSubtitleEnabled);
                vm.ToggleScrollingSubtitlesCommand.Execute(IsScrollSubtitleEnabled);

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
            YourLanguageChanged?.Invoke(this, lang);
        }

        [RelayCommand]
        private void ChangeLearningLanguage(string lang)
        {
            SelectedLearningLanguage = lang;
            LearningLanguageChanged?.Invoke(this, lang);
        }

        partial void OnIsMouseHoverEnabledChanged(bool value)
        {
            MouseHoverEnabledChanged?.Invoke(this,value);
        }

        partial void OnIsSubtitleVisibleChanged(bool value)
        {
            MouseHoverEnabledChanged?.Invoke(this, value);
        }

        partial void OnBackwardTimeChanged(int value)
        {
            BackwardTimeChanged?.Invoke(this, value);
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
        public event EventHandler<string> OnSystemLanguageChanged;
        public event EventHandler<string> OnChangeOpacity;
        public event EventHandler<string> YourLanguageChanged;
        public event EventHandler<string> LearningLanguageChanged;

        public event EventHandler<bool> SubtitleVisibleChanged;
        public event EventHandler<bool> MouseHoverEnabledChanged;
        public event EventHandler<int> BackwardTimeChanged;
        public event EventHandler<int> ForwardTimeChanged;

        // Additional commands can be added for other menu items
    }
}