using SubtitlesParser.Classes;
using System.Diagnostics;
using System.IO;
using System.Windows.Threading;
using LibVLCSharp.Shared;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;
using Echo.ViewModels;
using System.Windows;

namespace Echo.Handlers
{
    public class SubtitleHandler
    {
        private List<SubtitleItem> _subtitles;
        private readonly DispatcherTimer _timer;
        private readonly Action<string> _updateSubtitleText;
        private long _currentTime;
        private readonly LibVLCSharp.Shared.MediaPlayer _mediaPlayer;
        
        public bool IsLoaded = false;

        public SubtitleItem CurrentSubtitleItem;

        public event Action<List<SubtitleItem>> SubtitlesLoaded;

        public SubtitleHandler(Action<string> updateSubtitleText, LibVLCSharp.Shared.MediaPlayer mediaPlayer)
        {
            _updateSubtitleText = updateSubtitleText;
            _subtitles = new List<SubtitleItem>();
            _mediaPlayer = mediaPlayer;

            mediaPlayer.TimeChanged += HandleMediaTimeChanged;
        }

        private void HandleMediaTimeChanged(object? sender, MediaPlayerTimeChangedEventArgs e)
        {
            CheckCurrentSubtitle(sender,e);
            UpdateTime(e.Time);
        }

        public void LoadSubtitle(string subtitlePath)
        {
            if (!File.Exists(subtitlePath)) return;

            var parser = new SubtitlesParser.Classes.Parsers.SubParser();
            using (var fileStream = File.OpenRead(subtitlePath))
            {
                _subtitles = parser.ParseStream(fileStream);
                SubtitlesLoaded?.Invoke(_subtitles);
            }
            IsLoaded = true;
        }

        public void Show()
        {
            CheckCurrentSubtitle(null,null);
        }

        public void Hide()
        {
            _updateSubtitleText(string.Empty);
        }


        public void UpdateTime(long currentTimeMs)
            {
                _currentTime = currentTimeMs;
            }


        private void CheckCurrentSubtitle(object sender, EventArgs e)
        {
       
            if (_subtitles == null || !_subtitles.Any())
            {
                _updateSubtitleText(string.Empty);
                return;
            }

            var currentSubtitle = _subtitles.FirstOrDefault(s =>
                s.StartTime <= _currentTime &&
                s.EndTime >= _currentTime);

            if (currentSubtitle != null)
            {
                if (!ReferenceEquals(CurrentSubtitleItem, currentSubtitle))
                {
                    CurrentSubtitleItem = currentSubtitle;
                    var text = string.Join("\n", currentSubtitle.Lines)
                                     .Replace("\\N", "\n")
                                     .Replace("\\n", "\n");
                    _updateSubtitleText(text);
                }
            }
            else
            {
                if (CurrentSubtitleItem != null)
                {
                    CurrentSubtitleItem = null;
                    _updateSubtitleText(string.Empty);
                }
            }
        }

        public void Dispose()
        {
            IsLoaded = false;

            _subtitles?.Clear();
            _subtitles = null;

            SubtitlesLoaded = null;
            _updateSubtitleText(string.Empty);

        }
    }
}