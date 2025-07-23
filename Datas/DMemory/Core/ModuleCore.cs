
using DMemory.Core.Copy;

namespace DMemory.Core;

public class ModuleCore( ServerClient serverClient) : MemoryNomeNew("Cuda", serverClient)
{

  public override void CallbackCommandDatAction(RecDataMetaData dMetaData)
  {
    if (dMetaData?.MetaData == null || !dMetaData.MetaData.TryGetValue("type", out var typeIdStr))
      return;

    // Новый ответ от сервера: просто подтверждение
    SendAck();
    // Обработку данных можно вынести в отдельный поток, чтобы не блокировать ответ
    Task.Run(() =>
    {
      try
      {
        var dMeta = dMetaData.MetaData;
        var bytes = dMetaData.Bytes;

//        var v0 = bytes.Sum(x => x);
//        var v1 = long.Parse(dMeta["control_sum"]);


        if (!uint.TryParse(typeIdStr, out var typeId)) return;

        Console.WriteLine($"\n[C# СЕРВЕР] Получены данные с ID типа: {typeId}");

        // Используем switch для обработки разных типов данных
        switch (typeId)
        {
          case DataTypeIds.Logger:
            var lsLoggers = MessagePackSerializer.Deserialize<string[]>(dMetaData.Bytes);
            Console.WriteLine($"  -> Тип: Vector[]. Количество: {lsLoggers.Length}");
            Console.WriteLine(
              $"  -> Первое значение первого вектора: {lsLoggers[0]}\n  ----- {lsLoggers[0]}\n");

            break;

          case DataTypeIds.CudaVector:
            var vectors = MessagePackSerializer.Deserialize<Vector[]>(dMetaData.Bytes);
            Console.WriteLine($"  -> Тип: Vector[]. Количество: {vectors.Length}");
            Console.WriteLine(
              $"  -> Первое значение первого вектора: {vectors.FirstOrDefault()?.Values.FirstOrDefault()}");
            break;

          case DataTypeIds.CudaDateTimeVariable:
            var vars = MessagePackSerializer.Deserialize<DateTimeVariable[]>(dMetaData.Bytes);
            Console.WriteLine($"  -> Тип: DateTimeVariable[]. Количество: {vars.Length}");
            Console.WriteLine($"  -> Первая переменная: {vars.FirstOrDefault()?.Variable}");
            break;

          // Добавьте обработчики для других типов по аналогии
          // case DataTypeIds.CudaMatrix:
          // ...
          // break;

          default:
            Console.WriteLine($"  -> Неизвестный ID типа: {typeId}");
            break;
        }
      }
      catch (Exception e)
      {
        Console.WriteLine($"[C# СЕРВЕР] Ошибка десериализации: {e.Message}");
      }
    });
  }


// ... (остальной код)


     



/*
    if (dMetaData == null)
      return;
    // Здесь нужно писать в очередь

    Task.Run(() =>
    {
      var v = dMetaData;
      var dMeta = v.MetaData;

      var v0 = v.Bytes.Sum(x => x);
      var v1 = long.Parse(dMeta["control_sum"]);

      //if (!dMeta.ContainsKey("control_sum") || v.Bytes.Sum(x => x) != long.Parse(dMeta["control_sum"]))
      //{
      //  throw new MyException("Error in memory sum bytes", -34);
      //  return;
      //}

      var typeName = dMeta["type"];

      string format = "HH:mm:ss.FFFFFFF"; // 7 заглавных 'F'

      try
      {
        switch (typeName)
        {
          case not null when typeName == MemStatic.StCudaTemperature:  //_cudaTemperature:
          {
            var _temperature = MessagePackSerializer.Deserialize<CudaTemperature>(v.Bytes);
            CudaDtTemperature _CudaDtTemp = new CudaDtTemperature(
              DateTime.ParseExact(_temperature.Dt, format, CultureInfo.InvariantCulture), _temperature.Temp);
            break;
          }
          case not null when typeName == MemStatic.StArrCudaTemperature:  //_arrCudaTemperature:
          {
            var temperatureArr = MessagePackSerializer.Deserialize<CudaTemperature[]>(v.Bytes);
            var lsCudaDtTemp = temperatureArr.Select(x =>
                new CudaDtTemperature(DateTime.ParseExact(x.Dt, format, CultureInfo.InvariantCulture), x.Temp)).ToList();
             CudaTest01.PrintCudaTemperatures(temperatureArr);
             Trace.WriteLine(" ---  Server ==> SEND  ---  ");
              TestReturnData(temperatureArr);
            break;
          }
        }
      }
      catch (Exception e)
      {
        Console.WriteLine(e);
        throw new MyException($"Error convert {typeName}  byte from memory. ", -35);

      }
    });

  }
*/
  /// <summary>
  /// Отправляет клиенту подтверждение о получении данных.
  /// Передаются только метаданные, без основного блока данных.
  /// </summary>

  public void SendAck()
  {
    var ackMetadata = new Dictionary<string, string>
    {
      { "command", "ok" },
      { "size", "0" } // Явно указываем, что данных нет
    };

    // Отправляем пустой массив байт и метаданные
    WriteDataToMemory(Array.Empty<byte>(), ackMetadata);
    Console.WriteLine("[C# СЕРВЕР] Подтверждение 'ok' отправлено клиенту.");
  }

  public void TestReturnData(CudaTemperature[] data)
  {
    data = data.Select(x=> new CudaTemperature(x.Dt, x.Temp*10)).ToArray();

    var _nameTypeRecord = data.GetType().Name.ToLower();
    var bytesTemp = MessagePackSerializer.Serialize(data);

    long sumByte = bytesTemp.Sum(x => x);
    var size = "" + bytesTemp.Length;
    var dict = new Dictionary<string, string>();
    dict.TryAdd("type", _nameTypeRecord);
    dict.TryAdd("size", size);
    dict.TryAdd("control_sum", sumByte.ToString());
    WriteDataToMemory(bytesTemp, dict);

  }
}
// ReSharper disable once InvalidXmlDocComment
/// Например: 21.06.2025 22:01:00.333    "yyyy'-'MM'-'dd' 'HH':'mm':'ss'.'fffffffzzz". Например, 2015-07-17T17:04:43.4092892+03:00

