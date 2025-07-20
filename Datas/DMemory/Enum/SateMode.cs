using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DMemory.Enum;

public enum SateMode
{
  None,
  Initialization,
  Work,
  Dispose
}

public enum ClientServer
{
  Client,
  Server
}

public static class ClientServerExtensions
{
  public static string AsKey(this ClientServer cmd) => cmd switch
  {
    ClientServer.Client => "client",
    ClientServer.Server => "server",
    _ => ""
  };
}
                                                                
//// Пример использования:
//var reserved = new Dictionary<string, string>
//{
//  [ ClientServer.Client.AsKey()] = "serverCUDA",
//  [ ClientServer.Server.AsKey()] = "ok"
//};
