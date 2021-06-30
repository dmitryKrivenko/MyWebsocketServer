using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketServer
{
  internal class BroadcastTimestamp : IHostedService, IDisposable
  {
    private Timer timer;

    private ILogger<BroadcastTimestamp> _logger;

    public BroadcastTimestamp(ILogger<BroadcastTimestamp> logger)
    {
      _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
      var interval = TimeSpan.FromSeconds(Program.TIMESTAMP_INTERVAL_SEC);
      timer = new Timer(QueueBroadcast, null, TimeSpan.Zero, interval);
      return Task.CompletedTask;
    }

    private void QueueBroadcast(object state)
    {
      var message = $"Server time: {DateTimeOffset.Now.ToString("o")}";
      _logger.LogDebug(message);
      WebSocketMiddleware.Broadcast(message);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
      timer?.Change(Timeout.Infinite, 0);
      return Task.CompletedTask;
    }

    public void Dispose()
    {
      timer?.Dispose();
    }
  }
}
