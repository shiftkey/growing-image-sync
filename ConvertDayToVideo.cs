using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Globalization;

namespace Grow.Update
{
    public class ConvertDayToVideo
    {
        private readonly ILogger _logger;

        public ConvertDayToVideo(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ConvertDayToVideo>();
        }

        [Function("ConvertDayToVideo")]
        public IActionResult Run([HttpTrigger(methods: ["post"], authLevel: AuthorizationLevel.Function)] HttpRequest request)
        {
            _logger.LogInformation($"C# HTTP trigger function executed at: {DateTime.Now}");

            var date = request.Query["date"].FirstOrDefault();

            if (DateTime.TryParseExact(date, "yyyy-mm-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime))
            {
                return new OkObjectResult($"TODO: process images for date: {dateTime}!");
            }
            else
            {
                return new BadRequestObjectResult($"The date received '{date}' was not valid. Try again.");
            }
        }
    }
}