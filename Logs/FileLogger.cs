using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace WebSocketServer.Logs
{
    public class FileLogger : ILogger
    {
        protected readonly FileLoggerProvider _fileLoggerProvider;

        private static object _lock = new object();
        public FileLogger([NotNull] FileLoggerProvider fileLoggerProvider)
        {
            this._fileLoggerProvider = fileLoggerProvider;
        }
        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var fileFullPath = string.Format("{0}/{1}", _fileLoggerProvider.Options.FolderPath, _fileLoggerProvider.Options.FilePath.Replace("{date}", DateTime.UtcNow.ToString("yyyyMMdd")));

            if (formatter != null && IsEnabled(logLevel))
            {
                lock (_lock)
                {
                    var logRecord = string.Format("{0} [{1}] {2} {3}", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"), logLevel.ToString(), formatter(state, exception), (exception != null ? exception.StackTrace : ""));
                    
                    Console.WriteLine(logRecord);
                  
                    using (var streamWriter = new StreamWriter(fileFullPath, true))
                    {
                        streamWriter.WriteLine(logRecord);
                    }
                }
            }
        }
    }
}