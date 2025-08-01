// See https://aka.ms/new-console-template for more information

using Common.Core.Channel;
using Data.Core;
using DMemory.Core.Test;
using MapCommands = System.Collections.Generic.Dictionary<string, string>;

Console.WriteLine("Запуск в DataContext  CUDAModule ");
Console.WriteLine(" Test на прием данных от С++  и пересылка управляющих данных в С++ ");

var _cudaServer = new CUDAModule();
var _test = new TestDataFactory();

int count=0;
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
  Console.WriteLine($" DsProg--->  {DateTime.Now:HH:mm:ss} [DcServer] ");

  if (_isSnd)
  {
    int ind = count % 4;
    RamData ramData = null;
    switch (ind)
    {
      case 10:
      {
        var x = _test.CreateDtVariable(count);
        ramData = new RamData(x, typeof(IdDataTimeVal), new MapCommands());
        break;
      }
      case 20:
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
    if (ramData != null)
      await _cudaServer.EnqueueToSendAsync(ramData);

  }


  Thread.Sleep(973);
  count++;

}

Console.ReadLine();
