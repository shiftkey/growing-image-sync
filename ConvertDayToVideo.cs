using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

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

            return new OkObjectResult($"Welcome to Azure Functions, {request.Query["name"]}!");
        }
    }

}