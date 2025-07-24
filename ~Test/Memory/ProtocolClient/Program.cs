

using DMemory.Core;
using DMemory.Enum;
using DryIoc.ImTools;
using System.Reactive.Concurrency;
using Windows.Media.Protection.PlayReady;
using static Microsoft.IO.RecyclableMemoryStreamManager;
using MapCommands = System.Collections.Generic.Dictionary<string, string>;

Console.WriteLine(" Тестируем Протокол по MetaData  со стороны CLIENT");
MetaSettings mata = new("CUDA");
int count = 0;

var client = new ClientMetaData(mata);

while (true)
{
  // Проверяем: нажата ли клавиша?
  if (Console.KeyAvailable)
  {
    var key = Console.ReadKey(intercept: true);
    if (key.Key == ConsoleKey.Escape)
    {
      Console.WriteLine("Выход из цикла по ESC.");
      break;
    }
  }

  Console.WriteLine($"Tick: {DateTime.Now:HH:mm:ss}  &  count {count}");
  Console.WriteLine($"STATE MODE");
  Console.WriteLine($"[Server] -> {client._mode}   [ПЕРЕДАЧА] {client._transferWaiting} ");
    Console.WriteLine($"[Client] -> {client._mode}   {client._transferWaiting} ");
  
    if (client._mode == SateMode.Work && client._transferWaiting == TransferWaiting.Transfer)
    {
      var map1 = new MapCommands()
      {
        [MdCommand.State.AsKey()] = "clientCUDA",
        ["id_client"] = count.ToString(),
      };
      client.WriteMetaMap(map1);   // пишем pong
      client._transferWaiting = TransferWaiting.Waiting;
    }


  Thread.Sleep(1000);
  count++;
}

client.Dispose();

await Task.WhenAll(client.WaiteEvent);