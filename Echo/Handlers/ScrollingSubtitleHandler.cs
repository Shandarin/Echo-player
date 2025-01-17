using SubtitlesParser.Classes;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using System.Windows.Threading;

namespace Echo.Handlers
{
    public class ScrollingSubtitleHandler
    {
        private List<SubtitleItem> _subtitles;
        private bool _isScrollingEnabled;
        private TextBlock _mainSubtitleBlock;
        private TextBlock _prevSubtitleBlock;
        private TextBlock _nextSubtitleBlock;
        private Dispatcher _dispatcher;

        public ScrollingSubtitleHandler()
        {
            _subtitles = new List<SubtitleItem>();
            _isScrollingEnabled = false;
        }

        public void Initialize(TextBlock mainBlock, TextBlock prevBlock, TextBlock nextBlock)
        {
            _mainSubtitleBlock = mainBlock;
            _prevSubtitleBlock = prevBlock;
            _nextSubtitleBlock = nextBlock;
            _dispatcher = mainBlock.Dispatcher;

            _dispatcher.Invoke(() =>
            {
                if (_prevSubtitleBlock != null)
                {
                    _prevSubtitleBlock.Opacity = 0.5;
                    _prevSubtitleBlock.Visibility = Visibility.Collapsed;
                }

                if (_nextSubtitleBlock != null)
                {
                    _nextSubtitleBlock.Opacity = 0.5;
                    _nextSubtitleBlock.Visibility = Visibility.Collapsed;
                }
            });
        }

        public void SetSubtitles(List<SubtitleItem> subtitles)
        {
            _subtitles = subtitles ?? new List<SubtitleItem>();
        }

        public void EnableScrolling(bool enable)
        {
            _isScrollingEnabled = enable;

            _dispatcher.InvokeAsync(() =>
            {
                if (_prevSubtitleBlock != null)
                {
                    _prevSubtitleBlock.Visibility = enable ? Visibility.Visible : Visibility.Collapsed;
                    _prevSubtitleBlock.Text = string.Empty;
                }

                if (_nextSubtitleBlock != null)
                {
                    _nextSubtitleBlock.Visibility = enable ? Visibility.Visible : Visibility.Collapsed;
                    _nextSubtitleBlock.Text = string.Empty;
                }
            });
        }

        public void UpdateSubtitles(long currentTimeMs)
        {
            if (!_isScrollingEnabled || _subtitles == null || !_subtitles.Any() ||
                _prevSubtitleBlock == null || _nextSubtitleBlock == null)
            {
                ClearSubtitles();
                return;
            }

            var currentIndex = _subtitles.FindIndex(s =>
                s.StartTime <= currentTimeMs &&
                s.EndTime >= currentTimeMs);

            _dispatcher.InvokeAsync(() =>
            {
                if (currentIndex != -1)
                {
                    // Update previous subtitle
                    if (currentIndex > 0)
                    {
                        var prevSubtitle = _subtitles[currentIndex - 1];
                        _prevSubtitleBlock.Text = string.Join("\n", prevSubtitle.Lines);
                    }
                    else
                    {
                        _prevSubtitleBlock.Text = string.Empty;
                    }

                    // Update next subtitle
                    if (currentIndex < _subtitles.Count - 1)
                    {
                        var nextSubtitle = _subtitles[currentIndex + 1];
                        _nextSubtitleBlock.Text = string.Join("\n", nextSubtitle.Lines);
                    }
                    else
                    {
                        _nextSubtitleBlock.Text = string.Empty;
                    }
                }
                else
                {
                    ClearSubtitles();
                }
            });
        }

        private void ClearSubtitles()
        {
            if (_dispatcher == null) return;

            _dispatcher.InvokeAsync(() =>
            {
                if (_prevSubtitleBlock != null)
                {
                    _prevSubtitleBlock.Text = string.Empty;
                }
                if (_nextSubtitleBlock != null)
                {
                    _nextSubtitleBlock.Text = string.Empty;
                }
            });
        }
    }
}