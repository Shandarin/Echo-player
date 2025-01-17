
using System.Diagnostics;
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
            //if (_textBlock == null) return;
            _textBlock.Inlines.Clear();
            if (string.IsNullOrEmpty(text)) return;

            //Split text into clickable inlines using SubtitleStyleHandler
            var inlines = SubtitleStyleHandler.ProcessSubtitleText(text, OnWordClick);
            foreach (var inline in inlines)
            {
                _textBlock.Inlines.Add(inline);
            }
        }

        private void OnWordClick(string word, Point position)
        {
            _translationService?.ShowTranslation(word, position);
           
        }

        public void SetTextBlock(TextBlock textBlock)
        {
            _textBlock = textBlock;
        }

    }
}