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
using LanguageDetection;

namespace Echo.Handlers
{
    public class SubtitleHandler
    {
        private List<SubtitleItem> _subtitles;
        private readonly DispatcherTimer _timer;
        private readonly Action<string> _updateSubtitleText;
        private long _currentTime;
        private readonly LibVLCSharp.Shared.MediaPlayer _mediaPlayer;
        public string Language;

        private bool _isShowing;
        
        public bool IsLoaded = false;

        public SubtitleItem CurrentSubtitleItem;

        public event EventHandler SubtitlesLoaded;

        public SubtitleHandler(Action<string> updateSubtitleText, LibVLCSharp.Shared.MediaPlayer mediaPlayer, bool isShowing)
        {
            _updateSubtitleText = updateSubtitleText;
            _subtitles = new List<SubtitleItem>();
            _mediaPlayer = mediaPlayer;

            mediaPlayer.TimeChanged += HandleMediaTimeChanged;
            _isShowing = isShowing;
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
                SubtitlesLoaded?.Invoke(this, EventArgs.Empty);
            }

            Language = DetectLanguage();
            //MessageBox.Show(Language);
            IsLoaded = true;
        }

        public void Show()
        {
            _isShowing = true;
            CheckCurrentSubtitle(null,null);
        }

        public void Hide()
        {
            _isShowing = false;
            _updateSubtitleText(string.Empty);
        }


        public void UpdateTime(long currentTimeMs)
            {
                _currentTime = currentTimeMs;
            }

        public void AlwaysShow()
        {

        }

        public bool IsAnySubtitle()
        {
             return CurrentSubtitleItem is not null;
        }


        private void CheckCurrentSubtitle(object sender, EventArgs e)
        {
            if (_subtitles == null || !_subtitles.Any() || !_isShowing)
            {
                _updateSubtitleText(string.Empty);
                //Debug.WriteLine("Empty");
                return;
            }

            //Debug.WriteLine($"_currentTime {_currentTime}");
            var _currentSubtitle = _subtitles.FirstOrDefault(s =>
                s.StartTime <= _currentTime &&
                s.EndTime >= _currentTime);
            //Debug.WriteLine($"currentSubtitle {currentSubtitle}");

            if (_currentSubtitle != null)
            {
                // 移除 ReferenceEquals 检查，直接更新字幕
                CurrentSubtitleItem = _currentSubtitle;
                var text = string.Join("\n", _currentSubtitle.Lines)
                                .Replace("\\N", "\n")
                                .Replace("\\n", "\n");
                _updateSubtitleText(text);
            }
            else
            {
                CurrentSubtitleItem = null;
                _updateSubtitleText(string.Empty);
            }
        }

        private string DetectLanguage()
        {
            if (_subtitles == null || !_subtitles.Any())
            {
                return "No subtitles available";
            }

            // 提取所有字幕文本
            var allText = string.Join(" ", _subtitles.SelectMany(s => s.Lines));

            // 提取100个字符左右的字符串
            var sampleText = allText.Length > 100 ? allText.Substring(0, 100) : allText;

            LanguageDetector detector = new LanguageDetector();
            detector.AddAllLanguages();

            // 检测语言
            var detectedLanguage = detector.Detect(sampleText);

            // 将检测结果转换为两字母代码
            var languageMap = new Dictionary<string, string>
            {
                { "afr", "af" }, { "ara", "ar" }, { "ben", "bn" }, { "bul", "bg" },
                { "ces", "cs" }, { "dan", "da" }, { "deu", "de" }, { "ell", "el" },
                { "eng", "en" }, { "est", "et" }, { "fas", "fa" }, { "fin", "fi" },
                { "fra", "fr" }, { "guj", "gu" }, { "heb", "he" }, { "hin", "hi" },
                { "hrv", "hr" }, { "hun", "hu" }, { "ind", "id" }, { "ita", "it" },
                { "jpn", "ja" }, { "kan", "kn" }, { "kor", "ko" }, { "lav", "lv" },
                { "lit", "lt" }, { "mal", "ml" }, { "mar", "mr" }, { "mkd", "mk" },
                { "nep", "ne" }, { "nld", "nl" }, { "nor", "no" }, { "pan", "pa" },
                { "pol", "pl" }, { "por", "pt" }, { "ron", "ro" }, { "rus", "ru" },
                { "slk", "sk" }, { "slv", "sl" }, { "som", "so" }, { "spa", "es" },
                { "sqi", "sq" }, { "swa", "sw" }, { "swe", "sv" }, { "tam", "ta" },
                { "tel", "te" }, { "tgl", "tl" }, { "tha", "th" }, { "tur", "tr" },
                { "twi", "tw" }, { "ukr", "uk" }, { "urd", "ur" }, { "vie", "vi" },
                { "zho", "zh" }
            };

            return languageMap.TryGetValue(detectedLanguage, out var twoLetterCode) ? twoLetterCode : "unknown";
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