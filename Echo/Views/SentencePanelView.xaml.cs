using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Echo.Managers;
using Echo.Services;
using Echo.ViewModels;

namespace Echo.Views
{
    public partial class SentencePanelView : UserControl
    {
        private Point _dragStartPoint;
        private bool _isDragging = false;
        
        private string translateText;

        public event EventHandler CloseRequested;

        public SentencePanelView()
        {
            InitializeComponent();
            DataContext = new SentencePanelViewModel();
        }

        public async void Show(string text, Point position)
        {
            // 设置为可见并初始化位置
            this.Visibility = Visibility.Visible;
            this.UpdateLayout();

            // 保存鼠标位置，供后续调整位置时使用
            PositionPanelAboveMouse(position);

            // 执行分析
            await AnalyzeSubtitle(text, position);
        }

        private void PositionPanelAboveMouse(Point position)
        {
            // 默认显示在鼠标上方
            double newX = position.X;
            double newY = position.Y - this.ActualHeight - 10; // 上方偏移10像素

            var container = Parent as FrameworkElement;

            if (container != null)
            {
                // 确保不会超出左边界
                if (newX < 0)
                    newX = 0;

                // 确保不会超出右边界
                if (newX + this.ActualWidth > container.ActualWidth)
                    newX = container.ActualWidth - this.ActualWidth;

                // 如果上方空间不足，则显示在鼠标下方
                if (newY < 0)
                    newY = position.Y + 10;

                // 确保不会超出下边界
                if (newY + this.ActualHeight > container.ActualHeight)
                    newY = container.ActualHeight - this.ActualHeight;
            }

            // 设置面板位置
            Canvas.SetLeft(this, newX);
            Canvas.SetTop(this, newY);
        }

        private void UpdatePanelPosition(Point position)
        {
            this.UpdateLayout(); // 确保重新计算大小
            PositionPanelAboveMouse(position); // 根据最新大小重新调整位置
        }

        private async System.Threading.Tasks.Task AnalyzeSubtitle(string text, Point position)
        {
            
            try
            {
                ShowLoading(true);
                ///ContentText.Text = "Analyzing...";
                UpdatePanelPosition(position); // 确保 "Analyzing..." 状态下位置正确

                //var analysis = await _openAiService.AnalyzeSubtitleAsync(text);
                

                if (DataContext is SentencePanelViewModel vm)
                {
                    text = TextManager.RemoveHtmlTags(text);
                    vm.Sentence = text;
                    await vm.SentenceTranslateAsync(text);
                    //analysis = await vm.AnalyzeSubtitleAsync(text);
                }
                

                //ContentText.Text = translateText;

                // 更新面板位置，确保完整回复框也在鼠标上方
                UpdatePanelPosition(position);
            }
            catch (Exception ex)
            {
                ///ContentText.Text = $"Analysis failed: {ex.Message}";
                UpdatePanelPosition(position);
            }
            finally
            {
                ShowLoading(false);
            }
        }

        private void ShowLoading(bool show)
        {
            LoadingGrid.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OnTitleBarMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _isDragging = true;
                _dragStartPoint = e.GetPosition(Parent as UIElement);
                //TitleBar.CaptureMouse();

                _dragStartPoint = new Point(
                    _dragStartPoint.X - Canvas.GetLeft(this),
                    _dragStartPoint.Y - Canvas.GetTop(this)
                );
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_isDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                var currentPos = e.GetPosition(Parent as UIElement);

                double newX = currentPos.X - _dragStartPoint.X;
                double newY = currentPos.Y - _dragStartPoint.Y;

                var container = Parent as FrameworkElement;
                if (container != null)
                {
                    newX = Math.Max(0, Math.Min(newX, container.ActualWidth - ActualWidth));
                    newY = Math.Max(0, Math.Min(newY, container.ActualHeight - ActualHeight));
                }

                Canvas.SetLeft(this, newX);
                Canvas.SetTop(this, newY);
            }
        }

        public bool IsClickInsidePanel(Point clickPos)
        {
            // 获取面板的左、上位置
            double panelLeft = Canvas.GetLeft(this);
            double panelTop = Canvas.GetTop(this);

            // 面板的右、下位置
            double panelRight = panelLeft + this.ActualWidth;
            double panelBottom = panelTop + this.ActualHeight;

            // 检查点击位置是否在面板的边界内
            return clickPos.X >= panelLeft && clickPos.X <= panelRight &&
                   clickPos.Y >= panelTop && clickPos.Y <= panelBottom;
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);
            if (_isDragging)
            {
                _isDragging = false;
                //TitleBar.ReleaseMouseCapture();
            }
        }

        private void OnCloseButtonClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public void Close()
        {
            this.Visibility = Visibility.Collapsed;
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}