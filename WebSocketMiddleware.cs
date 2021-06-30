using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketServer
{
  public class WebSocketMiddleware : IMiddleware
  {
    private static WebSocketServerConnectionManager _manager;

    public static CancellationTokenSource SocketLoopTokenSource = new CancellationTokenSource();

    private static bool ServerIsRunning = true;

    private ILogger<WebSocketMiddleware> _logger;

    public WebSocketMiddleware(IHostApplicationLifetime hostLifetime, ILogger<WebSocketMiddleware> logger, WebSocketServerConnectionManager manager)
    {
      _logger = logger;

      _manager = manager;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
      try
      {
        if (ServerIsRunning)
        {
          if (context.WebSockets.IsWebSocketRequest)
          {
            var socket = await context.WebSockets.AcceptWebSocketAsync();
            var completion = new TaskCompletionSource<object>();

            var client = new ConnectedClient(socket, completion);

            _manager.AddClient(client);
            _logger.LogInformation($"Socket {client.SocketId}: New connection.");

            _ = Task.Run(() => SocketProcessingLoopAsync(client, _logger).ConfigureAwait(false));
            await completion.Task;
          }
          else
          {
            if (context.Request.Headers["Accept"][0].Contains("text/html"))
            {
              _logger.LogDebug("Sending HTML to client.");
              await context.Response.WriteAsync(SimpleHtmlClient.HTML);
            }
          }
        }
        else
        {
          context.Response.StatusCode = 409;
        }
      }
      catch (Exception ex)
      {
        context.Response.StatusCode = 500;
        _logger.LogError(ex, $"Internal server error: {0}", ex.Message);
      }
      finally
      {
        if (!context.Response.HasStarted)
          await next(context);
      }
    }

    public static void Broadcast(string message)
    {
      if (_manager != null)
      {
        foreach (var kvp in _manager?.GetAllSockets())
          kvp.Value.Messages.Add(message);
      };
    }

    private static async Task SocketProcessingLoopAsync(ConnectedClient client, ILogger<WebSocketMiddleware> logger)
    {
      _ = Task.Run(() => client.ClientLoopAsync(logger).ConfigureAwait(false));

      var socket = client.Socket;
      var loopToken = SocketLoopTokenSource.Token;
      var broadcastTokenSource = client.BroadcastLoopTokenSource;
      try
      {
        var buffer = WebSocket.CreateServerBuffer(4096);
        while (socket.State != WebSocketState.Closed && socket.State != WebSocketState.Aborted && !loopToken.IsCancellationRequested)
        {
          var receiveResult = await client.Socket.ReceiveAsync(buffer, loopToken);

          if (!loopToken.IsCancellationRequested)
          {
            if (client.Socket.State == WebSocketState.CloseReceived && receiveResult.MessageType == WebSocketMessageType.Close)
            {
              logger.LogInformation($"Socket {client.SocketId}: closing");
              broadcastTokenSource.Cancel();
              await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Socket closed", CancellationToken.None);
            }

            if (client.Socket.State == WebSocketState.Open)
            {
              logger.LogInformation($"Socket {client.SocketId}: Received {receiveResult.MessageType} frame ({receiveResult.Count} bytes).");
              logger.LogInformation($"Socket {client.SocketId}: Echoing data.");
              string message = Encoding.UTF8.GetString(buffer.Array, 0, receiveResult.Count);
              client.Messages.Add(message);
            }
          }
        }
      }
      catch (Exception ex)
      {
        logger.LogError(ex, $"Socket {client.SocketId}:");
      }
      finally
      {
        broadcastTokenSource.Cancel();

        logger.LogInformation($"Socket {client.SocketId}: Ended processing in state {socket.State}");

        if (client.Socket.State != WebSocketState.Closed)
          client.Socket.Abort();

        if (_manager.RemoveClient(client))
          socket.Dispose();

        client.TaskCompletion.SetResult(true);
      }
    }
  }
}
