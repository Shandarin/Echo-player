﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Echo.Managers;
using Echo.Models;
using Echo.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Echo.ViewModels
{
    public partial class SentencePanelViewModel: ObservableObject
    {
        private readonly DatabaseService _databaseService;
        private readonly OpenAIService _openAiService;

        private SentenceModel _sentenceModel;
        private string _collectionName;

        private string _translationText;

        //private MessageManager _messageManager = new()

        [ObservableProperty]
        private bool _isFavorite;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _errorMessage;

        [ObservableProperty]
        private bool _isVisible;

        [ObservableProperty]
        private string _sentence;

        [ObservableProperty]
        private string _favoriteIcon;

        //[ObservableProperty]
        //private string _sourceLanguage;

        //[ObservableProperty]
        //private string _targetLanguage;

        [ObservableProperty]
        private List<string> _contentLines;

        private readonly MainWindowViewModel MainWindowVM;


        public SentencePanelViewModel()
        {
            _databaseService = new DatabaseService();
            _openAiService = new OpenAIService();

            FavoriteIcon = "/Assets/images/collect.png";//要有预设值否则会出错
            _openAiService = new OpenAIService();

            //SourceLanguage = Properties.Settings.Default.LearningLanguage;
            //TargetLanguage = Properties.Settings.Default.YourLanguage;

             MainWindowVM = Application.Current.MainWindow.DataContext as MainWindowViewModel;
            //SourceLanguage = MainWindow.your
            //TargetLanguage = Properties.Settings.Default.YourLanguage;
            _collectionName = Path.GetFileName(MainWindowVM.VideoFilePath);

            // 订阅设置变更事件，监听语言变化
            Properties.Settings.Default.PropertyChanged += Settings_PropertyChanged;
        }

        private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // 当学习语言或目标语言变更时，重新查询句子的状态与翻译
            if (e.PropertyName == nameof(Properties.Settings.Default.LearningLanguage) ||
                e.PropertyName == nameof(Properties.Settings.Default.YourLanguage))
            {
                // 这里可以使用当前界面上显示的句子文本重新查询
                _ = SentenceTranslateAsync(_sentence);
            }
        }


        [RelayCommand]
        private async Task ToggleFavoriteAsync()
        {
            try
            {
                IsFavorite = !IsFavorite;
                FavoriteIcon = IsFavorite ? "/Assets/images/collect-active.png" : "/Assets/images/collect.png";
                // Toggle favorite status
                //CurrentWordModel.IsFavorite = !CurrentWordModel.IsFavorite;

                // Save to database
                if (IsFavorite)
                {
                    await _databaseService.CollectSentenceAsync(_sentenceModel, _collectionName);
                }
                else
                {
                    await _databaseService.RemoveSentenceAsync(_sentenceModel);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error toggling favorite: {ex.Message}");
            }
        }

        [RelayCommand]
        private void Close()
        {
            IsVisible = false;
        }

        //private void Settings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        //{
        //    SourceLanguage = Properties.Settings.Default.LearningLanguage;
        //    TargetLanguage = Properties.Settings.Default.YourLanguage;
        //}

        public async Task SentenceTranslateAsync(string text)
        {
            _sentence = text;
            var SourceLanguage = MainWindowVM.LearningLanguage;
            var TargetLanguage = MainWindowVM.YourLanguage;

            _sentenceModel = await _databaseService.GetSentenceAsync(text,SourceLanguage, TargetLanguage);
            if (_sentenceModel != null)
            {
                _translationText = _sentenceModel.Translation;
                ContentLines = TextManager.SplitRows(_translationText);

               var result = await _databaseService.CheckSentenceCollectedAsync(_sentenceModel);
                if (result is not null)
                {
                    IsFavorite = true;
                    FavoriteIcon = "/Assets/images/collect-active.png";
                }
                return;
            }


            var sText = TextManager.RemoveHtmlTags(text).Trim(); ;


            if (Properties.Settings.Default.IsUseEchoAPI)
            {
                var responseString = await EchoService.OpenAIRequest(sText, SourceLanguage, TargetLanguage);
                if (string.IsNullOrEmpty(responseString))
                {
                    ErrorMessage = "Translation error";
      
                    return;
                }

                var jObj = Newtonsoft.Json.Linq.JObject.Parse(responseString);

                var result = jObj["resp_content"]?.ToString();
                _translationText = result.Replace("\t"," ");
                ContentLines = TextManager.SplitRows(result);
            }
            else
            {
                var analysis = await _openAiService.AnalyzeSubtitleAsync(sText, SourceLanguage,TargetLanguage);

                //Debug.WriteLine($"afdsfsd {analysis}");

                if (string.IsNullOrEmpty(analysis))
                {
                    ErrorMessage = "Translation error";
                    //Debug.WriteLine("Translationired");
                    return;
                }

                _translationText = analysis.Replace("\t", " ");
                ContentLines = TextManager.SplitRows(analysis);
            }

            if (string.IsNullOrWhiteSpace(_translationText))
            {
                ErrorMessage = "Translation error";
                return;
            }

            _sentenceModel = new SentenceModel();
            _sentenceModel.Sentence = sText;
            _sentenceModel.Translation = _translationText;
            _sentenceModel.SourceLanguageCode = SourceLanguage;
            _sentenceModel.TargetLanguageCode = TargetLanguage;

            await _databaseService.GetOrSaveSentenceAsync(_sentenceModel);

        }



    }

    }
