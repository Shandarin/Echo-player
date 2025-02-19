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

        private char[] sentenceTerminators = new char[] { '.', '。', '?', '？', '!', '！', '-', ']' };

        private List<SubtitleItem> _subtitles;
        private readonly DispatcherTimer _timer;
        private readonly Action<string,string> _updateSubtitleText;
        private long _currentTime;
        private readonly LibVLCSharp.Shared.MediaPlayer _mediaPlayer;
        public string Language;

        private bool _isShowing;
        
        public bool IsLoaded = false;

        public SubtitleItem CurrentSubtitleItem;

        public event EventHandler SubtitlesLoaded;

        public SubtitleHandler(Action<string,string> updateSubtitleText, LibVLCSharp.Shared.MediaPlayer mediaPlayer, bool isShowing)
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
            _updateSubtitleText(string.Empty, string.Empty);
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
                _updateSubtitleText(string.Empty, string.Empty);
                return;
            }

            // 查询当前时间所在的字幕项
            var _currentSubtitle = _subtitles.FirstOrDefault(s =>
                s.StartTime <= _currentTime &&
                s.EndTime >= _currentTime);

            if (_currentSubtitle != null)
            {
                // 找到当前字幕，更新 CurrentSubtitleItem
                CurrentSubtitleItem = _currentSubtitle;

                // 拼接字幕文本，去除换行符
                var text = string.Join(" ", _currentSubtitle.Lines)
                                    .Replace("\\N", " ")
                                    .Replace("\\n", " ");

                // 定义句子终止符号
                //var sentenceEndings = new[] { ".", "。", "?", "？", "!", "！" };

                // 如果下一个字幕紧跟当前字幕且当前文本不以终止符结束，则进行合并
                var nextSubtitle = _subtitles.FirstOrDefault(s =>
                    s.StartTime == _currentSubtitle.EndTime &&
                    !sentenceTerminators.Any(e => text.EndsWith(e)));

                while (nextSubtitle != null)
                {
                    // 合并文本
                    text += " " + string.Join(" ", nextSubtitle.Lines)
                                        .Replace("\\N", " ")
                                        .Replace("\\n", " ");
                    // 更新结束时间为下一个字幕的结束时间
                    _currentSubtitle.EndTime = nextSubtitle.EndTime;
                    _subtitles.Remove(nextSubtitle);

                    // 检查下一个字幕是否继续合并
                    nextSubtitle = _subtitles.FirstOrDefault(s =>
                        s.StartTime == _currentSubtitle.EndTime &&
                        !sentenceTerminators.Any(e => text.EndsWith(e)));
                }
                // 更新显示：当前字幕作为当前显示，同时调用 GetPreviousSubtitle() 获取上一个字幕内容
                _updateSubtitleText(text, GetPreviousSubtitle());
            }
            else
            {
                // 当找不到新字幕时，使用上一个（最新）字幕作为 previousSubtitle 显示
                // 如果当前时间与最后一个字幕结束时间之差小于 3000 毫秒，则保持显示；超过 3 秒则清除
                if (CurrentSubtitleItem != null && (_currentTime - CurrentSubtitleItem.EndTime) < 3000)
                {
                    _updateSubtitleText(string.Empty, GetSubtitleItemText(CurrentSubtitleItem));
                }
                else
                {
                    _updateSubtitleText(string.Empty, string.Empty);
                }
            }
        }

        private string GetPreviousSubtitle()
        {
            if (_subtitles == null || !_subtitles.Any())
            {
                return string.Empty;
            }
            // 获取当前字幕项的索引
            int currentIndex = _subtitles.IndexOf(CurrentSubtitleItem);
            if (currentIndex < 0)
            {
                return string.Empty;
            }
            // 向前查找第一个非空字幕项
            int previousIndex = currentIndex - 1;
            while (previousIndex >= 0)
            {
                if (!string.IsNullOrWhiteSpace(GetSubtitleItemText(_subtitles[previousIndex])))
                {
                    return GetSubtitleItemText(_subtitles[previousIndex]);
                }
                previousIndex--;
            }
            return string.Empty;
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
            
            return sentenceTerminators.Contains(lastChar);
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

        public Tuple<int, int> GetCompleteSentenceRange()
        {
            if (CurrentSubtitleItem == null)
                return new Tuple<int, int>(-1, -1);
            Debug.WriteLine($"CurrentSubtitleItem.StartTime: {CurrentSubtitleItem.StartTime}");
            Debug.WriteLine($"CurrentSubtitleItem.Line: {string.Join("\\", CurrentSubtitleItem.Lines)}");
            Debug.WriteLine($"CurrentSubtitleItem.EndTime: {CurrentSubtitleItem.EndTime}");

            int currentIndex = _subtitles.IndexOf(CurrentSubtitleItem);

            // 获取当前字幕文本及状态
            string currentText = GetSubtitleItemText(_subtitles[currentIndex]).Trim();
            bool currentContainsTerminator = currentText.IndexOfAny(sentenceTerminators) != -1;
            bool currentEndsWithTerminator = EndsWithSentenceTerminator(currentText);

            // 确定起始字幕索引
            int startIndex = currentIndex;
            if (currentIndex > 0)
            {
                string prevText = GetSubtitleItemText(_subtitles[currentIndex - 1]).Trim();
                // 如果上一句以终止符结尾，则当前句起始点为当前字幕的起始点
                if (EndsWithSentenceTerminator(prevText))
                {
                    startIndex = currentIndex;
                }
                // 否则如果上一句中包含终止符（但不在末尾），则起始点为上一条字幕的开始
                else if (prevText.IndexOfAny(sentenceTerminators) >= 0)
                {
                    startIndex = currentIndex - 1;
                }
                else
                {
                    startIndex = currentIndex;
                }
            }
            // 如果当前句中间含有终止符（不在句尾），则起始点为当前字幕的开始
            if (currentContainsTerminator && !currentEndsWithTerminator)
            {
                startIndex = currentIndex;
            }

            // 确定结束字幕索引
            int endIndex = currentIndex;
            // 如果当前字幕中不包含任何终止符，则向后查找第一条包含终止符的字幕（不论出现在中间或末尾）
            if (currentText.IndexOfAny(sentenceTerminators) == -1)
            {
                for (int i = currentIndex + 1; i < _subtitles.Count; i++)
                {
                    string nextText = GetSubtitleItemText(_subtitles[i]).Trim();
                    if (nextText.IndexOfAny(sentenceTerminators) != -1)
                    {
                        endIndex = i;
                        break;
                    }
                    else
                    {
                        endIndex = i;
                    }
                }
            }
            else
            {
                endIndex = currentIndex;
            }

            // 限制合并后的句子长度不超过 MAX_SENTENCE_LENGTH
            StringBuilder sb = new StringBuilder();
            int adjustedEndIndex = endIndex;
            for (int i = startIndex; i <= endIndex; i++)
            {
                if (i > startIndex)
                    sb.Append(" ");
                sb.Append(GetSubtitleItemText(_subtitles[i]).Trim());
            }
            // 如果超出最大长度，则逐步缩短结束边界
            while (sb.Length > MAX_SENTENCE_LENGTH && adjustedEndIndex > startIndex)
            {
                adjustedEndIndex--;
                sb.Clear();
                for (int i = startIndex; i <= adjustedEndIndex; i++)
                {
                    if (i > startIndex)
                        sb.Append(" ");
                    sb.Append(GetSubtitleItemText(_subtitles[i]).Trim());
                }
            }

            return new Tuple<int, int>(startIndex, adjustedEndIndex);
        }

        public long GetCompleteSentenceStartTime()
        {
            var range = GetCompleteSentenceRange();
            if (range.Item1 >= 0)
                return _subtitles[range.Item1].StartTime;
            return CurrentSubtitleItem != null ? CurrentSubtitleItem.StartTime : 0;
        }

        public long GetCompleteSentenceForwardTime()
        {
            var range = GetCompleteSentenceRange();
            if (range.Item1 < 0)
                return 0;

            // 获取当前完整句子的终止时间
            long sentenceEndTime = _subtitles[range.Item2].EndTime;

            // 查找第一条开始时间大于当前句子终止时间的字幕
            var nextSubtitle = _subtitles.FirstOrDefault(sub => sub.StartTime > sentenceEndTime);

            if (nextSubtitle != null)
                return nextSubtitle.StartTime;
            else
                return sentenceEndTime + 200;
        }

        public long GetCurrentSubtitleStartTime()
        {
            return CurrentSubtitleItem != null ? CurrentSubtitleItem.StartTime : _currentTime;
        }

        public long GetPreviousSubtitleStartTime()
        {
            if (CurrentSubtitleItem == null) return _currentTime;
            int index = _subtitles.IndexOf(CurrentSubtitleItem);
            if (index > 0)
                return _subtitles[index - 1].StartTime;
            else
                return CurrentSubtitleItem.StartTime;
        }

        public long GetNextSubtitleStartTime()
        {
            if (CurrentSubtitleItem == null) return _currentTime;
            int index = _subtitles.IndexOf(CurrentSubtitleItem);
            if (index < _subtitles.Count - 1)
                return _subtitles[index + 1].StartTime;
            else
                return CurrentSubtitleItem.StartTime;
        }

        public void Dispose()
        {
            IsLoaded = false;

            _subtitles?.Clear();
            _subtitles = null;

            SubtitlesLoaded = null;
            _updateSubtitleText(string.Empty,string.Empty);

        }
    }
}