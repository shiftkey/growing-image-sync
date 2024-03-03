using System.Globalization;
using System.Text.RegularExpressions;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SixLabors.Fonts;

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
        public void Run([TimerTrigger("0 */5 7-20 * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            var dateFileFormatRegex = new Regex(@"\d{4}\d{2}\d{2}-\d{2}\d{2}\d{2}");
            var containerName = "images";
            //var latestFileName = "latest.jpg";

            FontCollection collection = new();
            var family = collection.Add("assets/NotoSansMono-Regular.ttf");
            var font = family.CreateFont(12, FontStyle.Regular);


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

                var humanFriendlyDateTime = dt.ToString("h:mm tt - d MMMM", CultureInfo.InvariantCulture);
                Console.WriteLine("Latest timestamp (friendly): " + humanFriendlyDateTime);

                var lastBlobClient = new BlobClient(connectionString, containerName, last.Name);

                var initialStream = new MemoryStream();
                lastBlobClient.DownloadTo(initialStream);
                initialStream.Seek(0, SeekOrigin.Begin);

                // var streamForUploading = new MemoryStream();

                // using (Image img = Image.Load(initialStream))
                // {
                //     img.Mutate(ctx => ApplyScalingWaterMarkWordWrap(ctx, font, humanFriendlyDateTime, Color.White, 5));
                //     img.Save(streamForUploading, new JpegEncoder());
                // }

                //streamForUploading.Seek(0, SeekOrigin.Begin);

                // var latestBlobClient = new BlobClient(connectionString, containerName, latestFileName);
                // latestBlobClient.Upload(streamForUploading, true);
            }


            if (myTimer.ScheduleStatus is not null)
            {
                _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
            }
        }
    }
}
