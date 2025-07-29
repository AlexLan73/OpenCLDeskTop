

using DMemory.Core;
using Common.Core.Channel;
using DMemory.Core.Channel;
using DMemory.Core.Test;
using DMemory.Enums;
using static Microsoft.IO.RecyclableMemoryStreamManager;
using MapCommands = System.Collections.Generic.Dictionary<string, string>;

Console.WriteLine(" Тестируем Протокол по MetaData  со стороны CLIENT");
MetaSettings mata = new("CUDA");
int count = 0;
var processor = new MemoryDataProcessor(mata.MemoryName, HandleReceivedData);

var client = new ClientMetaData(mata, processor);
var _test = new TestDataFactory();

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

/*
     public TransferWaiting GeTransferWaiting() => _transferWaiting;
   public SateMode GetSateMode() =>  _mode;

 
 */
//  Console.WriteLine($"Tick: {DateTime.Now:HH:mm:ss}  &  count {count}");
//  Console.WriteLine($"STATE MODE");
  Console.WriteLine($"[Client] -> {client.GetSateMode()}   [ПЕРЕДАЧА] {client.GeTransferWaiting()} ");
//    Console.WriteLine($"[Client] -> {client.GetSateMode()}   {client.GeTransferWaiting()} ");

  if (client.GetSateMode() == SateMode.Work && client.GeTransferWaiting() == TransferWaiting.Transfer)
  {


    //var map1 = new MapCommands()
    //  {
    //    [MdCommand.State.AsKey()] = "clientCUDA",
    //    ["id_client"] = count.ToString(),
    //  };
    //  var ramDataTest01 = new RamData(null, null, map1);
    //  await client.EnqueueToSendAsync(ramDataTest01);

    /*
          if (_isSnd && count%5==4)
          {
                  //_isSnd = false;
          Console.WriteLine($" [Client] ->    Tick: {DateTime.Now:HH:mm:ss}  &  count {count}");
            var x = CreateDtVariableChannel(count);
            var ramData = new RamData(x, typeof(DtVariableChannel), new MapCommands());
     //       var ramData1 = new RamData(null, null, new MapCommands());
            await client.EnqueueToSendAsync(ramData);

          }
    */

    if (_isSnd)
    {
      int ind = count % 4;
      RamData ramData = null;
      switch (ind)
      {
        case 1:
        {
          var x = _test.CreateDtVariable(count);
          ramData = new RamData(x, typeof(IdDataTimeVal), new MapCommands());
          break;
        }
        case 2:
        {
          var x = _test.CreateVDtValues(count, 10);
          ramData = new RamData(x, typeof(VIdDataTimeVal), new MapCommands());
          break;
        }
        case 3:
        {
          var x = _test.CreateLoggerBase(count);
          ramData = new RamData(x, typeof(LoggerBase), new MapCommands());
          break;
        }

      }
      if(ramData != null) 
        await client.EnqueueToSendAsync(ramData);

    }
  }

  Thread.Sleep(50);
  count++;
}

client.Dispose();

//await Task.WhenAll(client.WaiteEvent);

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
  Console.WriteLine($"[CLIENT]  Received data of type: {data.DataType.Name}");
  // Например обработать данные, передать дальше и т.п.
//  Console.WriteLine($"Id: {example.Id}, Tik: {example.Values.Tik}, Value: {example.Values.Values:F2}");
}
