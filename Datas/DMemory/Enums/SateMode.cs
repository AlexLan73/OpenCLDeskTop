namespace DMemory.Enums;

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

public enum TransferWaiting
{
  None,
  Transfer,
  Waiting
}


//// Пример использования:
//var reserved = new Dictionary<string, string>
//{
//  [ ClientServer.Client.AsKey()] = "serverCUDA",
//  [ ClientServer.Server.AsKey()] = "ok"
//};
