using Common.Constants;
using DMemory.Core.Channel;
using Force.Crc32;
using MessagePack;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Reflection;
using System.Threading.Tasks;
using Common.Core.Channel;
using MapCommands = System.Collections.Generic.Dictionary<string, string>;

//public record RamData(object Data, Type DataType, MapCommands MetaData);

namespace DMemory.Core {
  public class MemoryDataProcessor : IDisposable
  {

    // Событие — метаданные подготовлены для передачи
    public event EventHandler<MapCommands> MetaReady;

    // Временное хранилище
    private (RamData Data, byte[] Buffer, MapCommands Meta)? _pending;

    private readonly string _memoryName;
    private readonly int _memorySize = 64 * 1024;
    private readonly Dictionary<string, Type> _typeMapping;
    private MemoryMappedFile _mmf;
    private MemoryMappedViewAccessor _accessor;

    // Событие обратного вызова для успешного получения и десериализации RamData
    private readonly Action<RamData> _onDataReceived;


    //  TypeFromNamespace
    //  { public static Dictionary<string, Type> GetTypeMappingFromNamespace(
    //

    // Делегат для callback в ServerMetaData 
    //    private readonly Action<MapCommands> _metaDataCallback;

    //    public MemoryDataProcessor(string memoryName, ConcurrentQueue<RamData> dataQueue, Dictionary<string, Type> typeMapping, Action<MapCommands> metaDataCallback)
    public MemoryDataProcessor(string memoryName, Action<RamData> onDataReceived)
    {
//      _typeMapping = GetTypeMappingFromNamespace("Channel");
//      _onDataReceived = onDataReceived;
//      _memoryName = memoryName ?? throw new ArgumentNullException(nameof(memoryName));
////      _dataQueue = dataQueue ?? throw new ArgumentNullException(nameof(dataQueue));
////      _typeMapping = typeMapping ?? throw new ArgumentNullException(nameof(typeMapping));
////      _metaDataCallback = metaDataCallback ?? throw new ArgumentNullException(nameof(metaDataCallback));

//      _mmf = MemoryMappedFile.CreateOrOpen(_memoryName, _memorySize, MemoryMappedFileAccess.ReadWrite);
//      _accessor = _mmf.CreateViewAccessor(0, _memorySize, MemoryMappedFileAccess.ReadWrite);

      _memoryName = memoryName ?? throw new ArgumentNullException(nameof(memoryName));
      // Инициализация маппинга типов (пример, ваш код может быть другим)
      _typeMapping = GetTypeMappingFromNamespace("Channel");

      _onDataReceived = onDataReceived ?? throw new ArgumentNullException(nameof(onDataReceived));

      _mmf = MemoryMappedFile.CreateOrOpen(_memoryName, _memorySize, MemoryMappedFileAccess.ReadWrite);
      _accessor = _mmf.CreateViewAccessor(0, _memorySize, MemoryMappedFileAccess.ReadWrite);
    }

    public void SerializeAndPrepare(RamData ramData)
    {
      if (ramData == null) throw new ArgumentNullException(nameof(ramData));
      // ... сериализация ...
      byte[] serialized = MessagePackSerializer.Serialize(ramData.DataType, ramData.Data);
      string crc = Crc32Helper.Compute(serialized);

      var meta = new MapCommands(ramData.MetaData)
      {
        ["type"] = ramData.DataType.IsArray ? ramData.DataType.Name + "[]" : ramData.DataType.Name,
        ["size"] = serialized.Length.ToString(),
        ["crc"] = crc
      };

      _pending = (ramData, serialized, meta);

      // Вызовем событие — метаданные готовы!
      MetaReady?.Invoke(this, meta);
    }

    public void CommitWrite() 
    {
      if (_pending != null)
        _accessor.WriteArray(0, _pending.Value.Buffer, 0, _pending.Value.Buffer.Length);
    }

    private Dictionary<string, Type> GetTypeMappingFromNamespace(string targetNamespace = "Channel")
    {
      var asm = Assembly.GetExecutingAssembly();
      var types = asm.GetTypes()
        .Where(t => t.Namespace != null && t.IsClass && t.Namespace.Contains(targetNamespace))
        .ToList();

      var mapping = new Dictionary<string, Type>();
      foreach (var type in types)
      {
        mapping[type.Name] = type;
        mapping[type.Name + "[]"] = type.MakeArrayType();
      }
      return mapping;
    }

    public string ProcessMetaData(MapCommands metaData)
    {
      if (metaData == null)
        return "no";

      if (!metaData.TryGetValue("size", out var sizeStr) || !int.TryParse(sizeStr, out int size) || size <= 0 || size > _memorySize)
        return "no";

      if (!metaData.TryGetValue("crc", out var crcExpected))
        return "no";

      if (!metaData.TryGetValue("type", out var typeKey))
        return "no";

      if (!_typeMapping.TryGetValue(typeKey, out var dataType))
        return "no";

      try
      {
        byte[] buffer = new byte[size];
        _accessor.ReadArray(0, buffer, 0, size);

        string crcActual = Crc32Helper.Compute(buffer);
        if (!string.Equals(crcActual, crcExpected, StringComparison.OrdinalIgnoreCase))
          return "no";

        object deserializedObj = MessagePackSerializer.Deserialize(dataType, buffer);
        if (deserializedObj == null)
          return "no";

        var ramData = new RamData(deserializedObj, dataType, new MapCommands(metaData));

        // Вызов события с готовыми данными — уведомляем "верх"
        _onDataReceived?.Invoke(ramData);

        return "ok";
      }
      catch (Exception ex)
      {
        Console.WriteLine($"[MemoryDataProcessor] Ошибка десериализации: {ex.Message}");
        return "no";
      }
    }

    public void Dispose()
    {
      _accessor?.Dispose();
      _mmf?.Dispose();
    }
  }

  public static class Crc32Helper
  {
    public static string Compute(byte[] data)
    {
      uint crc = Force.Crc32.Crc32Algorithm.Compute(data);
      return crc.ToString("X8");
    }
  }
}

