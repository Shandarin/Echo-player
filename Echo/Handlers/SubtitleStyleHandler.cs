
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using System.Diagnostics;


namespace Echo.Handlers
    {
        public class SubtitleStyleHandler
        {
            // 1) 斜体 / 粗体 / 颜色 的匹配（保持不变）
            private static readonly Regex ItalicRegex = new Regex(@"<i>(.*?)</i>", RegexOptions.Singleline);
            private static readonly Regex BoldRegex = new Regex(@"<b>(.*?)</b>", RegexOptions.Singleline);
            private static readonly Regex ColorRegex = new Regex(@"<font color=""?(#?\w+)""?>(.*?)</font>", RegexOptions.Singleline);

            // 2) 新的 tokenRegex：
            //    - (\r\n|\n|\r) 匹配换行符
            //    - [A-Za-z0-9\p{L}\p{M}\p{N}]+(?:[-'][A-Za-z0-9\p{L}\p{M}\p{N}]+)*  
            //      表示一段以字母/数字/Unicode字母开头的单词，中间可带有连字符或撇号；例如 isn't / self-contained
            //    - [^\p{L}\p{M}\p{N}\s]+ 匹配任何非字母/数字/空白的符号（即标点符号等）
            //    - \S 匹配任意非空白字符 (fallback)
            private static readonly Regex tokenRegex = new Regex(
                @"(\r\n|\n|\r|[A-Za-z0-9\p{L}\p{M}\p{N}]+(?:[-'][A-Za-z0-9\p{L}\p{M}\p{N}]+)*|[^\p{L}\p{M}\p{N}\s]+|\S)",
                RegexOptions.Compiled);

            /// <summary>
            /// 主入口：先拆分成 Segment，然后逐段处理
            /// </summary>
            public static List<Inline> ProcessSubtitleText(string text, Action<string, Point> onWordClick = null)
            {
                // 拆分得到多个带样式的段落
                var segments = SplitTextIntoSegments(text);

                var processedInlines = new List<Inline>();

                // 使用 for 循环，可以方便在本段处理完后看下一段情况（是否要插空格）
                for (int i = 0; i < segments.Count; i++)
                {
                    var segment = segments[i];

                // 在进入 Regex 拆分之前，处理换行符后加空格、样式标签前后加空格等
                string segmentText = segment.Text;
                segmentText = Regex.Replace(segmentText, @"<(i|b|u|font)>\s*[\r\n]+", "<$1>").TrimStart('\r', '\n');
                segmentText
                        .Replace("\n", "\n ")
                        .Replace("\r", "\r ")
                        .Replace("<i>", " <i>")
                        .Replace("</i>", " </i>")
                        .Replace("<b>", " <b>")
                        .Replace("</b>", " </b>");
                //Debug.WriteLine(segmentText);
                    // 根据 segment.Type 决定样式
                    FontStyle? fs = null;
                    FontWeight? fw = null;
                    Brush brush = null;

                    switch (segment.Type)
                    {
                        case SegmentType.Italic:
                            fs = FontStyles.Italic;
                            break;
                        case SegmentType.Bold:
                            fw = FontWeights.Bold;
                            break;
                        case SegmentType.Color:
                            if (!string.IsNullOrWhiteSpace(segment.Color))
                            {
                                var colorObj = ColorConverter.ConvertFromString(segment.Color);
                                if (colorObj is Color colorVal)
                                {
                                    brush = new SolidColorBrush(colorVal);
                                }
                            }
                            break;
                        default:
                            // Normal 段落
                            break;
                    }

                    // 调用正则拆分保留标点，并生成一组 Inlines
                    var segmentInlines = ProcessTokensKeepPunctuation(segmentText, fs, fw, brush, onWordClick);
                    processedInlines.AddRange(segmentInlines);

                    // 3) 在斜体后面如果紧跟着下一段是普通文字，则插一个空格
                    if (segment.Type == SegmentType.Italic
                        && i < segments.Count - 1
                        && segments[i + 1].Type == SegmentType.Normal)
                    {
                        processedInlines.Add(new Run(" "));
                    }

                    // 如果还想“斜体/粗体 => 普通”都插空格，也可以写成：
                    /*
                    if ((segment.Type == SegmentType.Italic 
                      || segment.Type == SegmentType.Bold 
                      || segment.Type == SegmentType.Color)
                      && i < segments.Count - 1
                      && segments[i + 1].Type == SegmentType.Normal)
                    {
                        processedInlines.Add(new Run(" "));
                    }
                    */
                }

                return processedInlines;
            }

            /// <summary>
            /// 用正则匹配 token，保留标点并将撇号/连字符视为单词一部分；再根据是否是文字决定是否加点击事件
            /// </summary>
            private static List<Inline> ProcessTokensKeepPunctuation(
                string text,
                FontStyle? fontStyle,
                FontWeight? fontWeight,
                Brush foreground,
                Action<string, Point> onWordClick)
            {
                var inlines = new List<Inline>();
                var matches = tokenRegex.Matches(text);

                bool isFirst = true;
                foreach (Match match in matches)
                {
                    var token = match.Value;
                    // 判断是否包含字母或数字，用于决定是否加点击事件
                    bool isWordLike = ContainsLetterOrDigit(token);

                    // 如果不是第一个 token、且当前 token 不是换行符，可以考虑插一个空格
                    // 注：如果不想在标点前面插空格，可以根据 token 特征灵活定制
                    if (!isFirst && !IsNewLine(token))
                    {
                        inlines.Add(new Run(" "));
                    }

                    // 创建一个 Run
                    var run = new Run(token);

                    // 设置样式
                    if (fontStyle.HasValue) run.FontStyle = fontStyle.Value;
                    if (fontWeight.HasValue) run.FontWeight = fontWeight.Value;
                    if (foreground != null) run.Foreground = foreground;

                    // 如果需要点击事件且是“文字型”token
                    if (onWordClick != null && isWordLike)
                    {
                        var container = new Span(run);
                        container.MouseEnter += (s, e) => OnWordMouseEnter(run);
                        container.MouseLeave += (s, e) => OnWordMouseLeave(run, foreground);
                        container.MouseLeftButtonDown += (s, e) =>
                        {
                            //Debug.WriteLine($"Word clicked: {token}");
                            onWordClick(token, e.GetPosition((IInputElement)s));
                            e.Handled = true;
                        };
                        inlines.Add(container);
                    }
                    else
                    {
                        // 标点或空白之类直接用 Run
                        inlines.Add(run);
                    }

                    isFirst = false;
                }

                return inlines;
            }

            /// <summary>简单判断 token 里是否包含字母或数字，用来决定是否给它加点击事件</summary>
            private static bool ContainsLetterOrDigit(string token)
            {
                foreach (char c in token)
                {
                    if (char.IsLetterOrDigit(c))
                    {
                        return true;
                    }
                }
                return false;
            }

            /// <summary>是否换行</summary>
            private static bool IsNewLine(string token)
            {
                return token == "\r" || token == "\n" || token == "\r\n";
            }

            private static void OnWordMouseEnter(Run run)
            {
            run.SetValue(Run.TagProperty, run.Foreground);
                run.TextDecorations = TextDecorations.Underline;
                run.Foreground = new SolidColorBrush(Colors.Yellow);
                Mouse.OverrideCursor = Cursors.Hand;
            DependencyObject current = run;
                while (current != null)
                {
                    if (current is TextBlock tb && tb.Name == "SubtitleTextBlock")
                    {
                        var background = new SolidColorBrush(Colors.Black);
                        background.Opacity = 0.35;
                        tb.Background = background;
                        break;
                    }
                    current = LogicalTreeHelper.GetParent(current);
                }
            }

            private static void OnWordMouseLeave(Run run, Brush originalForeground)
            {
                run.TextDecorations = null;
                run.Foreground = (Brush)run.GetValue(Run.TagProperty)
                                 ?? originalForeground
                                 ?? new SolidColorBrush(Colors.White);
                Mouse.OverrideCursor = null;

                DependencyObject current = run;
                while (current != null)
                {
                    if (current is TextBlock tb && tb.Name == "SubtitleTextBlock")
                    {
                        var background = new SolidColorBrush(Colors.Black);
                        background.Opacity = 0.5;
                        tb.Background = background;
                        break;
                    }
                    current = LogicalTreeHelper.GetParent(current);
                }
            }

            // ========== 以下是分段逻辑，保持不变，只加了 enum 的注释 ==========
            private static List<TextSegment> SplitTextIntoSegments(string text)
            {
                var segments = new List<TextSegment>();
                int currentIndex = 0;

                while (currentIndex < text.Length)
                {
                    var italicMatch = ItalicRegex.Match(text, currentIndex);
                    var boldMatch = BoldRegex.Match(text, currentIndex);
                    var colorMatch = ColorRegex.Match(text, currentIndex);

                    var nextMatch = GetNextMatch(text.Length, currentIndex,
                        new[] { italicMatch, boldMatch, colorMatch },
                        out SegmentType type,
                        out string param);

                    if (!nextMatch.Success)
                    {
                        if (currentIndex < text.Length)
                        {
                            segments.Add(new TextSegment
                            {
                                Type = SegmentType.Normal,
                                Text = text.Substring(currentIndex)
                            });
                        }
                        break;
                    }

                    if (nextMatch.Index > currentIndex)
                    {
                        segments.Add(new TextSegment
                        {
                            Type = SegmentType.Normal,
                            Text = text.Substring(currentIndex, nextMatch.Index - currentIndex)
                        });
                    }

                    // 对应正则里，各组的内容
                    var contentGroup = type == SegmentType.Color ? 2 : 1;
                    segments.Add(new TextSegment
                    {
                        Type = type,
                        Text = nextMatch.Groups[contentGroup].Value,
                        Color = type == SegmentType.Color ? param : null
                    });

                    currentIndex = nextMatch.Index + nextMatch.Length;
                }

                return segments;
            }

            private static Match GetNextMatch(int textLength, int currentIndex,
                Match[] matches, out SegmentType type, out string param)
            {
                type = SegmentType.Normal;
                param = null;
                Match nextMatch = Match.Empty;
                int nearestIndex = textLength;

                for (int i = 0; i < matches.Length; i++)
                {
                    var match = matches[i];
                    if (match.Success && match.Index >= currentIndex && match.Index < nearestIndex)
                    {
                        nearestIndex = match.Index;
                        nextMatch = match;
                        type = (SegmentType)i;
                        if (type == SegmentType.Color)
                        {
                            param = match.Groups[1].Value; // Color 值
                        }
                    }
                }

                return nextMatch;
            }

            // ：Normal = -1, Italic=0, Bold=1, Color=2
            private enum SegmentType
            {
                Normal = -1,
                Italic = 0,
                Bold = 1,
                Color = 2
            }

            private class TextSegment
            {
                public SegmentType Type { get; set; }
                public string Text { get; set; }
                public string Color { get; set; }
            }
        }
    }