// See https://aka.ms/new-console-template for more information
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

//using var md = new BasicMemoryMd("Server", _metaSettings.MetaEventServer, _metaSettings.MetaSize, _metaSettings.ControlName);
//using var sendToClient = new EventWaitHandle(false, EventResetMode.AutoReset, _metaSettings.MetaEventClient);
//using var md = new BasicMemoryMd("Client", _metaSettings.MetaEventClient, _metaSettings.MetaSize, _metaSettings.ControlName);
//using var sendToServer = new EventWaitHandle(false, EventResetMode.AutoReset, _metaSettings.MetaEventServer);

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


