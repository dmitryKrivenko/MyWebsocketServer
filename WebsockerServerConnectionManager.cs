using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.WebSockets;

namespace WebSocketServer
{
  public class WebSocketServerConnectionManager
  {
    private ConcurrentDictionary<string, ConnectedClient> _clients = new ConcurrentDictionary<string, ConnectedClient>();

    private ILogger<WebSocketServerConnectionManager> _logger;

    public WebSocketServerConnectionManager(ILogger<WebSocketServerConnectionManager> logger)
    {
      _logger = logger;
    }

    public string AddClient(ConnectedClient client)
    {
      _clients.TryAdd(client.SocketId, client);
      _logger.LogInformation($"WebSocketServerConnectionManager-> AddClient: Client added with ID: {client.SocketId}");
      return client.SocketId;
    }

    public bool RemoveClient(ConnectedClient client)
    {
      return _clients.TryRemove(client.SocketId, out _);
    }

    public ConcurrentDictionary<string, ConnectedClient> GetAllSockets()
    {
      return _clients;
    }
  }
}