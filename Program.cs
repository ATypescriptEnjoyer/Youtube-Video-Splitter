using System;
using System.IO;
using System.Threading.Tasks;

namespace YT_Playlist_DL_Splitter
{
    class Program
    {
        static async Task Main(string[] args)
        {

            var videoUrl = GetVideoUrl();
            var storagePath = GetStoragePath();

            var processor = new YoutubeProcessor(videoUrl, storagePath);
            await processor.Start();
        }

        static string GetVideoUrl()
        {
            Console.WriteLine("Enter Video URL");
            var videoUrl = Console.ReadLine();
            if (!videoUrl.ToLower().Contains("youtube"))
            {
                Console.WriteLine("That's not a YouTube URL :(");
                Console.ReadLine();
                Console.Clear();
                return GetVideoUrl();
            }
            return videoUrl;
        }

        static string GetStoragePath()
        {
            Console.WriteLine("Where should we store the output folder? (Provide Full Path)");
            var saveLocation = Console.ReadLine();
            if (!Directory.Exists(saveLocation))
            {
                Console.WriteLine("That directory doesn't exist!");
                return GetStoragePath();
            }
            return saveLocation;
        }
    }
}
