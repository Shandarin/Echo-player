
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
        private static readonly Regex ItalicRegex = new Regex(@"<i>(.*?)</i>", RegexOptions.Singleline);
        private static readonly Regex BoldRegex = new Regex(@"<b>(.*?)</b>", RegexOptions.Singleline);
        private static readonly Regex ColorRegex = new Regex(@"<font color=""?(#?\w+)""?>(.*?)</font>", RegexOptions.Singleline);

        public static List<Inline> ProcessSubtitleText(string text, Action<string, Point> onWordClick = null)
        {
            var processedInlines = new List<Inline>();
            var segments = SplitTextIntoSegments(text);

            foreach (var segment in segments)
            {
                List<Inline> segmentInlines;
                switch (segment.Type)
                {
                    case SegmentType.Italic:
                        segmentInlines = ProcessWords(segment.Text, FontStyles.Italic, null, null, onWordClick);
                        break;
                    case SegmentType.Bold:
                        segmentInlines = ProcessWords(segment.Text, null, FontWeights.Bold, null, onWordClick);
                        break;
                    case SegmentType.Color:
                        var color = ColorConverter.ConvertFromString(segment.Color);
                        var brush = new SolidColorBrush((Color)color);
                        segmentInlines = ProcessWords(segment.Text, null, null, brush, onWordClick);
                        break;
                    default:
                        segmentInlines = ProcessWords(segment.Text, null, null, null, onWordClick);
                        break;
                }
                processedInlines.AddRange(segmentInlines);
            }

            return processedInlines;
        }

        private static List<Inline> ProcessWords(string text, FontStyle? fontStyle, FontWeight? fontWeight,
            Brush foreground, Action<string, Point> onWordClick)
        {
            var inlines = new List<Inline>();
            var words = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            bool isFirst = true;

            foreach (var word in words)
            {
                if (!isFirst)
                {
                    inlines.Add(new Run(" "));
                }

                var run = new Run(word);
                if (fontStyle.HasValue) run.FontStyle = fontStyle.Value;
                if (fontWeight.HasValue) run.FontWeight = fontWeight.Value;
                if (foreground != null) run.Foreground = foreground;

                if (onWordClick != null)
                {
                    var container = new Span(run);
                    container.MouseEnter += (s, e) => OnWordMouseEnter(run);
                    container.MouseLeave += (s, e) => OnWordMouseLeave(run, foreground);
                    container.MouseLeftButtonDown += (s, e) =>
                    {
                        Debug.WriteLine($"Word clicked: {word}");
                        onWordClick(word, e.GetPosition((IInputElement)s));
                        e.Handled = true;
                    };
                    inlines.Add(container);
                }
                else
                {
                    inlines.Add(run);
                }

                isFirst = false;
            }

            return inlines;
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
            run.Foreground = (Brush)run.GetValue(Run.TagProperty) ?? originalForeground ?? new SolidColorBrush(Colors.White);
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
                        param = match.Groups[1].Value;
                    }
                }
            }

            return nextMatch;
        }

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