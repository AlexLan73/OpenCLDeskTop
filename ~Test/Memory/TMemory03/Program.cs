// See https://aka.ms/new-console-template for more information
using Common.Enum;
using DMemory.Constants;
using DMemory.Core;
using DMemory.Core.Server;
using System.Data;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Threading;
using DMemory.Enums;
using MapCommands = System.Collections.Generic.Dictionary<string, string>;

Console.WriteLine("Тест протокола с С++");
Console.WriteLine(" Test 003 Memory SERVER ");
var _server = new TestServer();
_server.Run();

Console.WriteLine("Нажмите Enter для завершения работы сервера.");
Console.ReadLine();


class TestServer
{
  private string _memoryName = "CUDA";
  private MemoryMd _mem;
  private CancellationTokenSource cts;

  public TestServer()
  {
    cts = new CancellationTokenSource();
    _mem = new MemoryMd(_memoryName, ClientServer.Server, ParserMap);

  }
  public void Run()
  {


//    cts.Cancel();
  }

  private void ParserMap(MapCommands map)
  {
    PrintMap(map);
    if (map.TryGetValue("id", out string id_value))
    {
      var id = int.Parse(id_value);
      //if (id % 3 != 0) return;
      //map = new MapCommands
      //{
      //  [_mem.State] = _mem.NameServer,
      //  ["test"] = "test -> внешний server send  " + id_value
      //};
      //_mem.WriteInMemoryMd(map);
    }

  }
  private void PrintMap(MapCommands map)
  {
    foreach (var kv in map)
      Console.WriteLine($" - внешний уровень server == >  {kv.Key} = {kv.Value}");

  }

}


// Запуск event loop в отдельном потоке
/*
Task.Run(() =>
{
  mdChannel.EventLoop((data, md) =>
  {
    Console.WriteLine("CALLBACK с данными:");
    foreach (var kv in md)
      Console.WriteLine($"  {kv.Key} = {kv.Value}");
    Console.WriteLine("Data length: " + data.Length);
  }, cts.Token);
});
*/
/*
// Имитация записи тестовых метаданных
var dict = new Dictionary<string, string>
{
  ["state"] = role == "server" ? "serverTestModule" : "clientTestModule",
  ["size"] = "10",
  ["crc"] = "AB12CD34",
  ["typedate"] = "UInt32Array"
};
*/
// Имитация записи тестовых метаданных
//var dict = new Dictionary<string, string>
//{
//  ["state"] = role == "server" ? "server" : "client",

//  //  ["size"] = "10",
//  //  ["crc"] = "AB12CD34",
//  //  ["typedate"] = "UInt32Array"
//};

//mdChannel.WriteMetaData(dict);


//while (true)
//{
// var _mmd = _mem.ReadMemoryMd();
// foreach (var kv in _mmd)
//   Console.WriteLine($" -->  {kv.Key} = {kv.Value}");
//  System.Threading.Thread.Sleep(500);

//}

