using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using WebSocketServer.Logs;
using Microsoft.Extensions.Logging;

namespace WebSocketServer
{
  public class Startup
  {

    public void ConfigureServices(IServiceCollection services)
    {
      services.AddTransient<WebSocketMiddleware>();

      services.AddHostedService<BroadcastTimestamp>();

      services.AddLogging();

      services.AddSingleton<WebSocketServerConnectionManager>();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
      app.UseWebSockets(new WebSocketOptions
      {
        KeepAliveInterval = TimeSpan.FromSeconds(120),
        ReceiveBufferSize = 4 * 1024
      });

      app.UseMiddleware<WebSocketMiddleware>();
    }
  }
}
