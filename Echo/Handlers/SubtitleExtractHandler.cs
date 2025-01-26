using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Echo.Handlers
{
    public class SubtitleExtractHandler
    {

        public static async Task ExtractEmbeddedSubtitles(string videoPath)
        {
            var appRootDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var mkvExtractPath = Path.Combine(appRootDirectory, "mkvtoolnix", "mkvextract.exe");
            var mkvInfoPath = Path.Combine(appRootDirectory, "mkvtoolnix", "mkvinfo.exe");
            var outputDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Echo", "Subtitles");
            Directory.CreateDirectory(outputDir);

            // Get track info
            var trackInfo = GetTrackInfo(mkvInfoPath, videoPath);
            if (string.IsNullOrEmpty(trackInfo))
            {
                throw new Exception("Failed to get track info from video file.");
            }

            // Parse subtitle tracks
            var subtitleTracks = ParseTracks(trackInfo);
            if (!subtitleTracks.Any())
            {
                throw new Exception("No subtitle tracks found in the video file.");
            }

            // Get video filename without extension for naming subtitle files
            var videoFileName = Path.GetFileNameWithoutExtension(videoPath);

            foreach (var track in subtitleTracks)
            {
                // Generate subtitle filename
                string subtitleFileName = GenerateSubtitleFileName(videoFileName, track);
                string outputPath = Path.Combine(outputDir, subtitleFileName);

                // Extract subtitle using mkvextract
                using (var process = new Process())
                {
                    process.StartInfo = new ProcessStartInfo
                    {
                        FileName = mkvExtractPath,
                        Arguments = $"\"{videoPath}\" tracks {track.TrackId}:\"{outputPath}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    try
                    {
                        process.Start();
                        await process.WaitForExitAsync();

                        if (process.ExitCode != 0)
                        {
                            var errorMessage = process.StandardError.ReadToEnd();
                            throw new Exception($"Failed to extract subtitle track {track.TrackId}: {errorMessage}");
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Error executing mkvextract: {ex.Message}");
                    }
                }
            }
        }

        private static string GetTrackInfo(string mkvInfoPath, string videoPath)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = mkvInfoPath,
                    Arguments = $"--ui-language en \"{videoPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                var errorMessage = process.StandardError.ReadToEnd();
                Console.WriteLine($"MKVInfo Error: {errorMessage}");
                return null;
            }

            return output;
        }

        public static List<string> LoadSavedSubtitles(string videoPath)
        {
            var subtitleDir = Path.Combine(Path.GetDirectoryName(videoPath), "ExtractedSubtitles");
            var subtitleFiles = Directory.GetFiles(subtitleDir, "*.srt")
                                          .Concat(Directory.GetFiles(subtitleDir, "*.ass"))
                                          .ToList();

            var SubtitleList = new List<string>(subtitleFiles); // Update the subtitle list
            return SubtitleList;
        }

        //public void SelectDefaultSubtitle(string LearningLanguage, List<string> SubtitleList)
        //{
        //    var preferredLanguage = LearningLanguage; // Assuming you have a configuration for preferred language
        //    var defaultSubtitle = SubtitleList.FirstOrDefault(subtitle => subtitle.Contains(preferredLanguage));

        //    if (defaultSubtitle != null)
        //    {
        //        LoadSubtitle(defaultSubtitle);
        //    }
        //}

        //private static List<int> GetSubtitleTrackNumbers(string mkvInfoOutput)
        //{
        //    var subtitleTrackNumbers = new List<int>();

        //    // Regular expression to match subtitle tracks
        //    var regex = new Regex(@"\+ Track number: (\d+) \(track ID for mkvmerge & mkvextract: (\d+)\)\s+\|.*?Track type: subtitles", RegexOptions.Multiline);

        //    var matches = regex.Matches(mkvInfoOutput);

        //    foreach (Match match in matches)
        //    {
        //        // Extract the mkvextract-compatible track ID (2nd capture group)
        //        if (int.TryParse(match.Groups[2].Value, out int trackId))
        //        {
        //            subtitleTrackNumbers.Add(trackId);
        //        }
        //    }

        //    return subtitleTrackNumbers;
        //}

        private static string GenerateSubtitleFileName(string videoFileName, SubtitleTrackInfo track)
        {
            var parts = new List<string> { videoFileName };

            // Add language if available
            if (!string.IsNullOrEmpty(track.Language))
            {
                parts.Add(track.Language);
            }

            //// Add track name if available
            //if (!string.IsNullOrEmpty(track.Name))
            //{
            //    parts.Add(track.Name);
            //}

            //// Add SDH indicator if it's for hearing impaired
            //if (track.IsHearingImpaired)
            //{
            //    parts.Add("SDH");
            //}

            // Add default indicator
            if (track.IsOriginalLanguage)
            {
                parts.Add("OriginalLang");
            }

            // Combine all parts with underscores and add extension
            return $"{string.Join("_", parts)}.srt";
        }


        public static List<SubtitleTrackInfo> ParseTracks(string input)
        {
            var trackInfos = new List<SubtitleTrackInfo>();
            var lines = input.Split('\n');
            SubtitleTrackInfo currentTrack = null;

            foreach (var line in lines)
            {
                Debug.WriteLine(line);
                var trimmedLine = line.Trim();

                // Start of a new track
                if (trimmedLine.Contains("Track number"))
                {
                    if (currentTrack != null && currentTrack.IsSubtitle)
                    {
                        trackInfos.Add(currentTrack);
                    }
                    currentTrack = new SubtitleTrackInfo();

                    // Then parse track number and ID
                    var trackNumberStr = trimmedLine.Split(':')[1].Trim().Split(' ')[0];
                    currentTrack.TrackId = ParseTrackId(trimmedLine);
                    continue;
                }

                // Skip if we're not in a track section
                if (currentTrack == null) continue;

                // Parse track properties
                if (trimmedLine.Contains("Track type"))
                {
                    currentTrack.IsSubtitle = trimmedLine.Contains("subtitles");
                }
                //else if (trimmedLine.Contains("Codec ID:"))
                //{
                //    currentTrack.CodecID = trimmedLine.Split(':')[1].Trim();
                //}
                else if (trimmedLine.Contains("Language"))
                {
                    currentTrack.Language = trimmedLine.Split(':')[1].Trim();
                }
                //else if (trimmedLine.Contains("Name"))
                //{
                //    currentTrack.Name = trimmedLine.Split(':')[1].Trim();
                //}
                //else if (trimmedLine.Contains("\"Default track\" flag:"))
                //{
                //    currentTrack.IsDefault = trimmedLine.Split(':')[1].Trim() == "1";
                //}

                else if (trimmedLine.Contains("Original language"))
                {
                    currentTrack.IsOriginalLanguage = trimmedLine.Split(':')[1].Trim() == "1";
                }
                
            }

            // Add the last track if it's a subtitle track
            if (currentTrack != null && currentTrack.IsSubtitle)
            {
                trackInfos.Add(currentTrack);
                Debug.WriteLine(currentTrack.TrackId);
            }

            return trackInfos;
        }

        private static int ParseTrackId(string trackNumberLine)
        {
            try
            {
                // 在行中定位 "mkvextract" 关键字
                int mkvExtractIndex = trackNumberLine.IndexOf("mkvextract");
                if (mkvExtractIndex == -1) return -1;

                // 找到 "mkvextract: " 后面的数字
                int colonIndex = trackNumberLine.IndexOf(':', mkvExtractIndex);
                if (colonIndex == -1) return -1;

                // 提取并解析数字
                string idStr = trackNumberLine.Substring(colonIndex + 1).Trim();
                idStr = idStr.TrimEnd(')'); // 移除可能的右括号
                return int.Parse(idStr);
            }
            catch
            {
                return -1;
            }
        }

        public class SubtitleTrackInfo
        {
            //public int TrackNumber { get; set; }
            public int TrackId { get; set; }
            public string CodecID { get; set; }
            public string Language { get; set; }
            public string Name { get; set; }
            //public bool IsDefault { get; set; }
            //public bool IsHearingImpaired { get; set; }
            public bool IsOriginalLanguage { get; set; }
            public bool IsSubtitle { get; set; }
        }
    }
}
