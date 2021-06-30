using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketServer
{
  public class ConnectedClient
  {
    private static int _counter = 0;
    public ConnectedClient(WebSocket socket, TaskCompletionSource<object> taskCompletion)
    {
      var id = Interlocked.Increment(ref _counter);
      SocketId = id.ToString();
      Socket = socket;
      TaskCompletion = taskCompletion;
    }

    public string SocketId { get; private set; }

    public WebSocket Socket { get; private set; }

    public TaskCompletionSource<object> TaskCompletion { get; private set; }

    public BlockingCollection<string> Messages { get; } = new BlockingCollection<string>();

    public CancellationTokenSource BroadcastLoopTokenSource { get; set; } = new CancellationTokenSource();

    public async Task ClientLoopAsync(ILogger logger)
    {
      var cancellationToken = BroadcastLoopTokenSource.Token;
      while (!cancellationToken.IsCancellationRequested)
      {
        try
        {
          await Task.Delay(Program.BROADCAST_TRANSMIT_INTERVAL_MS, cancellationToken);
          if (!cancellationToken.IsCancellationRequested && Socket.State == WebSocketState.Open && Messages.TryTake(out var message))
          {
            logger.LogInformation($"Socket {SocketId}: Sending.");
            var msgbuf = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message));
            await Socket.SendAsync(msgbuf, WebSocketMessageType.Text, endOfMessage: true, CancellationToken.None);
          }
        }
        catch (Exception ex)
        {
          logger.LogError(ex, ex.Message);
        };
      }
    }
  }
}
