
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Echo.Handlers;
using Echo.Services;
using Echo.ViewModels;


namespace Echo
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer controlBarTimer;
        private DateTime fullscreenStartTime;
        private Point lastMousePosition;
        private const double SUBTITLE_AREA_HEIGHT = 150;
        private bool isInSubtitleArea = false;

        public MainWindow()
        {
            InitializeComponent();

            // Find the translation container 
            var translationContainer = this.FindName("TranslationContainer") as Canvas;
            var sentenceContainer = this.FindName("SentenceContainer") as Canvas;
            var SubtitletextBlock = this.FindName("SubtitleTextBlock") as TextBlock;

            var vm = DataContext as MainWindowViewModel;
            if (vm != null)
            {
                VideoView.MediaPlayer = vm.MediaPlayer;

                vm.SetTranslationContainer(translationContainer);
                vm.SetSentenceContainer(sentenceContainer);

                var mainSubtitleBlock = this.FindName("SubtitleTextBlock") as TextBlock;
                var prevSubtitleBlock = this.FindName("PreviousSubtitleBlock") as TextBlock;
                var nextSubtitleBlock = this.FindName("NextSubtitleBlock") as TextBlock;
                vm.SetSubtitleBlocks(mainSubtitleBlock, prevSubtitleBlock, nextSubtitleBlock);

                //for word panel position
                vm.VideoViewElement = VideoView;
                vm.SubtitleTextElement = SubtitleTextBlock;
            }

            controlBarTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            controlBarTimer.Tick += (s, e) =>
            {
                if (DataContext is MainWindowViewModel vm)
                {
                    vm.VideoControlVM.IsControlBarVisible = false;
                    vm.MenuBarVM.IsMenuBarVisible = false;
                    controlBarTimer.Stop();
                }
            };

            // Subscribe to fullscreen changes
            vm.FullscreenChanged += (sender, isFullscreen) =>
            {
                if (isFullscreen)
                {
                    fullscreenStartTime = DateTime.Now;
                }
            };
   

            Initialize();

            this.Closed += OnWindowClosed;
        }

        private void Initialize()
        {


        }

        private void OnSubtitleAreaMouseEnter(object sender, MouseEventArgs e)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.OnSubtitleAreaMouseEnter();
            }
        }

        private void OnSubtitleAreaMouseLeave(object sender, MouseEventArgs e)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.OnSubtitleAreaMouseLeave();
            }
        }

        private void OnSubtitleMouseLeftButtonDown(object sender, MouseEventArgs e)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.OnSubtitleAreaMouseLeftButtonDown(e);
            }
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);
            if (DataContext is MainWindowViewModel vm)
            {
                vm.OnPreviewKeyDown(e);
            }



        }

        private void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                Point clickPosition = e.GetPosition(MouseDetectionLayer);
                if(!IsInSubtitleArea(clickPosition))
                {
                    vm.HandleVideoAreaClickCommand.Execute(clickPosition);
                    e.Handled = true;
                }
                   
            }
            this.Focus();
            Keyboard.Focus(this);
        }

        private void OnWindowClosed(object? sender, EventArgs e)
        {
            var vm = DataContext as MainWindowViewModel;
            vm?.DisposeMedia();
        }

        private void OnMouseLeftButtonDown(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                var position = e.GetPosition(MouseDetectionLayer);
                if (IsInSubtitleArea(position) && !string.IsNullOrEmpty(vm.SubtitleText))
                {
                    vm.OnSubtitleAreaMouseLeftButtonDown(e);
                }
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!(DataContext is MainWindowViewModel vm))
                return;

            var currentPosition = e.GetPosition(MouseDetectionLayer);
            var currentTime = DateTime.Now;

            bool isCurrentlyInSubtitleArea = IsInSubtitleArea(currentPosition);

            if (isCurrentlyInSubtitleArea != isInSubtitleArea)
            {
                isInSubtitleArea = isCurrentlyInSubtitleArea;
                if (isInSubtitleArea)
                {
                    vm.OnSubtitleAreaMouseEnter();
                }
                else
                {
                    vm.OnSubtitleAreaMouseLeave();
                }
            }

            // 全屏模式下的控制栏显示逻辑
            if (vm.IsFullScreen)
            {
                var timeSinceFullscreen = (currentTime - fullscreenStartTime).TotalSeconds;

                var distanceMoved = Math.Sqrt(
                    Math.Pow(currentPosition.X - lastMousePosition.X, 2) +
                    Math.Pow(currentPosition.Y - lastMousePosition.Y, 2)
                );

                // 仅在进入全屏1秒内且移动超过10像素时显示VideoControl and MenuBar
                if (timeSinceFullscreen >= 1 && distanceMoved > 10)
                {
                    vm.VideoControlVM.IsControlBarVisible = true;
                    //vm.MenuBarVM.IsMenuBarVisible = true; //会出错?
                    controlBarTimer.Stop();
                    controlBarTimer.Start();
                }
            }

            lastMousePosition = currentPosition;
        }

        private bool IsInSubtitleArea(Point mousePosition)
        {
            double subtitleAreaTop = MouseDetectionLayer.ActualHeight - SUBTITLE_AREA_HEIGHT;
            return mousePosition.Y >= subtitleAreaTop;
        }
        private void AdjustWindowSize(uint videoWidth, uint videoHeight)
        {
            Debug.WriteLine("ok");
        }


    }
}