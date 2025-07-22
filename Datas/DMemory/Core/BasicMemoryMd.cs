using MapCommands = System.Collections.Generic.Dictionary<string, string>;

namespace DMemory.Core;

public class BasicMemoryMd : IDisposable
{
  private readonly MemoryMappedFile _mmf;
  private readonly EventWaitHandle _event;
  private readonly int _size;
  private readonly CancellationTokenSource _cts;
  private readonly Task _waiteEvent;
  private readonly Action<MapCommands> _callBack;
  private readonly object _syncLock = new();
  private readonly EventWaitHandle _sendTo;



  public BasicMemoryMd(string eventName, int size, string controlName, Action<MapCommands> callBack, EventWaitHandle sendTo)
  {
    _size = size;
    _sendTo = sendTo ?? throw new ArgumentNullException(nameof(sendTo));
    _mmf = MemoryMappedFile.CreateOrOpen(controlName, size);
    _event = new EventWaitHandle(false, EventResetMode.AutoReset, eventName);
    _callBack = callBack ?? (_ => { });
    _cts = new CancellationTokenSource();
    _waiteEvent = Task.Run(() => WaitEventLoop(_cts.Token), _cts.Token);


  }

  private void WaitEventLoop(CancellationToken token)
  {
    while (!token.IsCancellationRequested)
    {
      try
      {
        if (!_event.WaitOne(1000)) continue;
        MapCommands map = null;
        lock (_syncLock)
        {
          using var accessor = _mmf.CreateViewAccessor();
          var buffer = new byte[_size];
          accessor.ReadArray(0, buffer, 0, buffer.Length);
          var result = Encoding.UTF8.GetString(buffer).TrimEnd('\0');
          if (!string.IsNullOrWhiteSpace(result))
            map = ConvertStringToDict(result);
          // Очищаем всю MD память
          accessor.WriteArray(0, new byte[_size], 0, _size);
        }
        if (map is { Count: > 0 })
          _callBack(map);
      }
      catch (Exception)
      {
        _callBack(null);
      }
    }
  }

  public void WriteMetaMap(MapCommands map)
  {
    if (map == null || map.Count == 0) return;
    WriteMeta(ConvertDictToString(map));
  }

  public void WriteMeta(string text)
  {
    if (string.IsNullOrWhiteSpace(text)) return;
    lock (_syncLock)
    {
      using var accessor = _mmf.CreateViewAccessor();
      var data = Encoding.UTF8.GetBytes(text);
      accessor.WriteArray(0, data, 0, data.Length);
      // Можно дополнительно очистить остаток, если это критично
    }
    _sendTo.Set();
  }
  public static string ConvertDictToString(MapCommands dic)
    => dic == null || dic.Count == 0 ? null : string.Join(";", dic.Select(x => $"{x.Key}={x.Value}")) + ";";

  public static MapCommands ConvertStringToDict(string str)
    => string.IsNullOrWhiteSpace(str)
      ? null
      : str.Split(';', StringSplitOptions.RemoveEmptyEntries)
        .Select(s => s.Split('=', 2, StringSplitOptions.RemoveEmptyEntries))
        .Where(parts => parts.Length == 2)
        .ToDictionary(parts => parts[0], parts => parts[1]);

  public void Dispose()
  {
    _cts?.Cancel();
    Task.WhenAny(_waiteEvent, Task.Delay(1000)).Wait(); // не блокируйся бесконечно
    _event?.Dispose();
    _mmf?.Dispose();
    _cts?.Dispose();
    _sendTo?.Dispose();
  }
}


/*
  public string WaitAndRead()
  {
    _event.WaitOne();
    lock (_syncLock)
    {
      using var accessor = _mmf.CreateViewAccessor();
      var buffer = new byte[_size];
      accessor.ReadArray(0, buffer, 0, buffer.Length);
      return Encoding.UTF8.GetString(buffer).TrimEnd('\0');
    }
  }
*/

//using MapCommands = System.Collections.Generic.Dictionary<string, string>;

//namespace DMemory.Core;
//public class BasicMemoryMd : IDisposable
//{
//  private readonly MemoryMappedFile _mmf;
//  private readonly EventWaitHandle _event;
//  private readonly int _size;
//  private readonly CancellationTokenSource _cts;                 // завершение потока
//  private readonly Task _waiteEvent;
//  private readonly Action<MapCommands> _callBack;            // Инициализация пустым делегатом передать данные на верх
//  private readonly object _syncLock = new object();
//  private string _name;
//  private readonly EventWaitHandle _sendTo;
//  #region ===-- Constructor  --===
//  public BasicMemoryMd(string eventName, int size, string controlName, Action<MapCommands> callBack, EventWaitHandle sendTo)
//  {
//    _name = eventName;
//    _size = size;
//    _sendTo = sendTo;
//    _mmf = MemoryMappedFile.CreateOrOpen(controlName, size);
//    _event = new EventWaitHandle(false, EventResetMode.AutoReset, eventName);

//    _cts = new CancellationTokenSource();
//    var token = _cts.Token;
//    _waiteEvent = ReadDataCallBack(token);
//    _callBack = callBack;
//  }
//  #endregion
//  #region ===-- Read Write --===
//  private async Task ReadDataCallBack(CancellationToken cancellationToken = default)
//  {
//    await Task.Run(() =>
//    {
//    try
//    {
//      while (!cancellationToken.IsCancellationRequested)
//      {
//        if (!_event.WaitOne(1000)) continue;

//        // Блокируем доступ на время чтения
//        lock (_syncLock) // Добавляем объект для синхронизации
//        {
//         // _event.Reset();
//            using var accessor = _mmf.CreateViewAccessor();
//            var buffer = new byte[_size];
//            accessor.ReadArray(0, buffer, 0, buffer.Length);
//            var result = Encoding.UTF8.GetString(buffer).TrimEnd('\0');
//            // Очищаем память
//            using var accessor1 = _mmf.CreateViewAccessor();
//            //  Записываем пустой массив байтов по всему объему памяти
//            accessor1.WriteArray(0, MemStatic.EmptyBuffer, 0, MemStatic.SizeDataControl);
//            //            _event.Reset(); // сбрасываем прерывание

//            if (string.IsNullOrEmpty(result))
//              continue;
//            var map = ConvertStringToDict(result);
//            if(map  == null || !map.Any())
//              continue;
//            _callBack?.Invoke(map);
//        }
//      }
//    }
//    catch (Exception)
//    {
//      _callBack?.Invoke(null);
//    }
//    }, cancellationToken);
//  }
//  public void WriteMetaMap(MapCommands map)
//  {
//    string text = ConvertDictToString(map);
//    using var accessor = _mmf.CreateViewAccessor();
//    var data = Encoding.UTF8.GetBytes(text);
//    accessor.WriteArray(0, data, 0, data.Length);
//    _sendTo.Set();
//  }
//  public void WriteMeta(string text)
//  {
//    using var accessor = _mmf.CreateViewAccessor();
//    var data = Encoding.UTF8.GetBytes(text);
//    accessor.WriteArray(0, data, 0, data.Length);
//    //    Trace.WriteLine($"[{_role}] >>> META записано: {text}");
//    _sendTo.Set();
//  }
//  public string WaitAndRead()
//  {
//    _event.WaitOne();
//    using var accessor = _mmf.CreateViewAccessor();
//    byte[] buffer = new byte[_size];
//    accessor.ReadArray(0, buffer, 0, buffer.Length);
//    var result = Encoding.UTF8.GetString(buffer).TrimEnd('\0');
////    Trace.WriteLine($"[{_role}] <<< META прочитано: {result}");
//    return result;
//  }
//  #endregion
//  #region ===-- Convert --===
//  public string ConvertDictToString(MapCommands dic)
//  {
//    if (dic == null || !dic.Any())
//      return null;

//    return string.Join(";", dic.Select(x => $"{x.Key}={x.Value}")) + ";";
//  }
//  public MapCommands ConvertStringToDict(string str)
//  {
//    if (string.IsNullOrEmpty(str))
//      return null;

//    return str.Split(';', StringSplitOptions.RemoveEmptyEntries)
//      .Select(s => s.Split('=', 2, StringSplitOptions.RemoveEmptyEntries))
//      .Where(parts => parts.Length == 2)
//      .ToDictionary(parts => parts[0], parts => parts[1]);
//  }
//  #endregion

//  #region ===--  Dispose()  --===
//  public void Dispose()
//  {
//    _cts?.Cancel();
//    Task.WaitAll(_waiteEvent);
//    _event?.Dispose();
//    _mmf?.Dispose();
//  }
//  #endregion
//}
