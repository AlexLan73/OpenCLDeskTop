using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Threading.Tasks;
using MessagePack;
using Force.Crc32;
using MapCommands = System.Collections.Generic.Dictionary<string, string>;

public record RamData(object Data, Type DataType, MapCommands MetaData);

namespace DMemory.Core {
  public class MemoryDataProcessor : IDisposable
  {
    private readonly string _memoryName;
    private readonly int _memorySize = 64 * 1024;
    private readonly ConcurrentQueue<RamData> _dataQueue;
    private readonly Dictionary<string, Type> _typeMapping;
    private MemoryMappedFile _mmf;
    private MemoryMappedViewAccessor _accessor;

    // Делегат для callback в ServerMetaData 
    private readonly Action<MapCommands> _metaDataCallback;

    public MemoryDataProcessor(string memoryName, ConcurrentQueue<RamData> dataQueue, Dictionary<string, Type> typeMapping, Action<MapCommands> metaDataCallback)
    {
      _memoryName = memoryName ?? throw new ArgumentNullException(nameof(memoryName));
      _dataQueue = dataQueue ?? throw new ArgumentNullException(nameof(dataQueue));
      _typeMapping = typeMapping ?? throw new ArgumentNullException(nameof(typeMapping));
      _metaDataCallback = metaDataCallback ?? throw new ArgumentNullException(nameof(metaDataCallback));

      _mmf = MemoryMappedFile.CreateOrOpen(_memoryName, _memorySize, MemoryMappedFileAccess.ReadWrite);
      _accessor = _mmf.CreateViewAccessor(0, _memorySize, MemoryMappedFileAccess.ReadWrite);
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
        _dataQueue.Enqueue(ramData);

        return "ok";
      }
      catch (Exception ex)
      {
        Console.WriteLine($"[MemoryDataProcessor] Ошибка десериализации: {ex.Message}");
        return "no";
      }
    }

    public async Task<bool> SerializeAndWriteAsync(RamData ramData)
    {
      if (ramData == null) throw new ArgumentNullException(nameof(ramData));
      if (ramData.Data == null) throw new ArgumentException("Data in RamData cannot be null");

      try
      {
        byte[] serialized = MessagePackSerializer.Serialize(ramData.DataType, ramData.Data);

        if (serialized.Length > _memorySize)
          throw new InvalidOperationException($"Serialized data size ({serialized.Length}) превышает {_memorySize} байт.");

        string crc = Crc32Helper.Compute(serialized);

        _accessor.WriteArray(0, serialized, 0, serialized.Length);

        var updatedMetaData = new MapCommands(ramData.MetaData)
        {
          ["type"] = ramData.DataType.IsArray ? ramData.DataType.Name + "[]" : ramData.DataType.Name,
          ["size"] = serialized.Length.ToString("X"),
          ["crc"] = crc
        };

        // Вызов callback вместо события
        _metaDataCallback(updatedMetaData);

        await Task.CompletedTask;

        return true;
      }
      catch (Exception ex)
      {
        Console.WriteLine($"[MemoryDataProcessor] Ошибка сериализации и записи: {ex}");
        return false;
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


/*
 Как из ServerMetaData создать и использовать MemoryDataProcessor:

var dataQueue = new ConcurrentQueue<RamData>();
   
   var typeMapping = new Dictionary<string, Type>()
   {
       { "Logger", typeof(Logger) },
       { "DTVariable", typeof(DTVariable) },
       { "VectorId", typeof(VectorId) },
       { "VectorId[]", typeof(VectorId[]) }
   };
   
   var processor = new MemoryDataProcessor("SharedMemoryName", dataQueue, typeMapping, updatedMeta =>
   {
       // Callback, который вызывается при обновлении метаданных
       this.WriteMetaMap(updatedMeta);
   });
   

 */