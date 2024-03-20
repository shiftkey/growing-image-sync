using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Grow
{
    // Adapted from https://dev.to/jakubkwa/hacking-idisposable-the-stopwatch-example-1ja0
    public class AutoStopwatch : IDisposable
    {
        private readonly Stopwatch _stopwatch;
        private readonly ILogger _logger;
        private readonly string _action;

        public AutoStopwatch(ILogger logger, string action)
        {
            _logger = logger;
            _action = action;
            _stopwatch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            _logger.LogInformation("Action '{0}' tooks {1}ms to complete", _action, _stopwatch.ElapsedMilliseconds);
        }
    }
}