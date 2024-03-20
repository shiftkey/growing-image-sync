using System.Globalization;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using static Grow.Watermark;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace Grow.Update
{
    public class UpdateLatestImageWithLabel
    {
        private readonly ILogger _logger;

        public UpdateLatestImageWithLabel(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<UpdateLatestImageWithLabel>();
        }

        [Function("UpdateLatestImageWithLabel")]
        public async Task Run([TimerTrigger("0 3,18,32,48 10-23 * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            var dateFileFormatRegex = new Regex(@"\d{4}\d{2}\d{2}-\d{2}\d{2}\d{2}");
            var containerName = "images";
            var latestFileName = "latest.jpg";

            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var resource = assembly.GetManifestResourceStream("Grow.assets.NotoSansMono-Regular.ttf");

            if (resource == null)
            {
                Console.WriteLine($"Unable to find font for rendering, exiting...");
                Environment.Exit(0);
            }

            FontCollection collection = new();
            var family = collection.Add(resource);
            var font = family.CreateFont(14, FontStyle.Regular);

            var connectionString = Environment.GetEnvironmentVariable("BLOB_STORAGE_CONNECTION_STRING");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                Console.WriteLine($"No BLOB_STORAGE_CONNECTION_STRING environment variable set, ignoring...");
                Environment.Exit(0);
            }
            else
            {
                var containerClient = new BlobContainerClient(connectionString, containerName);

                var blobs = containerClient.GetBlobs(Azure.Storage.Blobs.Models.BlobTraits.None, Azure.Storage.Blobs.Models.BlobStates.None, "image-");
                var last = blobs.Last();
                Console.WriteLine("Latest blob:" + last.Name);

                Match m = dateFileFormatRegex.Match(last.Name);
                if (!m.Success)
                {
                    Console.WriteLine($"Unable to extract date time value from file name: " + last.Name);
                    Environment.Exit(0);
                }

                var dt = DateTime.ParseExact(m.Value, "yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
                Console.WriteLine("Latest timestamp: " + dt);

                var humanFriendlyTime = dt.ToString("h:mm tt", CultureInfo.InvariantCulture);
                var humanFriendlyDate = dt.ToString("d MMMM", CultureInfo.InvariantCulture);
                Console.WriteLine("Latest timestamp (friendly): {0} - {1}", humanFriendlyTime, humanFriendlyDate);

                var lastBlobClient = new BlobClient(connectionString, containerName, last.Name);

                var initialStream = new MemoryStream();
                lastBlobClient.DownloadTo(initialStream);
                initialStream.Seek(0, SeekOrigin.Begin);

                var streamForUploading = new MemoryStream();

                using (Image img = Image.Load(initialStream))
                {
                    img.Mutate(ctx => ApplyToImage(ctx, font, humanFriendlyTime, Color.White, 10, firstRow: true));
                    img.Mutate(ctx => ApplyToImage(ctx, font, humanFriendlyDate, Color.White, 10));
                    img.Save(streamForUploading, new JpegEncoder());
                }

                streamForUploading.Seek(0, SeekOrigin.Begin);

                var latestBlobClient = new BlobClient(connectionString, containerName, latestFileName);
                latestBlobClient.Upload(streamForUploading, true);
            }

            await PostToFrontend();

            if (myTimer.ScheduleStatus is not null)
            {
                _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
            }
        }

        async Task PostToFrontend()
        {
            try
            {
                var endpoint = Environment.GetEnvironmentVariable("CALLBACK_URL");
                var bearerToken = Environment.GetEnvironmentVariable("CALLBACK_BEARER_TOKEN");
                if (string.IsNullOrWhiteSpace(bearerToken) || string.IsNullOrWhiteSpace(endpoint))
                {
                    _logger.LogInformation($"Check CALLBACK_URL and CALLBACK_BEARER_TOKEN are set, not making callback to signal new image available...");
                }
                else
                {
                    var client = new HttpClient();
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
                    var response = await client.PostAsync(endpoint, null);
                    _logger.LogInformation($"Callback response found with status: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unable to make callback request: {ex}");
            }
        }
    }
}
