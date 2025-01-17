using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Echo.Services;
using Echo.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using System.Web;
using System.Windows;

namespace Echo.ViewModels
{
    public partial class TranslationPanelViewModel : BaseViewModel
    {
        private readonly OxfordDictService _oxfordService;
        private readonly DatabaseService _databaseService;
        private JObject _wordDetails;
        private JObject _translationDetails;

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

        public TranslationPanelViewModel(OxfordDictService oxfordService,
            DatabaseService databaseService)
        {
            _oxfordService = oxfordService;
            _databaseService = databaseService;
        }

        [RelayCommand]
        private async Task TranslateWordAsync(string word)
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;
                CurrentWord = word;

                // First API call to get head word and other details
                var (headword, details) = await _oxfordService.GetWordDetailsAsync(word);
                if (string.IsNullOrEmpty(headword))
                {
                    ErrorMessage = "Word not found";
                    return;
                }
                _wordDetails = details;

                // Second API call to get translations
                var wordModel = await _oxfordService.GetTranslationsAsync(headword);
                if (wordModel?.Definitions == null || !wordModel.Definitions.Any())
                {
                    ErrorMessage = "Translation not found";
                    return;
                }

                //Translation = wordModel.Translation;
                IsVisible = true;

                //if (IsAutoSaveWord)
                //{
                //    // Save to database
                //    await SaveToDatabase();
                //}
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void Close()
        {
            IsVisible = false;
        }

        private async Task SaveToDatabase()
        {
            // 实现数据库保存逻辑
            // 使用_wordDetails和_translationDetails中的数据
            // 使用DatabaseService保存到数据库
        }
    }
}
