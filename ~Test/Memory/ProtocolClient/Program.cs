

using DMemory.Core;
using DMemory.Enum;
using DryIoc.ImTools;
using System.Reactive.Concurrency;
using Windows.Media.Protection.PlayReady;
using Common.Core.Channel;
using DMemory.Core.Channel;
using static Microsoft.IO.RecyclableMemoryStreamManager;
using MapCommands = System.Collections.Generic.Dictionary<string, string>;

Console.WriteLine(" Тестируем Протокол по MetaData  со стороны CLIENT");
MetaSettings mata = new("CUDA");
int count = 0;


var processor = new MemoryDataProcessor(mata.MemoryName, HandleReceivedData);

var client = new ClientMetaData(mata, processor);

bool _isSnd = true;
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

      if (_isSnd)
      {
        var x = CreateDtVariableChannel(count);
        var ramData = new RamData(x, typeof(DtVariableChannel), new MapCommands());
        client.EnqueueToSend(ramData);
      }

    }


  Thread.Sleep(1000);
  count++;
}

client.Dispose();

await Task.WhenAll(client.WaiteEvent);

// Метод генерации одного экземпляра с заданным int id
DtVariableChannel CreateDtVariableChannel(int id)
{
  var rnd = new Random();

  uint convertedId = unchecked((uint)id); // Преобразуем int в uint (если id всегда положительное — можно просто к uint)

  ulong tik = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); // Можно использовать тики времени в миллисекундах

  double value = rnd.NextDouble() * (100 - 30) + 30; // случайное double от 30 до 100

  return new DtVariableChannel(
    convertedId,
    new DtValues(tik, value)
  );
}
void HandleReceivedData(RamData data)
{
  // Логика обработки данных сверху
  Console.WriteLine($"Received data of type: {data.DataType.Name}");
  // Например обработать данные, передать дальше и т.п.
//  Console.WriteLine($"Id: {example.Id}, Tik: {example.Values.Tik}, Value: {example.Values.Values:F2}");
}
