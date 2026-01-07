using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace PosterWall
{
    public class PosterWallService : IHostedService, IDisposable
    {
        private Timer _timer;
        private readonly HttpClient _httpClient = new HttpClient();
        private readonly string _outputFile = Path.Combine("wwwroot", "poster_wall.jpg");
        private readonly string _apiKey = "YOUR_JELLYFIN_API_KEY"; // <- replace this
        private readonly string _serverUrl = "http://localhost:8096"; // <- replace this

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Run immediately, then every 6 hours
            _timer = new Timer(GeneratePosterWall, null, TimeSpan.Zero, TimeSpan.FromHours(6));
            return Task.CompletedTask;
        }

        private async void GeneratePosterWall(object state)
        {
            try
            {
                // Fetch movies
                string url = $"{_serverUrl}/Items?IncludeItemTypes=Movie&Fields=PrimaryImage&api_key={_apiKey}";
                var json = await _httpClient.GetStringAsync(url);
                using var doc = JsonDocument.Parse(json);

                var items = doc.RootElement.GetProperty("Items");
                var posterUrls = new System.Collections.Generic.List<string>();

                foreach (var item in items.EnumerateArray())
                {
                    if (item.TryGetProperty("ImageTags", out var tags) && tags.TryGetProperty("Primary", out _))
                    {
                        string posterUrl = $"{_serverUrl}/Items/{item.GetProperty("Id").GetString()}/Images/Primary?api_key={_apiKey}";
                        posterUrls.Add(posterUrl);
                    }
                }

                // Download posters locally
                string tempFolder = Path.Combine("wwwroot", "temp_posters");
                Directory.CreateDirectory(tempFolder);
                int index = 0;
                foreach (var poster in posterUrls)
                {
                    try
                    {
                        var bytes = await _httpClient.GetByteArrayAsync(poster);
                        File.WriteAllBytes(Path.Combine(tempFolder, $"poster_{index}.jpg"), bytes);
                        index++;
                    }
                    catch { /* skip failed downloads */ }
                }

                // Build collage
                var posterFiles = Directory.GetFiles(tempFolder, "*.jpg");
                CollageGenerator.CreateCollage(posterFiles, _outputFile);

                // Optional: clean up temp posters
                Directory.Delete(tempFolder, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PosterWall Error: {ex.Message}");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose() => _timer?.Dispose();
    }
}
