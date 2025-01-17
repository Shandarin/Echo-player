using LibVLCSharp.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace Echo.Utils
{
    public class MkvToolnixExtractor
    {
        private readonly string _mkvInfoPath;
        private readonly string _mkvExtractPath;

        /// <summary>
        /// 构造函数，传入 mkvinfo.exe 和 mkvextract.exe 的完整路径。
        /// 或者你可以只用 mkvmerge -i 来获取轨道，再用 mkvextract 提取。
        /// </summary>
        public MkvToolnixExtractor(string mkvInfoPath, string mkvExtractPath)
        {
            if (!File.Exists(mkvInfoPath))
                throw new FileNotFoundException("mkvinfo executable not found.", mkvInfoPath);

            if (!File.Exists(mkvExtractPath))
                throw new FileNotFoundException("mkvextract executable not found.", mkvExtractPath);

            _mkvInfoPath = mkvInfoPath;
            _mkvExtractPath = mkvExtractPath;
        }

        /// <summary>
        /// 提取 MKV 文件内所有字幕轨道到 .srt 文件。
        /// 仅限文本字幕(如 SRT/ASS 等)，若是图像字幕(如 PGS、VobSub)，导出后是 .sup/.sub。
        /// 这里不做 OCR 处理。
        /// 
        /// 返回生成的字幕文件列表。
        /// </summary>
        public List<string> ExtractAllSubtitles(string inputMkv, string outputDirectory = null)
        {
            if (!File.Exists(inputMkv))
                throw new FileNotFoundException("Input MKV not found.", inputMkv);

            if (string.IsNullOrEmpty(outputDirectory))
                outputDirectory = Path.GetDirectoryName(inputMkv);

            Directory.CreateDirectory(outputDirectory);

            // 1. 获取全部字幕轨道信息
            var subtitleTracks = GetSubtitleTracks(inputMkv);
            var extractedFiles = new List<string>();

            Debug.WriteLine("subtitleTracks");
            Debug.WriteLine(subtitleTracks);

            // 2. 依次提取
            //    mkvextract tracks "input.mkv" 3:"out.srt" 4:"out2.srt" ...
            // 也可以一个轨道一个命令，下面示例逐轨道提取
            int index = 0;
            foreach (var track in subtitleTracks)
            {
                // 构建输出文件名
                // 例如：<视频文件名>_track<TrackID>_<Language>.srt
                string baseName = Path.GetFileNameWithoutExtension(inputMkv);
                string langPart = string.IsNullOrEmpty(track.Language) ? "" : "_" + track.Language;
                string outFileName = $"{baseName}_track{track.TrackID}{langPart}.{track.FileExtension}";
                string outPath = Path.Combine(outputDirectory, outFileName);

                // 调用 mkvextract 命令
                // 例如：mkvextract tracks "input.mkv" 3:"C:\Out\xxx.srt"
                string arguments = $"tracks \"{inputMkv}\" {track.TrackID}:\"{outPath}\"";
                RunProcess(_mkvExtractPath, arguments);

                // 如果导出成功，加入列表
                if (File.Exists(outPath))
                {
                    extractedFiles.Add(outPath);
                }
                index++;
            }

            return extractedFiles;
        }

        /// <summary>
        /// 使用 mkvinfo 来获取字幕轨道的 TrackID、语言、可能的扩展名等
        /// </summary>
        private List<MkvSubtitleTrack> GetSubtitleTracks(string inputMkv)
        {
            var tracks = new List<MkvSubtitleTrack>();

            // 执行 mkvinfo "input.mkv"
            string output = RunProcess(_mkvInfoPath, $"\"{inputMkv}\" --ui-language en");
            Debug.WriteLine("output");
            Debug.WriteLine(output);
            // 需要在 output 里解析类似:
            // | + A track
            // |  + Track number: 3 (track ID for mkvmerge & mkvextract: 3)
            // |  + Track type: subtitles
            // |  + Language: eng
            // |  + Codec ID: S_TEXT/ASS
            // ...
            //
            // 有时 Language 不一定有，有时 Codec ID 可能是 S_TEXT/UTF8（SRT）、S_TEXT/ASS（ASS），
            // S_VOBSUB（VobSub），S_HDMV/PGS（PGS）等
            //
            // Track ID 从 " (track ID for mkvmerge & mkvextract: X)" 里拿
            // 语言从 "Language: eng" 里拿
            // Codec ID 从 "Codec ID: S_TEXT/UTF8" 里拿
            //
            // 此处给出一个简易的正则/解析方法，仅做参考，实际需更严格判断
            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            // 在 mkvinfo 的输出中找 带 "Track type: subtitles" 的段，然后收集相关信息
            // 由于 mkvinfo 输出是分行的，需要一点状态机式解析
            bool inSubtitleSection = false;
            MkvSubtitleTrack currentTrack = null;

            foreach (var rawLine in lines)
            {
                string line = rawLine.Trim();
                Debug.WriteLine("line");
                Debug.WriteLine(line);
                if (line.Contains("+ Track number"))
                {
                    // 开始新轨道
                    inSubtitleSection = false;
                    currentTrack = new MkvSubtitleTrack();
                }

                // 判断是否是字幕轨
                if (currentTrack != null)
                {
                    if (line.Contains("Track type: subtitles"))
                    {
                        inSubtitleSection = true;
                    }
                }


                if (currentTrack != null)
                {
                    // Track ID
                    // 例："Track number: 3 (track ID for mkvmerge & mkvextract: 3)"
                    var trackIdMatch = Regex.Match(line, @"\(track ID for mkvmerge & mkvextract:\s*(\d+)\)");
                    if (trackIdMatch.Success)
                    {
                        currentTrack.TrackID = trackIdMatch.Groups[1].Value;
                    }

                    // Language
                    // 例："Language: eng"
                    var langMatch = Regex.Match(line, @"Language:\s*([a-zA-Z0-9]+)");
                    if (langMatch.Success)
                    {
                        currentTrack.Language = langMatch.Groups[1].Value;
                    }

                    // Codec ID
                    // 例："Codec ID: S_TEXT/UTF8"
                    var codecMatch = Regex.Match(line, @"Codec ID:\s*(S_[^\s]+)");
                    if (codecMatch.Success)
                    {
                        currentTrack.CodecID = codecMatch.Groups[1].Value;
                    }
                }

                // 当我们遇到 "| + Track" 下一个轨道了 或者文本结束了，就把这个 Track 加入列表
                // 这里做个判断
                if (line.StartsWith("| + Track") || rawLine == lines[lines.Length - 1])
                {
                    // 若 currentTrack 已标记为 subtitles，并且 ID 非空，则是有效字幕
                    if (inSubtitleSection && currentTrack != null && !string.IsNullOrEmpty(currentTrack.TrackID))
                    {
                        // 根据 codecID 推测扩展名
                        currentTrack.FileExtension = GuessSubtitleExtension(currentTrack.CodecID);
                        tracks.Add(currentTrack);
                    }
                    // reset
                    currentTrack = new MkvSubtitleTrack();
                    inSubtitleSection = false;
                }
            }

            return tracks;
        }

        private string GuessSubtitleExtension(string codecId)
        {
            // 常见几种编码ID -> 扩展名
            // S_TEXT/UTF8, S_TEXT/SSA, S_TEXT/ASS -> srt/ssa/ass
            // S_VOBSUB -> .sub
            // S_HDMV/PGS -> .sup
            // 你可根据需求自行判断，这里粗略演示

            if (string.IsNullOrEmpty(codecId))
                return "sub"; // fallback

            if (codecId.Contains("UTF8"))
                return "srt";
            if (codecId.Contains("ASS"))
                return "ass";
            if (codecId.Contains("SSA"))
                return "ssa";
            if (codecId.Contains("VOBSUB"))
                return "sub";
            if (codecId.Contains("PGS"))
                return "sup";

            return "sub"; // fallback
        }

        private string RunProcess(string exePath, string arguments)
        {
            var psi = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            using (var process = new Process())
            {
                process.StartInfo = psi;
                process.Start();
                string stdOut = process.StandardOutput.ReadToEnd();
                string stdErr = process.StandardError.ReadToEnd();
                process.WaitForExit();

                // mkvinfo 的关键信息大多在 stdout
                // mkvextract 则可能在 stderr/output 都会有
                // 这里统一拼接作为结果返回
                return stdOut + "\n" + stdErr;
            }
        }

        private class MkvSubtitleTrack
        {
            public string TrackID { get; set; }         // mkvextract用的轨道ID
            public string Language { get; set; }        // 可能是eng, chi等
            public string CodecID { get; set; }         // S_TEXT/UTF8, S_HDMV/PGS ...
            public string FileExtension { get; set; }   // srt, ass, sub, sup ...
        }
    }
}