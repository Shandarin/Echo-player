using Echo.Services;
using Echo.ViewModels;
using LibVLCSharp.WPF;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Echo.Views
{
    /// <summary>
    /// wordpanel.xaml 的交互逻辑
    /// </summary>
    public partial class WordPanelView : UserControl
    {
        private Point _dragStartPoint;
        private bool _isDragging = false;

        public WordPanelView()
        {
            InitializeComponent();

            // 初始化ViewModel并设置DataContext
            var oxfordService = new OxfordDictService();
            var databaseService = new DatabaseService();
            DataContext = new WordPanelViewModel(oxfordService, databaseService);
        }

        public void Show(string word, Point position)
        {
            // 设置为可见以便获取大小
            this.Visibility = Visibility.Visible;
            this.UpdateLayout();

            if (Application.Current.MainWindow.DataContext is MainWindowViewModel mainViewModel)
            {
                var videoView = mainViewModel.VideoViewElement as FrameworkElement;
                var subtitleTextBlock = mainViewModel.SubtitleTextElement as FrameworkElement;

                if (videoView == null || subtitleTextBlock == null)
                {
                    Debug.WriteLine("VideoView or SubtitleTextBlock not found.");
                    return;
                }

                // 转换坐标系
                Point relativePosition = subtitleTextBlock.TranslatePoint(position, videoView);

                // 默认显示在鼠标上方
                double newX = relativePosition.X;
                double newY = relativePosition.Y - this.ActualHeight - 50;

                // 确保面板不超出边界
                newX = Math.Max(0, Math.Min(newX, videoView.ActualWidth - this.ActualWidth));
                newY = newY < 0 ? relativePosition.Y + 10 : newY;

                // 设置面板位置
                Canvas.SetLeft(this, newX);
                Canvas.SetTop(this, newY);
                if (DataContext is WordPanelViewModel vm)
                {
                    vm.TranslateWordCommand.ExecuteAsync(word);
                }
            }
        }

        private void OnFavoriteClick(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Favorite clicked");
        }

        public void Close()
        {
            this.Visibility = Visibility.Collapsed;
        }

        public event EventHandler CloseRequested;
    }
}
