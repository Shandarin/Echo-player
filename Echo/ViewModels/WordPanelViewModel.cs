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
        private string errorMessage;

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
                MessageBox.Show("No word selected to toggle favorite status.- wordPanel");
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

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error toggling favorite: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task PlayPronunciation(string audioUrl)
        {
            Debug.WriteLine($"audioUrl {audioUrl}");
            if (string.IsNullOrEmpty(audioUrl)) return;
            await OnlineAudioPlayService.PlayAudioAsync(audioUrl);
        }

        [RelayCommand]
        private async Task TranslateWordAsync(string word)
        {
            try
            {
                var sourceLang = MainWindowVM.LearningLanguage;
                var targetLang = MainWindowVM.YourLanguage;

                var subtitleItem = MainWindowVM.CurrentSubtitleItem;//先获取当前字幕时间，以免延迟导致时间不准确

                IsLoading = true;
                ErrorMessage = null;

                //clickHandler已经转小写
                //word = word.ToLower();

                // Check if word is already in local database,if so ,return a simple word model
                var wordLocal = await _databaseService.GetWordFromLocalAsync(word, sourceLang, targetLang);

                if (wordLocal != null)
                {
                    HeadwordOP = wordLocal.Word;
                    Pronunciations = wordLocal.Pronounciations;
                    Definitions = wordLocal.Definitions;
                    CurrentWordModel = wordLocal;
                    AddInfo();
                    await CheckIfCollected(wordLocal);
                    return ;
                }

                WordModel? wordModel = new ();
                //请求Echo Server
                if (Properties.Settings.Default.IsUseEchoAPI)
                {
                    var responseString= await EchoService.OxfordAPIRequest(word,sourceLang,targetLang);
                    if (responseString is null)
                    {
                        ErrorMessage = "Word not found";
                        return;
                    }

                    wordModel = await EchoService.ParseOxfordAsync(responseString);
                    if (wordModel == null)
                    {
                        ErrorMessage = "Word not found";
                        return;
                    }
                }
                else
                {

                    // First API call to get head word and other details
                    var (headword, details) = await _oxfordService.GetWordDetailsAsync(word, sourceLang);
                    if (string.IsNullOrEmpty(headword))
                    {
                        ErrorMessage = "Word not found";
                        return;
                    }

                    wordLocal = await _databaseService.GetWordFromLocalAsync(headword.ToLower(), sourceLang, targetLang);
                    if (wordLocal != null)
                    {
                        HeadwordOP = wordLocal.Word;
                        Pronunciations = wordLocal.Pronounciations;
                        Definitions = wordLocal.Definitions;
                        CurrentWordModel = wordLocal;
                        AddInfo();
                        await CheckIfCollected(wordLocal);
                        return;
                    }
                    //_wordDetails = details;

                    // Second API call to get translations
                    wordModel = await _oxfordService.GetTranslationsAsync(headword, sourceLang, targetLang);

                    if (wordModel?.Definitions == null || !wordModel.Definitions.Any())
                    {
                        ErrorMessage = "Translation not found";

                        return;
                    }

                }


                string json = System.Text.Json.JsonSerializer.Serialize(wordModel, new JsonSerializerOptions
                {
                    WriteIndented = true   // 缩进美化输出
                });

                HeadwordOP = wordModel.Word;

                await CheckIfCollected(wordModel);

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
            Debug.WriteLine($"IsFavorite {IsFavorite}");
        }

        [RelayCommand]
        private void Close()
        {
            IsVisible = false;
        }
    }
}