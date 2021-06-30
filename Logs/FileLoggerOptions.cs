using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebSocketServer.Logs
{
  public class FileLoggerOptions
  {
    public virtual string FilePath { get; set; }

    public virtual string FolderPath { get; set; }

  }
}
