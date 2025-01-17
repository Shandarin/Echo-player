using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Echo.utils
{
    public class SubtitleExtractor
    {
        private readonly string _ffmpegPath;

        public SubtitleExtractor(string ffmpegPath)
        {
            if (!File.Exists(ffmpegPath))
                throw new FileNotFoundException("FFmpeg executable not found.", ffmpegPath);

            _ffmpegPath = ffmpegPath;
        }
        // 提取输入文件中的所有字幕流
        public List<string> ExtractAllSubtitles(string inputFile, string outputDirectory = null)
        {
            if (!File.Exists(inputFile))
                throw new FileNotFoundException("Input file not found.", inputFile);

            // 默认输出目录为输入文件所在目录
            outputDirectory ??= Path.GetDirectoryName(inputFile);
            Directory.CreateDirectory(outputDirectory);

            var subtitleStreams = GetSubtitleStreams(inputFile);
            var extractedFiles = new List<string>();

            int subtitleCount = 0;
            foreach (var stream in subtitleStreams)
            {
                string baseName = Path.GetFileNameWithoutExtension(inputFile);
                string langSuffix = string.IsNullOrEmpty(stream.Language) ? "" : $"_{stream.Language}";
                string outFileName = $"{baseName}_sub{subtitleCount}{langSuffix}.srt";
                string outPath = Path.Combine(outputDirectory, outFileName);


                string arguments = $"-i \"{inputFile}\" -map {stream.MapIndex} -c:s srt \"{outPath}\" -y";

                var psi = new ProcessStartInfo
                {
                    FileName = _ffmpegPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (var process = new Process())
                {
                    process.StartInfo = psi;
                    process.Start();

                    string stdOut = process.StandardOutput.ReadToEnd();
                    string stdErr = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                       
                        throw new InvalidOperationException($"FFmpeg failed: {stdErr}");
                    }
                }

                if (File.Exists(outPath))
                {
                    extractedFiles.Add(outPath);
                }

                subtitleCount++;
            }

            return extractedFiles;
        }

        // 获取输入文件中的字幕流信息
        private List<SubtitleStreamInfo> GetSubtitleStreams(string inputFile)
        {
            var result = new List<SubtitleStreamInfo>();

            //string baseName = Path.GetFileNameWithoutExtension(inputFile);
            //string outFileName = $"{baseName}_sub{subtitleCount}{langSuffix}.text";
            //string outPath = Path.Combine(outputDirectory, outFileName);

            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string projectRoot = Directory.GetParent(baseDirectory)?.Parent?.Parent?.Parent?.FullName;
            string outFilePath = System.IO.Path.Combine(projectRoot, "storage", "subtitles", $"{inputFile}.txt");


            var psi = new ProcessStartInfo
            {
                FileName = _ffmpegPath,
                Arguments = $"-i \"{inputFile}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            string ffmpegOutput;
            using (var process = new Process())
            {
                process.StartInfo = psi;
                process.Start();
                ffmpegOutput = process.StandardError.ReadToEnd(); // FFmpeg 的信息主要输出到错误流
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    
                    Debug.WriteLine("ok");
                    Debug.WriteLine(ffmpegOutput);
                    Debug.WriteLine(psi.Arguments);
                    //throw new InvalidOperationException($"FFmpeg failed to analyze file: {ffmpegOutput}");
                    
                }
            }

            var regex = new Regex(
                @"Stream\s+#0:(?:s:)?(?<index>\d+)(?:\((?<lang>\w+)\))?:\s+Subtitle:\s+(?<codec>\S+)",
                RegexOptions.IgnoreCase);

            var matches = regex.Matches(ffmpegOutput);
            foreach (Match m in matches)
            {
                var indexStr = m.Groups["index"].Value;
                var langStr = m.Groups["lang"].Value;
                var codecStr = m.Groups["codec"].Value;

                string mapIndex = $"0:s:{indexStr}";

                result.Add(new SubtitleStreamInfo
                {
                    MapIndex = mapIndex,
                    Language = langStr,
                    Codec = codecStr
                });
            }

            return result;
        }

        private class SubtitleStreamInfo
        {
            public string MapIndex { get; set; }
            public string Language { get; set; }
            public string Codec { get; set; }
        }
    }
}