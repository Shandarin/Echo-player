
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using Echo.Services;

namespace Echo.Handlers
{
    public class WordClickHandler
    {
        private TextBlock _textBlock;
        //private ToolTip _currentTooltip;

        private readonly TranslationService _translationService;

        public WordClickHandler(TranslationService translationService)
        {

            //_textBlock = textBlock;
            _translationService = translationService;
        }

        public void SetText(string text)
        {
            if (_textBlock == null) return;

            // 确保在 UI 线程上操作 _textBlock
            _textBlock.Dispatcher.Invoke(() =>
            {
                _textBlock.Inlines.Clear();
                if (string.IsNullOrEmpty(text)) return;

                // Split text into clickable parts and add to Inlines
                var inlines = SubtitleStyleHandler.ProcessSubtitleText(text, OnWordClick);
                foreach (var inline in inlines)
                {
                    _textBlock.Inlines.Add(inline);
                }
            });
        }

        private void OnWordClick(string word, Point position)
        {
            word = TrimAllCharacters(word);
            Debug.WriteLine($"work clicked!: {word}");
            _translationService?.ShowTranslation(word, position);
            
        }

        private static string TrimAllCharacters(string word)
        {
            word = word.ToLower();
            //移除尾部所有符号空格数字
            return Regex.Replace(word, @"[\p{N}\p{P}\p{S}\s]+$", string.Empty);
        }

        public void SetTextBlock(TextBlock textBlock)
        {
            _textBlock = textBlock;
        }

    }
}