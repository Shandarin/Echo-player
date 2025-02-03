
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Echo.Handlers;
using Echo.Services;
using Echo.ViewModels;
using Echo.Views;
using LibVLCSharp.Shared;


namespace Echo
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer _controlBarTimer;
        //private DateTime _fullscreenStartTime;
        private DateTime _lastMouseMoveTime;
        private Point _lastMousePosition;

        private double _subtitle_area_height;

        private double _subtitle_area_width;

        private bool _isInSubtitleArea = false;

        public MainWindow()
        {
            InitializeComponent();

            var SubtitletextBlock = this.FindName("SubtitleTextBlock") as TextBlock;

            if (DataContext is MainWindowViewModel vm)
            {
                VideoView.MediaPlayer = vm.MediaPlayer;

                if (FindName("TranslationContainer") is Canvas translationCanvas)
                {
                    vm.SetTranslationContainer(translationCanvas);
                }
                if (FindName("SentenceContainer") is Canvas sentenceCanvas)
                {
                    vm.SetSentenceContainer(sentenceCanvas);
                }

                if (FindName("SubtitleTextBlock") is TextBlock mainSubtitleBlock &&
                    FindName("PreviousSubtitleBlock") is TextBlock prevSubtitleBlock &&
                    FindName("NextSubtitleBlock") is TextBlock nextSubtitleBlock)
                {
                    vm.SetSubtitleBlocks(mainSubtitleBlock, prevSubtitleBlock, nextSubtitleBlock);
                    vm.SubtitleTextElement = mainSubtitleBlock;

                    _subtitle_area_height = mainSubtitleBlock.Height + 10;

                    mainSubtitleBlock.LayoutUpdated += (s, e) =>
                    {
                        if (mainSubtitleBlock.ActualHeight != _subtitle_area_height)
                        {
                            _subtitle_area_height = mainSubtitleBlock.ActualHeight +10;
                        }

                        if(mainSubtitleBlock.ActualWidth != _subtitle_area_width)
                        {
                            _subtitle_area_width = mainSubtitleBlock.ActualWidth;
                        }
                    };
                }

                if (FindName("VideoControlView") is VideoControlView videoControlView)
                {
                    videoControlView.VideoControlMouseMoved += () =>
                    {
                        if (vm.IsFullScreen)
                        {
                            _lastMouseMoveTime = DateTime.Now;
                        }

                    };
                }

                vm.VideoViewElement = VideoView;

                vm.ToggleFullScreenRequested += HandleToggleFullScreenRequested;
            }

            // 控制栏自动隐藏的计时器
            _controlBarTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            _controlBarTimer.Tick += OnControlBarTimerTick;
            _controlBarTimer.Start();

            _lastMouseMoveTime = DateTime.Now;

            this.Closed += OnWindowClosed;
        }

        #region Event Handlers

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            if (DataContext is not MainWindowViewModel vm) return;

            var clickPosition = e.GetPosition(MouseDetectionLayer);
            bool isClickOnSubtitle = IsInSubtitleArea(clickPosition);

            if (isClickOnSubtitle && !string.IsNullOrEmpty(vm.SubtitleText))
            {
                // 字幕区点击
                vm.OnSubtitleAreaMouseLeftButtonDown(e);
            }
            else
            {
                // 视频区点击
                vm.HandleVideoAreaClickCommand.Execute(clickPosition);
            }

            // 让窗口获取焦点，用于键盘控制
            Focus();
            Keyboard.Focus(this);

            // 阻止进一步冒泡（如果需要）
            e.Handled = true;
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);
            if (DataContext is MainWindowViewModel vm)
            {
                vm.OnPreviewKeyDown(e);
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (DataContext is not MainWindowViewModel vm) return;

            var currentPosition = e.GetPosition(MouseDetectionLayer);

            _lastMouseMoveTime = DateTime.Now;

            // 判断是否进入或离开字幕区
            bool isCurrentlyInSubtitleArea = IsInSubtitleArea(currentPosition);
            if (isCurrentlyInSubtitleArea != _isInSubtitleArea)
            {
                _isInSubtitleArea = isCurrentlyInSubtitleArea;
                if (_isInSubtitleArea)
                {
                    vm.OnSubtitleAreaMouseEnter();
                }
                else
                {
                    vm.OnSubtitleAreaMouseLeave();
                }
            }


            //if (isCurrentlyInSubtitleArea != _isInSubtitleArea)
            //{
            //    _isInSubtitleArea = isCurrentlyInSubtitleArea;
            //    if (_isInSubtitleArea)
            //    {
            //        vm.OnSubtitleAreaMouseEnter();
            //    }
            //    else
            //    {
            //        vm.OnSubtitleAreaMouseLeave();
            //    }
            //}

            // 3) 全屏模式下，若移动距离大于一定阈值则显示控制栏
            if (vm.IsFullScreen)
            {
                double distanceMoved = (currentPosition - _lastMousePosition).Length;
                
                if (distanceMoved > 50)
                {
                    // 显示控制栏
                    if (!vm.VideoControlVM.IsControlBarVisible)
                    {
                        vm.VideoControlVM.IsControlBarVisible = true;
                        // 如果你不想同时隐藏或显示 MenuBar，可以去掉这行
                        // vm.MenuBarVM.IsMenuBarVisible = true;
                    }
                }
            }

            // 记录最后一次鼠标位置
            _lastMousePosition = currentPosition;
        }


        private void OnControlBarTimerTick(object? sender, EventArgs e)
        {
            if (DataContext is not MainWindowViewModel vm) return;

            // 仅在全屏模式下检查是否要隐藏
            if (vm.IsFullScreen)
            {
                double secondsSinceLastMove = (DateTime.Now - _lastMouseMoveTime).TotalSeconds;
                //Debug.WriteLine(secondsSinceLastMove);
                if (secondsSinceLastMove >= 3)
                {
                
                    // 鼠标已经停止 3 秒，隐藏控制栏
                    vm.VideoControlVM.IsControlBarVisible = false;
                    // vm.MenuBarVM.IsMenuBarVisible = false;//会出错
                }
            }
        }

        public void HandleToggleFullScreenRequested(object sender, bool isFullScreen)
        {
            if(isFullScreen)
            {
                this.SizeToContent = SizeToContent.Manual;
                this.WindowStyle = WindowStyle.None;
                this.ResizeMode = ResizeMode.NoResize;
                this.WindowState = WindowState.Maximized;
                this.Topmost = true;
            }
            else
            {
                this.Topmost = false;
                this.WindowStyle = WindowStyle.SingleBorderWindow;
                this.SizeToContent = SizeToContent.WidthAndHeight;
                this.ResizeMode = ResizeMode.CanResize;
                this.WindowState = WindowState.Normal;
            }
            
        }

        private void OnWindowClosed(object? sender, EventArgs e)
        {
            var vm = DataContext as MainWindowViewModel;
            vm?.Closed();
        }
        #endregion

        private void OnSubtitleAreaMouseEnter(object sender, MouseEventArgs e)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.OnSubtitleAreaMouseEnter();
            }
        }


        private void OnSubtitleMouseLeftButtonDown(object sender, MouseEventArgs e)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.OnSubtitleAreaMouseLeftButtonDown(e);
            }
        }

        private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.OnWindowSizeChanged(e.NewSize);
            }
        }

        private bool IsInSubtitleArea(Point mousePosition)
        {

            double subtitleAreaTop = MouseDetectionLayer.ActualHeight - _subtitle_area_height;
            double subtitleAreaBottom = MouseDetectionLayer.ActualHeight - 10;

            if (SubtitleTextBlock.Visibility == Visibility.Collapsed)
            {
                _subtitle_area_width = 300;
                _subtitle_area_height = 100;
            }

            double subtitleAreaLeft = MouseDetectionLayer.ActualWidth / 2 - _subtitle_area_width / 2;
            double subtitleAreaRight = subtitleAreaLeft + _subtitle_area_width;

            bool isTop = mousePosition.Y >= subtitleAreaTop;
            bool isBottom = mousePosition.Y <= subtitleAreaBottom;
            bool isLeft = mousePosition.X > subtitleAreaLeft;
            bool isRight = mousePosition.X < subtitleAreaRight;

            return isTop && isBottom && isLeft && isRight;

        }

        private void OnDragOver(object sender, DragEventArgs e)
        {
            // Check if the dragged data contains file(s)
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }
        private async void OnFileDrop(object sender, DragEventArgs e)
        {
            try
            {
                // 保存当前工作目录
                string originalDirectory = Environment.CurrentDirectory;

                try
                {
                    // 设置正确的工作目录
                    string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    Environment.CurrentDirectory = baseDirectory;

                    Debug.WriteLine($"Original Directory: {originalDirectory}");
                    Debug.WriteLine($"Set Directory to: {baseDirectory}");

                    if (e.Data.GetDataPresent(DataFormats.FileDrop))
                    {
                        string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                        if (files.Length > 0)
                        {
                            string filePath = Path.GetFullPath(files[0]);
                            if (DataContext is MainWindowViewModel vm)
                            {
                                await vm.HandleFileOpenAsync(filePath);
                            }
                        }
                    }
                }
                finally
                {
                    // 恢复原始工作目录
                    Environment.CurrentDirectory = originalDirectory;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OnFileDrop error: {ex.Message}");
                Debug.WriteLine($"Current Directory: {Environment.CurrentDirectory}");
                MessageBox.Show($"打开文件时发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}