// ReSharper disable InvalidXmlDocComment
using MapCommands = System.Collections.Generic.Dictionary<string, string>;
namespace DMemory.Core;

public class MemoryMetaDataChannel : IDisposable
{
  private readonly MemoryMappedFile _mmf;
  private readonly MemoryMappedViewAccessor _accessor;
  private readonly EventWaitHandle _eventHandle;
  private readonly int _controlSize = 8192;
  private bool _disposed = false;

  public string Name { get; }
  public string Role { get; } // "server" или "client"

  public MemoryMetaDataChannel(string name, string role)
  {
    Name = name;
    Role = role;
    _mmf = MemoryMappedFile.CreateOrOpen($"{name}Control", _controlSize);
    _accessor = _mmf.CreateViewAccessor();
    /////////
    ///  Только на момент теста
    ///  если Role server
    /// 

//    if (Role == "server") ClearMemoryMd();

    _eventHandle = new EventWaitHandle(false, EventResetMode.AutoReset, $"Global\\Event{name}");
  }

  // Запись в MD (метаданные)
  public void WriteMetaData(Dictionary<string, string> map)
  {
    /*
        // Добавляем информационный ключ: ["server"]=client или ["client"]=server
    //    map[Role] = Role == "server" ? "client" : "server";
        var str = DictToString(map);
        var data = Encoding.UTF8.GetBytes(str);
        _accessor.WriteArray(0, data, 0, data.Length);
        _eventHandle.Set();
    */
    var sData = ConvertDictToString(map);
    using (var accessor = _mmf.CreateViewAccessor())
    {
      var data = Encoding.UTF8.GetBytes(sData);
      accessor.WriteArray(0, data, 0, data.Length);
    }
    _eventHandle.Set();
  }

  public MapCommands ConvertStringToDict(string str)
  {
    if (string.IsNullOrEmpty(str))
      return null;

    return str.Split(';', StringSplitOptions.RemoveEmptyEntries)
      .Select(s => s.Split('=', 2, StringSplitOptions.RemoveEmptyEntries))
      .Where(parts => parts.Length == 2)
      .ToDictionary(parts => parts[0], parts => parts[1]);
  }

  public string ConvertDictToString(MapCommands dic)
  {
    if (dic == null || !dic.Any())
      return null;

    return string.Join(";", dic.Select(x => $"{x.Key}={x.Value}")) + ";";
  }
  // Чтение из MD (метаданные)
  public Dictionary<string, string> ReadMetaData()
  {
    var buf = new byte[_controlSize];
    _accessor.ReadArray(0, buf, 0, buf.Length);
    int actualLen = Array.FindLastIndex(buf, b => b != 0) + 1;
    if (actualLen == 0)
      return new Dictionary<string, string>();
    var s = Encoding.UTF8.GetString(buf, 0, actualLen);
    return StringToDict(s);
  }

  // Асинхронный цикл ожидания событий и обработки метаданных
  public void EventLoop(Action<byte[], Dictionary<string, string>> callback, CancellationToken token)
  {
    while (!token.IsCancellationRequested)
    {
      if (!_eventHandle.WaitOne(1000)) continue;

      var md = ReadMetaData();

      // Обработка handshake и формирования ответа
      if (md.TryGetValue("state", out var state))
      {
        switch (Role)
        {
          // Сервер получил запрос от client
          case "server" when state.StartsWith("client"):
            md["command"] = "ok";
            WriteMetaData(md);
            continue;
          // Клиент получил запрос от server
          case "client" when state.StartsWith("server"):
            md["command"] = "ok";
            WriteMetaData(md);
            continue;
        }
      }

      int size = 0;
      if (md.TryGetValue("size", out var sizeStr))
        int.TryParse(sizeStr, out size);

      byte[] data = Array.Empty<byte>();

      if (size > 0)
        data = ReadBinary(size);

      // Дополнительно: можно сверить checksum и тип данных
      if (md.ContainsKey("crc"))
      {
        // Проверку CRC добавлять тут по необходимости
      }
      if (md.ContainsKey("typedate"))
      {
        // Обработка типа данных (например, десериализация)
      }

      callback(data, md);
      _eventHandle.Reset();
    }
  }

  // Для примера — читает бинарные данные из начала области (нужно доработать в production)
  public byte[] ReadBinary(int size)
  {
    var data = new byte[size];
    _accessor.ReadArray(0, data, 0, Math.Min(size, _controlSize));
    return data;
  }

  private string DictToString(Dictionary<string, string> dict)
      => string.Join(";", dict.Select(kv => $"{kv.Key}={kv.Value}")) + ";";

  private Dictionary<string, string> StringToDict(string s)
  {
    var d = new Dictionary<string, string>();
    foreach (var pair in s.Split(';', StringSplitOptions.RemoveEmptyEntries))
    {
      var parts = pair.Split('=', 2);
      if (parts.Length == 2)
        d[parts[0]] = parts[1];
    }
    return d;
  }

  public void ClearCommandControl()
  {
    Trace.WriteLine("[Команда] Очистка разделяемой памяти...");

    // 1. Получаем доступ к памяти
    using (var accessor = _mmf.CreateViewAccessor())
    {
      // 2. Записываем пустой массив байтов по всему объему памяти
      accessor.WriteArray(0, MemStatic.EmptyBuffer, 0, MemStatic.SizeDataControl);
    }

    // 3. Подаем сигнал, чтобы "разбудить" всех ожидающих читателей
    // Они проснутся и прочитают пустые данные.
    Console.WriteLine("[Команда] Память очищена. Подаю сигнал.");

  }
  public void Dispose()
  {
    if (!_disposed)
    {
      _eventHandle?.Dispose();
      _accessor?.Dispose();
      _mmf?.Dispose();
      _disposed = true;
    }
  }
}

