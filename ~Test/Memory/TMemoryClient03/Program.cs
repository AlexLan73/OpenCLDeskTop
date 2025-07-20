// See https://aka.ms/new-console-template for more information
using Common.Enum;
using DMemory.Constants;
using DMemory.Core;
using DMemory.Core.Server;
using System.Data;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Text;
using DMemory.Enum;
using MapCommands = System.Collections.Generic.Dictionary<string, string>;
//Console.WriteLine("Тест протокола с С++");
Console.WriteLine(" Test 003 Memory Client ");

var testClient = new TestClient();
testClient.Run();

Console.WriteLine("Нажмите Enter для завершения работы сервера.");
Console.ReadLine();


class TestClient
{
  private string _memoryName = "CUDA";
  private MemoryMd _mem;
  private CancellationTokenSource cts;

  public TestClient()
  {
    cts = new CancellationTokenSource();
    _mem = new MemoryMd(_memoryName, ClientServer.Client, ParserMap);

  }
  public void Run()
  {
    Console.WriteLine("Нажмите Enter для завершения работы сервера.");
    Console.ReadLine();
    // Имитация записи тестовых метаданных
    var dict = new Dictionary<string, string>
    {
      ["state"] = "client",
    };
    dict["id"] = "not";

    //mdChannel.WriteMetaData(dict);

    int ii = 0;

    while (true)
    {
      dict = new MapCommands()
      {
        [_mem.State] = _mem.NameClient,
        ["id"] = ii.ToString(),
      };

      var _mapTest = _mem.ReadMemoryMd();
      if (_mapTest != null && _mapTest.Count() == 0)
      {
        _mem.WriteInMemoryMd(dict);
        Console.WriteLine($" i = {ii} ");
        ii++;
      }
      System.Threading.Thread.Sleep(800);
    }


    //    cts.Cancel();
  }

  private void ParserMap(MapCommands map)
  {
    PrintMap(map);
/*
    if (map.TryGetValue("id", out string id_value))
    {
      var id = int.Parse(id_value);
      if (id % 3 != 0) return;
      map = new MapCommands
      {
        [_mem.State] = _mem.NameServer,
        ["test"] = "test" + id_value
      };
      _mem.WriteInMemoryMd(map);
    }
*/
  }
  private void PrintMap(MapCommands map)
  {
    foreach (var kv in map)
      Console.WriteLine($" - внешний уровень client == >  {kv.Key} = {kv.Value}");

  }

}


/*
Console.WriteLine("Нажмите Enter для выхода...");
Console.ReadLine();
cts.Cancel();
//mdChannel.Dispose();
*/
