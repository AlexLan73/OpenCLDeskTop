// See https://aka.ms/new-console-template for more information
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

var server = new ServerMetaData(mata);

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
  Thread.Sleep(1000);
  count++;
}

server.Dispose();

await Task.WhenAll(server.WaiteEvent);
