using System;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using WebSocketServer.Logs;
using Microsoft.Extensions.Configuration;


namespace WebSocketServer
{
    public class Program
    {
        public const int TIMESTAMP_INTERVAL_SEC = 15;
        public const int BROADCAST_TRANSMIT_INTERVAL_MS = 250;
        public const int CLOSE_SOCKET_TIMEOUT_MS = 2500;

        public static void Main(string[] args)
        {
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseUrls(new string[] { @"http://localhost:8080/" });
                    webBuilder.UseStartup<Startup>();
                })
                .ConfigureLogging((context, logging) =>
                {
                  logging.AddFileLogger(options =>
                      context.Configuration.GetSection("Logging").GetSection("WebsocketServerFile").GetSection("Options").Bind(options)
                      );
                })
                .Build()
                .Run();
        }
    }
}
