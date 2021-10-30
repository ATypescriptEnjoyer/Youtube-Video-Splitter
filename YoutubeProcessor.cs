using System;
using System.Threading.Tasks;
using YoutubeDLSharp;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.IO;

namespace YT_Playlist_DL_Splitter
{
    public class YoutubeProcessor
    {

        private string videoUrl { get; }
        private string savePath { get; }
        private YoutubeDL downloader { get; }

        public YoutubeProcessor(string url, string storagePath)
        {
            this.videoUrl = url;
            this.savePath = storagePath;
            this.downloader = new YoutubeDL(1);
            this.downloader.YoutubeDLPath = "./yt-dlp.exe";
            this.downloader.FFmpegPath = "./ffmpeg.exe";
        }

        private int ConvertStringToTime(string time)
        {
            var timeSplit = time.Split(":");
            var seconds = 0;
            if (time.Length == 3)
            {
                seconds = (int.Parse(timeSplit[0]) * 60 * 60);
                timeSplit = timeSplit.Skip(1).ToArray();
            }
            seconds += (int.Parse(timeSplit[0]) * 60) + int.Parse(timeSplit[1]);
            return seconds;
        }

        private string ReplaceInvalidChars(string filename)
        {
            return string.Join("_", filename.Split(Path.GetInvalidFileNameChars()));
        }

        public async Task Start()
        {
            var metadata = await this.downloader.RunVideoDataFetch(this.videoUrl);
            if (metadata.Data.Description == "")
            {
                Console.WriteLine("Unable to process video description.");
                return;
            }

            var videoName = metadata.Data.Title;
            var description = metadata.Data.Description;
            var descriptionLines = description.Split("\n").Where((value) => value != "").ToArray();
            var regexPattern = new Regex("^((?:[0-9]{1,2}:{0,2}){1,3})(.*)");
            var trackList = new List<Track>();
            foreach (string possibleTrack in descriptionLines)
            {
                if (regexPattern.IsMatch(possibleTrack))
                {
                    var match = regexPattern.Match(possibleTrack);
                    trackList.Add(new Track() { StartTime = match.Groups[1].Value.Trim(), TrackName = match.Groups[2].Value.Trim() });
                }
            }

            Console.WriteLine($"{trackList.Count} Tracks Found. Downloading and parsing now.");

            var res = await this.downloader.RunAudioDownload(this.videoUrl, YoutubeDLSharp.Options.AudioConversionFormat.Mp3);

            Console.WriteLine("Download finished, passing everything to FFMPEG.");

            var audioPath = res.Data;

            var ffmpegTemplate = "-ss {STARTTIMESECONDS} -i \"{INPUT}\" -t {LENGTHSECONDS} -c copy \"{OUTPUT}\"";

            var outputFolder = Path.Join(this.savePath, $"{videoName}\\");
            var directory = Directory.CreateDirectory(outputFolder);

            for (int i = 0; i < trackList.Count; i++)
            {
                var command = ffmpegTemplate;
                var track = trackList[i];
                var startTime = 0;
                var length = 0;
                var input = Path.Join(audioPath.Substring(0, audioPath.LastIndexOf("\\")), $"{videoName}.mp3");
                var output = $"{directory.FullName}{ReplaceInvalidChars(track.TrackName.Trim())}.mp3";
                if (i == 0)
                {
                    var nextTrack = trackList[i + 1];
                    length = ConvertStringToTime(nextTrack.StartTime);
                }
                else if (i == trackList.Count - 1)
                {
                    startTime = ConvertStringToTime(track.StartTime);
                    command = command.Replace("-t {LENGTHSECONDS} ", "");
                }
                else
                {
                    startTime = ConvertStringToTime(track.StartTime);
                    var nextTrack = trackList[i + 1];
                    length = ConvertStringToTime(nextTrack.StartTime) - startTime;

                }
                command = command.Replace("{STARTTIMESECONDS}", startTime.ToString()).Replace("{INPUT}", input).Replace("{LENGTHSECONDS}", length.ToString()).Replace("{OUTPUT}", output);

                var process = new System.Diagnostics.Process();
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.FileName = "./ffmpeg.exe";
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.Arguments = command;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                await process.WaitForExitAsync();

            }

            Console.WriteLine("Done!");
            Console.ReadLine();
        }

    }
}