using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Echo.Views;

namespace Echo.Services
{
    public class TranslationService
    {
        //private TranslationPanelView _translationPanel;
        private WordPanelView _wordPanel;
        //private readonly Window _ownerWindow;
        private  Canvas _container;

        public TranslationService()
        {

        }

        public void SetContainer(Canvas container)
        {
            _container = container;
        }

        public void ShowTranslation(string word, Point position)
        {
            if (_wordPanel != null)
            {
                _wordPanel.Close();
                _container.Children.Remove(_wordPanel);
            }

            _wordPanel = new WordPanelView();
            _wordPanel.CloseRequested += (s, e) =>
            {
                _container.Children.Remove(_wordPanel);
                _wordPanel = null;
            };

            _container.Children.Add(_wordPanel);
            _wordPanel.Show(word, position);
        }

        public bool IsClickInsidePanel(Point clickPosition)
        {
            if (_wordPanel == null || !_wordPanel.IsVisible)
                return false;

            // 获取单词面板的位置和大小
            double left = Canvas.GetLeft(_wordPanel);
            double top = Canvas.GetTop(_wordPanel);

            // 计算面板的边界
            Rect panelBounds = new Rect(
                left,
                top,
                _wordPanel.ActualWidth,
                _wordPanel.ActualHeight);

            // 检查点击位置是否在面板内
            return panelBounds.Contains(clickPosition);
        }

        public void CloseTranslation()
        {
            _wordPanel?.Close();
            if (_wordPanel != null)
            {
                _container.Children.Remove(_wordPanel);
                _wordPanel = null;
            }
        }
    }
}