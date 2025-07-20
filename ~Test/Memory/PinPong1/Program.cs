// See https://aka.ms/new-console-template for more information
using DMemory.Core;
using DMemory.Enum;
using DryIoc.ImTools;
using System.Reactive.Concurrency;
using Windows.Media.Protection.PlayReady;
using static Microsoft.IO.RecyclableMemoryStreamManager;
using MapCommands = System.Collections.Generic.Dictionary<string, string>;


Console.WriteLine(" Тестируем Пин-Понг по MetaData ");
MetaSettings _mata = new("CUDA");
int _count = 0;

var server = new ServerPong(_mata);
var client = new ClientPing(_mata);

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

  Console.WriteLine($"Tick: {DateTime.Now:HH:mm:ss}  &  count {_count}");
  Thread.Sleep(1000);
  _count++;
}

server.Dispose();
client.Dispose();

await Task.WhenAll(server._waiteEvent, client._waiteEvent);


public class ServerPong : IDisposable
{
  private MetaSettings _metaSettings;
  public BasicMemoryMd md;
  public EventWaitHandle sendToClient;
  private readonly CancellationTokenSource _cts;                 // завершение потока
  public readonly Task _waiteEvent;
  private Action<MapCommands> _callBack;            // Инициализация пустым делегатом передать данные на верх
  private readonly string _nameModule;
  public ServerPong(MetaSettings metaSettings) 
  { 
    _metaSettings = metaSettings;
    _nameModule = "server"+ _metaSettings.MemoryName;
    _cts = new CancellationTokenSource();
    var token = _cts.Token;
    sendToClient = new EventWaitHandle(false, EventResetMode.AutoReset, _metaSettings.MetaEventClient);
    md = new BasicMemoryMd(_metaSettings.MetaEventServer, _metaSettings.MetaSize, _metaSettings.ControlName, PrintMapServer, sendToClient);
    _waiteEvent = ReadDataCallBack(token);
  }
  private void PrintMapServer(MapCommands map)
  {
    foreach (var kv in map)
      Console.WriteLine($" - внешний уровень server == >  {kv.Key} = {kv.Value}");
  }

  private async Task ReadDataCallBack(CancellationToken cancellationToken = default)
  {
    await Task.Run(() =>
    {
      int i = 0;
      try
      {

        while (!cancellationToken.IsCancellationRequested)
        {
          Thread.Sleep(1000);
          var map = new MapCommands()
          {
            [MdCommand.State.AsKey()] = _nameModule,
            ["id_server"]=i.ToString(),
          };
          md.WriteMetaMap(map);   // пишем pong
          i++;
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Ошибка в ReadDataCallBack: {ex}");
        _callBack?.Invoke(null);
      }
    }, cancellationToken);
  }


  public void Dispose()
  {
    Console.WriteLine($"ServerPong  -- Dispose() ");

    _cts.Cancel();
    Task.WaitAll(_waiteEvent);
    md?.Dispose();
  }

}

public class ClientPing
{
  private MetaSettings _metaSettings;
  public BasicMemoryMd md;
  public EventWaitHandle sendToServer;
  private readonly CancellationTokenSource _cts;                 // завершение потока
  public readonly Task _waiteEvent;
  private Action<MapCommands> _callBack;            // Инициализация пустым делегатом передать данные на верх
  public readonly string _nameModule;

  public ClientPing(MetaSettings metaSettings) 
  { 
    _metaSettings = metaSettings;
    _nameModule = "client" + _metaSettings.MemoryName;

    _cts = new CancellationTokenSource();
    var token = _cts.Token;
    sendToServer = new EventWaitHandle(false, EventResetMode.AutoReset, _metaSettings.MetaEventServer);

    md = new BasicMemoryMd(_metaSettings.MetaEventClient, _metaSettings.MetaSize, _metaSettings.ControlName, PrintMapClient, sendToServer);
    _waiteEvent = ReadDataCallBack(token);
  }
  private async Task ReadDataCallBack(CancellationToken cancellationToken = default)
  {
    await Task.Run(() =>
    {
      int i = 0;
      try
      {
        var map1 = new MapCommands()
        {
          [MdCommand.State.AsKey()] = _nameModule,
          ["id_client"] = i.ToString(),
        };
        md.WriteMetaMap(map1);   // пишем pong
//        sendToServer.Set();

        while (!cancellationToken.IsCancellationRequested)
        {
          Thread.Sleep(1000);
          Console.WriteLine($"ClientPing");
          var map = new MapCommands()
          {
            [MdCommand.State.AsKey()] = _nameModule,
            ["id_client"] = i.ToString(),
          };
          md.WriteMetaMap(map);   // пишем pong
          i++;
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Ошибка в ReadDataCallBack: {ex}");
        _callBack?.Invoke(null);
      }
    }, cancellationToken);
  }
                                                     

  private void PrintMapClient(MapCommands map)
  {
    foreach (var kv in map)
      Console.WriteLine($" - внешний уровень client == >  {kv.Key} = {kv.Value}");
  }

  public void Dispose()
  {
    Console.WriteLine($"ClientPing  -- Dispose() ");

    _cts.Cancel();
    Task.WaitAll(_waiteEvent);
    md?.Dispose();

  }
}



//public async Task RunAsync()
//{

//  for (int i = 1; i <= 10; i++)
//  {
//    var msg = $"ping {i}";
//    md.WriteMeta(msg);
//    sendToServer.Set(); // триггерим сервер

//    var reply = md.WaitAndRead(); // ждем ответ
//    Console.WriteLine($"  [{_metaSettings.MemoryName}]   [Client] Received: {reply}");
//    await Task.Delay(500);
//  }
//}


/*
 
 using System.IO.MemoryMappedFiles;
using System.Text;

Console.WriteLine("Hello, World!");

MetaSettings _mata = new("CUDA");

// Пример использования в Main
var server =  Task.Run(() => new ServerPong(_mata).RunAsync());
var client = Task.Run(() => new ClientPing(_mata).RunAsync());
await Task.WhenAll(server, client);


public class ServerPong
{
  private MetaSettings _metaSettings;

  public ServerPong(MetaSettings metaSettings)=>_metaSettings = metaSettings;
  public async Task RunAsync()
  {
    using var md = new BasicMemoryMd("Server", _metaSettings.MetaEventServer, _metaSettings.MetaSize, _metaSettings.ControlName);
    using var sendToClient = new EventWaitHandle(false, EventResetMode.AutoReset, _metaSettings.MetaEventClient);

    for (int i = 1; i <= 10; i++)
    {
      var msg = md.WaitAndRead(); // ждём ping
      var reply = msg.Replace("ping", $" [{_metaSettings.MemoryName}]  Server pong");
      await Task.Delay(500); // имитируем обработку
      md.WriteMeta(reply);   // пишем pong
      sendToClient.Set();    // сигнал клиенту
    }
  }
}

public class ClientPing
{
  private MetaSettings _metaSettings;
  public ClientPing(MetaSettings metaSettings) => _metaSettings = metaSettings;
  public async Task RunAsync()
  {
    using var md = new BasicMemoryMd("Client", _metaSettings.MetaEventClient, _metaSettings.MetaSize, _metaSettings.ControlName);
    using var sendToServer = new EventWaitHandle(false, EventResetMode.AutoReset, _metaSettings.MetaEventServer);

    for (int i = 1; i <= 10; i++)
    {
      var msg = $"ping {i}";
      md.WriteMeta(msg);
      sendToServer.Set(); // триггерим сервер

      var reply = md.WaitAndRead(); // ждем ответ
      Console.WriteLine($"  [{_metaSettings.MemoryName}]   [Client] Received: {reply}");
      await Task.Delay(500);
    }
  }
}

public class BasicMemoryMd : IDisposable
{
  private readonly string _eventName;
  private readonly MemoryMappedFile _mmf;
  private readonly EventWaitHandle _event;
  private readonly string _role;
  private readonly int _size;
  private readonly string _contrName;

  public BasicMemoryMd(string role, string eventName, int size, string contrName)
  {
    _role = role;
    _eventName = eventName;
    _size = size;
    _contrName = contrName;
    _mmf = MemoryMappedFile.CreateOrOpen(_contrName, size);
    _event = new EventWaitHandle(false, EventResetMode.AutoReset, eventName);
  }

  public void WriteMeta(string text)
  {
    using var accessor = _mmf.CreateViewAccessor();
    var data = Encoding.UTF8.GetBytes(text);
    accessor.WriteArray(0, data, 0, data.Length);
    Console.WriteLine($"[{_role}] >>> META записано: {text}");
    _event.Set();
  }

  public string WaitAndRead()
  {
    _event.WaitOne();
    using var accessor = _mmf.CreateViewAccessor();
    byte[] buffer = new byte[_size];
    accessor.ReadArray(0, buffer, 0, buffer.Length);
    var result = Encoding.UTF8.GetString(buffer).TrimEnd('\0');
    Console.WriteLine($"[{_role}] <<< META прочитано: {result}");
    return result;
  }

  public void Dispose()
  {
    _event?.Dispose();
    _mmf?.Dispose();
  }
}


public class MetaSettings
{
  public string MemoryName { get; }
  public int MetaSize { get; }

  public string MetaEventServer => $"Global\\EventServer{MemoryName}";
  public string MetaEventClient => $"Global\\EventClientMeta{MemoryName}";
  public string ControlName => $"{MemoryName}Control";

  public MetaSettings(string name, int metaSize = 8192)
  {
    MemoryName = name ?? throw new ArgumentNullException(nameof(name));
    MetaSize = metaSize > 0 ? metaSize : throw new ArgumentOutOfRangeException(nameof(metaSize));
  }
}


 
 */
