using System.Globalization;
using System.Text.RegularExpressions;
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
        public void Run([TimerTrigger("0 */15 0,11-23 * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            var dateFileFormatRegex = new Regex(@"\d{4}\d{2}\d{2}-\d{2}\d{2}\d{2}");
            var containerName = "images";
            var latestFileName = "latest.jpg";

            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var resource = assembly.GetManifestResourceStream("growing_image_sync.assets.NotoSansMono-Regular.ttf");

            if (resource == null)
            {
                Console.WriteLine($"Unable to find font for rendering, exiting...");
                Environment.Exit(0);
            }

            FontCollection collection = new();
            var family = collection.Add(resource);
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

                var streamForUploading = new MemoryStream();

                using (Image img = Image.Load(initialStream))
                {
                    img.Mutate(ctx => ApplyTimeWatermark(ctx, font, humanFriendlyDateTime, Color.White, 5));
                    img.Save(streamForUploading, new JpegEncoder());
                }

                streamForUploading.Seek(0, SeekOrigin.Begin);

                var latestBlobClient = new BlobClient(connectionString, containerName, latestFileName);
                latestBlobClient.Upload(streamForUploading, true);
            }

            if (myTimer.ScheduleStatus is not null)
            {
                _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
            }
        }

        // Source: https://github.com/SixLabors/Samples/blob/main/ImageSharp/DrawWaterMarkOnImage/Program.cs#L28
        static IImageProcessingContext ApplyTimeWatermark(IImageProcessingContext processingContext,
            Font font,
            string text,
            Color color,
            float padding)
        {
            Size imgSize = processingContext.GetCurrentSize();
            float targetWidth = imgSize.Width - (padding * 2);
            float targetHeight = imgSize.Height - (padding * 2);

            float targetMinHeight = imgSize.Height - (padding * 3); // Must be with in a margin width of the target height

            // Now we are working in 2 dimensions at once and can't just scale because it will cause the text to
            // reflow we need to just try multiple times
            var scaledFont = font;
            FontRectangle s = new FontRectangle(0, 0, float.MaxValue, float.MaxValue);

            float scaleFactor = (scaledFont.Size / 2); // Every time we change direction we half this size
            int trapCount = (int)scaledFont.Size * 2;
            if (trapCount < 10)
            {
                trapCount = 10;
            }

            bool isTooSmall = false;

            while ((s.Height > targetHeight || s.Height < targetMinHeight) && trapCount > 0)
            {
                if (s.Height > targetHeight)
                {
                    if (isTooSmall)
                    {
                        scaleFactor /= 2;
                    }

                    scaledFont = new Font(scaledFont, scaledFont.Size - scaleFactor);
                    isTooSmall = false;
                }

                if (s.Height < targetMinHeight)
                {
                    if (!isTooSmall)
                    {
                        scaleFactor /= 2;
                    }
                    scaledFont = new Font(scaledFont, scaledFont.Size + scaleFactor);
                    isTooSmall = true;
                }
                trapCount--;

                s = TextMeasurer.MeasureSize(text, new TextOptions(scaledFont)
                {
                    WrappingLength = targetWidth
                });

            }

            var center = new PointF(imgSize.Width, imgSize.Height);
            var textOptions = new RichTextOptions(scaledFont)
            {
                Origin = center,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                WrappingLength = targetWidth
            };
            return processingContext.DrawText(textOptions, text, color);
        }
    }
}
