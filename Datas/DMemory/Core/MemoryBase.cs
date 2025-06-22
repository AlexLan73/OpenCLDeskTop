using DMemory.Constants;
using MessagePack;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace DMemory.Core;


public class MemoryBase :IDisposable
{
  private string NameMemoryData { get;  }
  private string NameMemoryDataControl { get;  }
  private string EventNameMemoryDataControl { get; }
  private EventWaitHandle EventMemoryDataControl { get; }
  private Task WaiteEventRead { get; }
  private readonly MemoryMappedFile _mmDataControl;
  private MemoryMappedFile _mmData;
  
  private readonly Action<RecDataMetaData> _callBack = delegate { }; // Инициализация пустым делегатом
  private readonly CancellationTokenSource _cts;
  private bool _disposed = false;
  private readonly object _syncLock = new object();
  public MemoryBase(string nameMemory, TypeBlockMemory typeBlockMemory, Action<RecDataMetaData> callBack=null)
  {
    NameMemoryData = nameMemory;
    NameMemoryDataControl = $"{NameMemoryData}Control";
    EventNameMemoryDataControl = @$"Global\Event{NameMemoryData}";

    _mmDataControl = MemoryMappedFile.CreateOrOpen(NameMemoryDataControl, MemStatic.SizeDataControl);
    _mmData =  MemoryMappedFile.CreateOrOpen(NameMemoryData, MemStatic.SizeDataControl);
    EventMemoryDataControl = new EventWaitHandle(
      false,
      EventResetMode.AutoReset,
      EventNameMemoryDataControl,
      out var createdNew
    );
    Console.WriteLine(createdNew ? "Создано новое событие" : "Подключено к существующему событию");

    if(callBack!=null)
      _callBack = callBack;

    _cts = new CancellationTokenSource();
    var token = _cts.Token;
    if (typeBlockMemory == TypeBlockMemory.Read)
      WaiteEventRead = WaitDataControlEventAsync(token);
  }

/// <summary>
/// Передаем командную строку для записи в раздел контроль и запускаем event
/// </summary>
/// <param name="sData"></param>
  public void SetCommandControl(Dictionary<string, string> dic)
  {
    var sData = ConvertDictToString(dic);
    using (var accessor = _mmDataControl.CreateViewAccessor())
    {
      var data = Encoding.UTF8.GetBytes(sData);
      accessor.WriteArray(0, data, 0, data.Length);
    }
    EventMemoryDataControl.Set();
  }

  /// <summary>
  /// Постоянный цикл, проверяем событие на получение данных
  /// данные получены возвращает строку с данными для парсинга
  /// </summary>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  private async Task WaitDataControlEventAsync(CancellationToken cancellationToken = default)
  {
    await Task.Run(() =>
    {
      try
      {
        while (!cancellationToken.IsCancellationRequested)
        {
          if (!EventMemoryDataControl.WaitOne(1000)) continue;

          // Блокируем доступ на время чтения
          lock (_syncLock) // Добавляем объект для синхронизации
          {
            using var accessor = _mmDataControl.CreateViewAccessor();
            var buffer = new byte[MemStatic.SizeDataControl];
            accessor.ReadArray(0, buffer, 0, buffer.Length);
            var received = Encoding.UTF8.GetString(buffer).TrimEnd('\0');
            Console.WriteLine("Received WaitDataControlEventAsync: " + received);

            // Сбрасываем данные в памяти после чтения (опционально)
            accessor.WriteArray(0, new byte[MemStatic.SizeDataControl], 0, MemStatic.SizeDataControl);

            var map = ConvertStringToDict(received);
            if (map==null)
            {
              EventMemoryDataControl.Reset();
              return;
            }

            var size = Convert.ToInt32(map["size"]);
            var bytes = ReadMemoryData(size);

            _callBack.Invoke(new RecDataMetaData(bytes, map));
          }

          // Для AutoReset режима это не обязательно, но для надежности:
          EventMemoryDataControl.Reset();
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Ошибка в WaitDataControlEventAsync: {ex}");
        // Можно добавить уведомление об ошибке через callback
//        _callBack.Invoke($"ERROR: {ex.Message}");
        _callBack.Invoke(null);
      }
    }, cancellationToken);
  }
  /// <summary>
  /// Читаем из памяти данные в размере sizeData байт
  /// </summary>
  /// <param name="sizeData"></param>
  /// Возвращает вектор байт  byte[]
  public virtual byte[] ReadMemoryData(int sizeData)
  {

    if (sizeData <= 0)
      throw new ArgumentException("Размер данных должен быть положительным");

    try
    {
      _mmData = MemoryMappedFile.CreateOrOpen(NameMemoryData, sizeData);
      using var accessor = _mmData.CreateViewAccessor();
      var buffer = new byte[sizeData];
      accessor.ReadArray(0, buffer, 0, buffer.Length);
      return buffer;
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Ошибка чтения из памяти: {ex}");
      throw;
    }
  }

  public string ConvertDictToString(Dictionary<string, string> dic)
  {
    if (dic == null || !dic.Any())
      return null;

    return string.Join(";", dic.Select(x => $"{x.Key}={x.Value}")) + ";";
  }

  public Dictionary<string, string> ConvertStringToDict(string str)
  {
    if(string.IsNullOrEmpty(str))
      return null;

    return str.Split(';', StringSplitOptions.RemoveEmptyEntries)
      .Select(s => s.Split('=', 2, StringSplitOptions.RemoveEmptyEntries))
      .Where(parts => parts.Length == 2)
      .ToDictionary(parts => parts[0], parts => parts[1]);
  }


  // Запись данных в общую память
  public virtual void WriteByteData(byte[] bytes)
  {
    _mmData = MemoryMappedFile.CreateOrOpen(NameMemoryData, bytes.Length);
    using var accessor = _mmData.CreateViewAccessor();
    accessor.WriteArray(0, bytes, 0, bytes.Length);
  }

  // Дополнительный метод для записи объектов (с сериализацией)
  public void WriteObject<T>(T obj)
  {
    if (obj == null)
      throw new ArgumentNullException(nameof(obj));

    var bytes = MessagePackSerializer.Serialize(obj);
    WriteByteData(bytes);
  }

  // Дополнительный метод для чтения объектов (с десериализацией)
  public T ReadObject<T>(int bufferSize = 1024)
  {
    var bytes = ReadMemoryData(bufferSize);
    return MessagePackSerializer.Deserialize<T>(bytes);
  }

  protected virtual void Dispose(bool disposing)
  {
    if (_disposed) return;

    if (disposing)
    {
      _cts.Cancel();
      WaiteEventRead?.Wait(TimeSpan.FromSeconds(1)); // Ограниченное ожидание
      EventMemoryDataControl?.Dispose();
      _mmDataControl?.Dispose();
      _cts?.Dispose();
    }
    _disposed = true;
  }

  public void Dispose()
  {
    Dispose(true);
    GC.SuppressFinalize(this);
  }
}





//////////////////////////////////////////////////////
/*
using MessagePack;
using System.Collections.Generic;
using System.IO;

var dict = new Dictionary<string, List<float>> {
  { "a", new List<float> { 1.1f, 2.2f } },
  { "b", new List<float> { 3.3f } }
};
byte[] bytes = MessagePackSerializer.Serialize(dict);
File.WriteAllBytes("data.msgpack", bytes);

// Десериализация
var restored = MessagePackSerializer.Deserialize<Dictionary<string, List<float>>>(bytes);

*/
/*
#include <msgpack.hpp>
#include <fstream>
#include <map>
#include <vector>
#include <string>

int main()
{
  std::map < std::string, std::vector<float> > data = { { "a", { 1.1f, 2.2f} }, { "b", { 3.3f} } }
  ;
  std::stringstream buffer;
  msgpack::pack(buffer, data);

  // запись в файл
  std::ofstream ofs("data.msgpack", std::ios::binary);
  std::string const&str = buffer.str();
  ofs.write(str.data(), str.size());
  ofs.close();
}

*/


/*
public class SharedMemoryReader : IDisposable
{
  private MemoryMappedFile mmf;
  private MemoryMappedViewAccessor accessor;
  private EventWaitHandle notifyEvent;
  private string memoryName;
  private string eventName;
  private int memorySize;

  public SharedMemoryReader(string baseName, int blockIndex, int memorySize = 65536)
  {
    this.memoryName = $"{baseName}_{blockIndex}";
    this.eventName = $"{baseName}_Event_{blockIndex}";
    this.memorySize = memorySize;

    try
    {
      mmf = MemoryMappedFile.OpenExisting(memoryName, MemoryMappedFileRights.ReadWrite);
      accessor = mmf.CreateViewAccessor(0, memorySize);
      notifyEvent = new EventWaitHandle(false, EventResetMode.AutoReset, eventName);
    }
    catch (Exception ex)
    {
      Dispose();
      throw new Exception($"Failed to initialize shared memory block {blockIndex}", ex);
    }
  }

  public string ReadData(int timeoutMs = 1000)
  {
    if (notifyEvent.WaitOne(timeoutMs))
    {
      byte[] buffer = new byte[memorySize];
      accessor.ReadArray(0, buffer, 0, buffer.Length);
      return System.Text.Encoding.UTF8.GetString(buffer).TrimEnd('\0');
    }
    return null;
  }

  public void Dispose()
  {
    accessor?.Dispose();
    mmf?.Dispose();
    notifyEvent?.Close();
  }
}

class Program
{
  const int BLOCK_COUNT = 3;
  const int MEM_SIZE = 65536; // 64KB

  static void Main()
  {
    // Создаем читателей для каждого блока памяти
    var readers = new SharedMemoryReader[BLOCK_COUNT];
    for (int i = 0; i < BLOCK_COUNT; i++)
    {
      try
      {
        readers[i] = new SharedMemoryReader("Global\\SharedMemory", i, MEM_SIZE);
        Console.WriteLine($"Initialized reader for block {i}");
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error initializing block {i}: {ex.Message}");
        readers[i] = null;
      }
    }

    try
    {
      while (true)
      {
        for (int i = 0; i < BLOCK_COUNT; i++)
        {
          if (readers[i] != null)
          {
            string data = readers[i].ReadData(500);
            if (data != null)
            {
              Console.WriteLine($"[Block {i}] Received: {data}");
            }
          }
        }
        Thread.Sleep(100);
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Error: {ex.Message}");
    }
    finally
    {
      // Освобождаем ресурсы
      foreach (var reader in readers)
      {
        reader?.Dispose();
      }
    }
  }
}

*/