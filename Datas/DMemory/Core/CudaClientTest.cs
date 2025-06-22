using MessagePack;
using Microsoft.AspNetCore.Html;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Core;
using Common.Core.Property;
using Common.Enum;
using DMemory.Constants;
using YamlDotNet.Serialization;

namespace DMemory.Core;

public class CudaClientTest
{
  private DuplexMemory _memory;
  string _nameTypeRecord;
  private ISerializer _iserializer;
  private IDeserializer _ideserializer;// = new DeserializerBuilder().Build();
  private CudaTemperature _temperature;
  private CudaTemperature[] _temperatureArr;

  public CudaClientTest()
  {
    _memory = new DuplexMemory("CUDA", ServerClient.Client, CallbackCommandDatAction);
    _nameTypeRecord = nameof(CudaTemperature);
    _iserializer = new SerializerBuilder().Build();
    _ideserializer = new DeserializerBuilder().Build();
  }

  private void CallbackCommandDatAction(string st)
  {
    Task.Run(() =>
    {
      var mas = _ideserializer.Deserialize<Dictionary<string, string>>(st);
      var size = Convert.ToInt32(mas["size"]);
      var bytes = _memory.ReadMemoryData(size);
      long sum = bytes.Sum(x => x);
      if (bytes.Sum(x => x) != long.Parse(mas["control_sum"]))
      {
        throw new MyException("Error in memory sum bytes", -34);
        return;
      }
      var typeName = mas["type"];

      try
      {
        switch (typeName)
        {
          case not null when typeName == MemStatic.StCudaTemperature: // _cudaTemperature:
          {
            _temperature = MessagePackSerializer.Deserialize<CudaTemperature>(bytes);
            break;
          }
          case not null when typeName == MemStatic.StArrCudaTemperature:
            {
            _temperatureArr = MessagePackSerializer.Deserialize<CudaTemperature[]>(bytes);
            break;
          }
        }
      }
      catch (Exception e)
      {
        Console.WriteLine(e);
        throw new MyException($"Error convert {typeName}  byte from memory. ", -35);

      }

      int jj = 1;

    });


  }

  public void TestData()
  {

    var ls = new List<CudaTemperature>
    {
      new CudaTemperature(DateTime.Now.ToString("HH:mm:ss.fff"), 43.0f),
      new CudaTemperature(DateTime.Now.ToString("HH:mm:ss.fff"), 41.0f),
      new CudaTemperature(DateTime.Now.ToString("HH:mm:ss.fff"), 42.0f),
      new CudaTemperature(DateTime.Now.ToString("HH:mm:ss.fff"), 44.0f),
      new CudaTemperature(DateTime.Now.ToString("HH:mm:ss.fff"), 33.0f)
    };

    var arr = ls.ToArray();

    _nameTypeRecord =  arr.GetType().Name.ToLower();
    var bytesTemp = MessagePackSerializer.Serialize(arr);

    _memory.WriteDataToMwmory(bytesTemp);
    long sumByte = bytesTemp.Sum(x=>x);
    var size = "" + bytesTemp.Length;
    var dict = new Dictionary<string, string>();
    dict.TryAdd("type", _nameTypeRecord);
    dict.TryAdd("size", size);
    dict.TryAdd("control_sum", sumByte.ToString());
    string sd = dict.ToString();

    string yamlString = _iserializer.Serialize(dict);
    _memory.CommandControl(yamlString);
  }
}


////      CudaTemperature[] deserializedUsertemp = MessagePackSerializer.Deserialize<CudaTemperature[]>(bytesTemp);


////      using var memoryWrite = new MemoryBase("MyCUDA", TypeBlockMemory.Write);
////      using var memoryRead = new MemoryBase("MyCUDA", TypeBlockMemory.Read, callBackSetCommand);
//      memoryWrite.SetCommandControl(yamlString);

//      //TestTaskDataControl();
//      //Thread.Sleep(1000);
//      //var iii = 1;

//      Console.ReadLine();



//      void callBackSetCommand(string st)
//      {

//        Console.WriteLine($" ==>>  {st}   <<=== ");
//        //  Task.Run(() =>
//        {
//          byte[] _bytes;
//          Dictionary<string, string> mas = deserializer.Deserialize<Dictionary<string, string>>(yamlString);
//          int _size = Convert.ToInt32(mas["size"]);

//          if (memoryRead != null)
//          {
//            _bytes = memoryRead.ReadMemoryData(_size);
//          }



//        });
//      }




