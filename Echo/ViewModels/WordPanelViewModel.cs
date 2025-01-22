using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Echo.Managers;
using Echo.Models;
using Echo.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SubtitlesParser.Classes;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace Echo.ViewModels
{
    public partial class WordPanelViewModel : ObservableObject
    {
        private readonly OxfordDictService _oxfordService;
        private readonly DatabaseService _databaseService;

        //private MessageManager _messageManager = new();

        [ObservableProperty]
        private string word;

        [ObservableProperty]
        private List<PronunciationModel>? _pronunciations = new();

        [ObservableProperty]
        private Dictionary<string, string>  _definitions = new();

        [ObservableProperty]
        private string _headwordOP;

        [ObservableProperty]
        private bool _isFavorite;

        [ObservableProperty]
        private string _currentWord;

        [ObservableProperty]
        private string _translation;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _errorMessage;

        [ObservableProperty]
        private bool _isVisible;

        [ObservableProperty]
        private bool isAutoSaveWord;

        [ObservableProperty]
        private WordModel currentWordModel = new();

        [ObservableProperty]
        private string _favoriteIcon;

        MainWindowViewModel MainWindowVM = Application.Current.MainWindow.DataContext as MainWindowViewModel;

        public WordPanelViewModel(OxfordDictService oxfordService, DatabaseService databaseService)
        {
            _oxfordService = oxfordService;
            _databaseService = databaseService;

            FavoriteIcon = "/Assets/images/collect.png";//要有预设值否则会出错
            //_ = CheckIfCollected();
            //Debug.WriteLine($"IsFavorite: {IsFavorite}");
        }

        public void UpdateFromWordModel(WordModel model)
        {
            if (model == null) return;

            Word = model.Word;

            //Definitions = model.Definitions;
        }

        [RelayCommand]
        private async Task ToggleFavoriteAsync()
        {

            if (string.IsNullOrWhiteSpace(CurrentWordModel.Word))
            {
                MessageBox.Show("No word selected to toggle favorite status.");
                return;
            }

            try
            {
                IsFavorite = !IsFavorite;
                FavoriteIcon = IsFavorite ? "/Assets/images/collect-active.png" : "/Assets/images/collect.png";
                // Toggle favorite status
                //CurrentWordModel.IsFavorite = !CurrentWordModel.IsFavorite;

                // Save to database
                if (IsFavorite)
                {
                    await _databaseService.CollectionLinkAsync(CurrentWordModel, CurrentWordModel.SourceFileName);
                }
                else
                {
                    await _databaseService.RemoveCollectionLinkAsync(CurrentWordModel, CurrentWordModel.SourceFileName);
                }
                Debug.WriteLine($"CurrentWordModel.Word {CurrentWordModel.Word}");

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error toggling favorite: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task PlayPronunciation(string audioUrl)
        {
            if (string.IsNullOrEmpty(audioUrl)) return;
            // TODO: Implement audio playback
        }

        [RelayCommand]
        private async Task TranslateWordAsync(string word)
        {
            
            try
            {
                var subtitleItem = MainWindowVM.CurrentSubtitleItem;//先获取当前字幕时间，以免延迟导致时间不准确
                //CurrentWordModel.SourceStartTime = subtitleItem.StartTime;
                //CurrentWordModel.SourceEndTime = subtitleItem.EndTime;

                IsLoading = true;
                ErrorMessage = null;
                //CurrentWord = word;

                // First API call to get head word and other details
                var (headword, details) = await _oxfordService.GetWordDetailsAsync(word);
                if (string.IsNullOrEmpty(headword))
                {
                    ErrorMessage = "Word not found";
                    
                    return;
                }
                //_wordDetails = details;

                // Second API call to get translations
                var wordModel = await _oxfordService.GetTranslationsAsync(headword);

                if (wordModel?.Definitions == null || !wordModel.Definitions.Any())
                {
                    ErrorMessage = "Translation not found";
                   
                    return;
                }

                string json = System.Text.Json.JsonSerializer.Serialize(wordModel, new JsonSerializerOptions
                {
                    WriteIndented = true   // 缩进美化输出
                });

                HeadwordOP = wordModel.Word;

                await CheckIfCollected(wordModel);

                //Translation = wordModel.Translation;


                //if (IsAutoSaveWord)
                //{
                //    // Save to database
                //    await SaveToDatabase();
                //}

                // Update observable collections

                Pronunciations.Clear();
                if (wordModel.Pronounciations != null)
                {
                    Pronunciations = wordModel.Pronounciations;
                }

                Definitions.Clear();
                if (wordModel.Definitions != null)
                {
                    foreach (var def in wordModel.Definitions)
                    {
                        Definitions = wordModel.Definitions; ;
                    }
                }

                CurrentWordModel = wordModel;

                CurrentWordModel.SourceStartTime = subtitleItem.StartTime;
                CurrentWordModel.SourceEndTime = subtitleItem.EndTime;

                IsVisible = true;

                AddInfo();
                await _databaseService.CheckAndSaveAsync(CurrentWordModel);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                IsLoading = false;
            }

        }

        private void AddInfo()
        {
            CurrentWordModel.SourceFilePath = MainWindowVM.VideoFilePath; 
            CurrentWordModel.SourceFileName = Path.GetFileName(MainWindowVM.VideoFilePath);
            //CurrentWordModel.LanguageCode = "en";
            //CurrentWordModel.LanguageCode = MainWindowVM.SourceLanguage;
            //CurrentWordModel.SourceStartTime = MainWindowVM.CurrentSubtitle.StartTime;
            //CurrentWordModel.SourceEndTime = MainWindowVM.CurrentSubtitle.EndTime;
        }


        private async Task CheckIfCollected(WordModel wordM)
        {
            if (await _databaseService.CheckCollectionLinkExistAsync(wordM))
            {
                IsFavorite = true;
                FavoriteIcon = "/Assets/images/collect-active.png";
            }
            else
            {
                IsFavorite = false;
                FavoriteIcon = "/Assets/images/collect.png";
            }
        }

        [RelayCommand]
        private void Close()
        {
            IsVisible = false;
        }
    }
}