
using System.Collections.Generic;

namespace DMemory.Core;

public class CudaMem(ServerClient serverClient) : MemoryNome("Cuda", serverClient)
{
  public override void CallbackCommandDatAction(RecDataMetaData dMetaData)
  {
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
             CudaTest.PrintCudaTemperatures(temperatureArr);
             Trace.WriteLine(" ---  Server ==> SEND  ---  ");
//              TestReturnData(temperatureArr);
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

