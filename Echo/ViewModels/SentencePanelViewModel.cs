using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Echo.Managers;
using Echo.Models;
using Echo.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        //private MessageManager _messageManager = new()

        [ObservableProperty]
        private bool _isFavorite;

        [ObservableProperty]
        private string _contentText;

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

        [ObservableProperty]
        private string _sourceLanguage;

        [ObservableProperty]
        private string _targetLanguage;

        [ObservableProperty]
        private List<string> _contentLines;


        public SentencePanelViewModel()
        {
            _databaseService = new DatabaseService();
            _openAiService = new OpenAIService();

            FavoriteIcon = "/Assets/images/collect.png";//要有预设值否则会出错
            _openAiService = new OpenAIService();

            SourceLanguage = Properties.Settings.Default.LearningLanguage;
            TargetLanguage = Properties.Settings.Default.YourLanguage;

            Properties.Settings.Default.PropertyChanged += Settings_PropertyChanged;
            ContentLines = new List<string>
        {
            "部落已经发声了。",
            "1. \"The tribe\"",
            "    ○ \"Tribe\" 指的是一个社会群体，通常是基于共同的文化、语言或历史背景。",
            "    ○ 在某些情况下，\"tribe\" 也可以用于比喻，指代一个志同道合的人群或团体。",
            "2. \"Has spoken\"",
            "    ○ \"Has spoken\" 是现在完成时，表示过去的某个动作对现在产生了影响。",
            "    ○ \"Spoken\" 是 \"speak\" 的过去分词，暗示一种表达、决策或表态的行为。",
            "3. 整句的结构",
            "    ○ 句子简洁而有力，强调部落的集体决策或意见，通常会在某种情况下用来表示一种权威或共识。"
        };
        }


        [RelayCommand]
        private async Task ToggleFavoriteAsync()
        {

            if (string.IsNullOrWhiteSpace(ContentText))
            {
                MessageBox.Show("No sentence selected to toggle favorite status.");
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
                    await _databaseService.CollectionSentenceAsync(Sentence, ContentText, SourceLanguage, TargetLanguage);
                }
                else
                {
                    await _databaseService.RemoveSentenceAsync(Sentence, SourceLanguage, TargetLanguage);
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

        private void Settings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            SourceLanguage = Properties.Settings.Default.LearningLanguage;
            TargetLanguage = Properties.Settings.Default.YourLanguage;
        }

        public async Task SentenceTranslateAsync(string text)
        {
            var sText = TextManager.RemoveHtmlTags(text);


            if (Properties.Settings.Default.IsEchoAPIEnabled)
            {
                var responseString = await EchoService.OpenAIRequest(sText, SourceLanguage, TargetLanguage);
                var jObj = Newtonsoft.Json.Linq.JObject.Parse(responseString);

                var result = jObj["resp_content"]?.ToString();

                ContentLines = TextManager.SplitRows(result);
            }
            else
            {
                var analysis = await _openAiService.AnalyzeSubtitleAsync(sText, TargetLanguage);

                ContentLines = TextManager.SplitRows(analysis);
            }

        }



    }

    }
