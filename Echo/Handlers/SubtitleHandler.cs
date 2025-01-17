using SubtitlesParser.Classes;
using System.Diagnostics;
using System.IO;
using System.Windows.Threading;
using LibVLCSharp.Shared;
using System.Windows.Media;

namespace Echo.Handlers
{
    public class SubtitleHandler
    {
        private List<SubtitleItem> _subtitles;
        private readonly DispatcherTimer _timer;
        private readonly Action<string> _updateSubtitleText;
        private long _currentTime;
        private readonly LibVLCSharp.Shared.MediaPlayer _mediaPlayer;

        public event Action<List<SubtitleItem>> SubtitlesLoaded;

        public SubtitleHandler(Action<string> updateSubtitleText, LibVLCSharp.Shared.MediaPlayer mediaPlayer)
        {
            _updateSubtitleText = updateSubtitleText;
            _subtitles = new List<SubtitleItem>();
            _mediaPlayer = mediaPlayer;

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _timer.Tick += CheckCurrentSubtitle;
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
        }

        public void Start()
        {
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
            _updateSubtitleText(string.Empty);
        }

        //triggered in MainWindowViewModel.cs by :
        //_mediaPlayer.TimeChanged += (sender, e) =>
        //    {
        //        _subtitleHandler.UpdateTime(e.Time);
        //    };
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
                var text = string.Join("\n", currentSubtitle.Lines);
                // 处理字幕样式标签
                text = text.Replace("\\N", "\n"); // 处理换行符
                text = text.Replace("\\n", "\n");
                _updateSubtitleText(text);
            }
            else
            {
                _updateSubtitleText(string.Empty);
            }
        }

        public void Dispose()
        {
            _timer.Stop();
        }
    }
}