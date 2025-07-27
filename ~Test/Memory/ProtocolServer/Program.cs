// See https://aka.ms/new-console-template for more information
using Common.Core.Channel;
using DMemory.Core;
using DMemory.Enum;
using DryIoc.ImTools;
using System.Reactive.Concurrency;
using Windows.Media.Protection.PlayReady;
using static Microsoft.IO.RecyclableMemoryStreamManager;
using MapCommands = System.Collections.Generic.Dictionary<string, string>;


Console.WriteLine(" Тестируем Протокол по MetaData  со стороны SERVER");
MetaSettings mata = new("CUDA");
int count = 0;
var processor = new MemoryDataProcessor(mata.MemoryName, HandleReceivedData);

var server = new ServerMetaData(mata, processor);
//var client = new ClientMetaData(mata);

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
  Console.WriteLine($"[Server] -> {server.GetSateMode()}   [ПЕРЕДАЧА] {server.GetSateMode()} ");
  //  Console.WriteLine($"[Client] -> {client._mode}   {client._transferWaiting} ");
  /*
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
  */

  if (server.GetSateMode() == SateMode.Work && server.GeTransferWaiting() == TransferWaiting.Transfer)
  {
    var map1 = new MapCommands()
    {
      [MdCommand.State.AsKey()] = "serverCUDA",
      ["id_server"] = count.ToString(),
    };
    var ramDataTest01 = new RamData(null, null, map1);
    await server.EnqueueToSendAsync(ramDataTest01);

  }

  Thread.Sleep(1000);
  count++;
}

server.Dispose();

Console.ReadLine();
//await Task.WhenAll(server.WaiteEvent);

void HandleReceivedData(RamData data)
{
  // Логика обработки данных сверху
  Console.WriteLine($" [SERVER] Received data of type: {data.DataType.Name}");
  // Например обработать данные, передать дальше и т.п.
  //  Console.WriteLine($"Id: {example.Id}, Tik: {example.Values.Tik}, Value: {example.Values.Values:F2}");
}
