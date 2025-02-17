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
        private const int MAX_SENTENCE_LENGTH = 200;

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
                //var text = string.Join("\n", _currentSubtitle.Lines)
                //                .Replace("\\N", "\n")
                //                .Replace("\\n", "\n");

                // 去掉换行符
                var text = string.Join(" ", _currentSubtitle.Lines)
                                .Replace("\\N", " ")
                                .Replace("\\n", " ");

                // 定义句子终止符号
                var sentenceEndings = new[] { ".", "。", "?", "？", "!", "！" };
                // 合并属于同一句的字幕项
                var nextSubtitle = _subtitles.FirstOrDefault(s =>
                    s.StartTime == _currentSubtitle.EndTime &&
                    !sentenceEndings.Any(e => text.EndsWith(e)));

                while (nextSubtitle != null)
                {
                    // 合并文本
                    text += " " + string.Join(" ", nextSubtitle.Lines)
                                        .Replace("\\N", " ")
                                        .Replace("\\n", " ");

                    // 更新结束时间
                    _currentSubtitle.EndTime = nextSubtitle.EndTime;
                    _subtitles.Remove(nextSubtitle);

                    // 检查下一个字幕项
                    nextSubtitle = _subtitles.FirstOrDefault(s =>
                        s.StartTime == _currentSubtitle.EndTime &&
                        !sentenceEndings.Any(e => text.EndsWith(e)));
                }

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

        public string GetFullSentence(bool usePrevious = false)
        {
            if (CurrentSubtitleItem == null)
                return string.Empty;

            // 选择基准字幕项：如果要求上一条字幕且存在，则取上一项；否则使用当前字幕
            int baseIndex = _subtitles.IndexOf(CurrentSubtitleItem);
            if (usePrevious && baseIndex > 0)
            {
                baseIndex--;
            }

            // 获取基准字幕的文本（替换换行符）
            string baseText = GetSubtitleItemText(_subtitles[baseIndex]).Trim();
            // 定义句子终止符（注意省略号不算）
            char[] sentenceTerminators = new char[] { '.', '。', '?', '？', '!', '！' };

            // 向上合并：从基准字幕向前合并
            int upwardIndex = baseIndex;
            string mergedText = baseText;
            while (upwardIndex > 0)
            {
                // 获取前一条字幕的文本
                string prevText = GetSubtitleItemText(_subtitles[upwardIndex - 1]).Trim();
                // 如果前一条字幕以终止符结尾，则认为完整，不合并
                if (EndsWithSentenceTerminator(prevText))
                {
                    break;
                }
                // 如果前一条字幕中包含终止符（但不在末尾），则只取最后一个终止符之后的片段合并，然后停止向上合并
                else if (prevText.IndexOfAny(sentenceTerminators) >= 0)
                {
                    string fragment = ExtractRelevantSegmentUpward(prevText, sentenceTerminators);
                    if (!string.IsNullOrEmpty(fragment))
                        mergedText = fragment + " " + mergedText;
                    break;
                }
                else
                {
                    // 没有任何终止符，则全部合并
                    mergedText = prevText + " " + mergedText;
                    upwardIndex--;
                }
            }

            mergedText = mergedText.Trim();

            // 向后合并：仅当合并后的文本既不以终止符也不以休止符结尾时，才继续向后查找
            int downwardIndex = baseIndex;
            string totalMergedText = mergedText;
            if (!EndsWithSentenceTerminator(totalMergedText))
            {
                while (downwardIndex < _subtitles.Count - 1)
                {
                    string nextText = GetSubtitleItemText(_subtitles[downwardIndex + 1]).Trim();
                    if (nextText.IndexOfAny(sentenceTerminators) >= 0)
                    {
                        // 如果存在终止符，则只取从开始到第一个终止符（包含终止符）的部分
                        string fragment = ExtractRelevantSegmentDownward(nextText, sentenceTerminators);
                        if (!string.IsNullOrEmpty(fragment))
                            totalMergedText = totalMergedText + " " + fragment;
                        break;
                    }
                    else
                    {
                        totalMergedText = totalMergedText + " " + nextText;
                        downwardIndex++;
                    }
                }
            }

            totalMergedText = totalMergedText.Trim();

            // 拆分合并后的文本为完整句子列表，并组合所有完整句子
            var sentences = SplitIntoSentences(totalMergedText, sentenceTerminators);
            if (sentences.Count > 0)
            {
                totalMergedText = string.Join(" ", sentences);
            }

            // 限制整体长度
            if (totalMergedText.Length > MAX_SENTENCE_LENGTH)
                totalMergedText = totalMergedText.Substring(0, MAX_SENTENCE_LENGTH) + "...";

            return totalMergedText;
        }

        private string GetSubtitleItemText(SubtitleItem item)
        {
            return string.Join(" ", item.Lines)
                         .Replace("\\N", " ")
                         .Replace("\\n", " ");
        }

        private bool EndsWithSentenceTerminator(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;
            if (text.EndsWith("...") || text.EndsWith("……"))
                return false;
            char lastChar = text[text.Length - 1];
            return new char[] { '.', '。', '?', '？', '!', '！' ,'-',']'}.Contains(lastChar);
        }

        private string ExtractRelevantSegmentUpward(string text, char[] sentenceTerminators)
        {
            int lastPos = -1;
            foreach (char term in sentenceTerminators)
            {
                int pos = text.LastIndexOf(term);
                if (pos > lastPos)
                    lastPos = pos;
            }
            if (lastPos >= 0 && lastPos < text.Length - 1)
            {
                // 返回终止符后面的片段
                return text.Substring(lastPos + 1).Trim();
            }
            return text;
        }

        private string ExtractRelevantSegmentDownward(string text, char[] sentenceTerminators)
        {
            int firstPos = -1;
            foreach (char term in sentenceTerminators)
            {
                int pos = text.IndexOf(term);
                if (pos >= 0)
                {
                    if (firstPos == -1 || pos < firstPos)
                        firstPos = pos;
                }
            }
            if (firstPos >= 0)
            {
                // 返回从开始到终止符的部分（包含终止符）
                return text.Substring(0, firstPos + 1).Trim();
            }
            return text;
        }


        private List<string> SplitIntoSentences(string text, char[] sentenceTerminators)
        {
            var sentences = new List<string>();
            if (string.IsNullOrWhiteSpace(text))
                return sentences;

            StringBuilder currentSentence = new StringBuilder();
            for (int i = 0; i < text.Length; i++)
            {
                currentSentence.Append(text[i]);
                if (sentenceTerminators.Contains(text[i]))
                {
                    // 判断是否为省略号（假定连续三个点或“……”为省略号）
                    bool isEllipsis = false;
                    if (text[i] == '.')
                    {
                        if (i >= 2 && text.Substring(i - 2, 3) == "...")
                            isEllipsis = true;
                    }
                    if (!isEllipsis)
                    {
                        sentences.Add(currentSentence.ToString().Trim());
                        currentSentence.Clear();
                    }
                }
            }
            if (currentSentence.Length > 0)
                sentences.Add(currentSentence.ToString().Trim());
            return sentences;
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