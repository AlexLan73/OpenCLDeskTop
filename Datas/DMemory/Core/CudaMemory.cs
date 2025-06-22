
using Common.Core;
using ControlzEx.Standard;
using DryIoc;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using DMemory.Constants;
using YamlDotNet.Serialization;
using static System.Windows.Forms.DataFormats;

namespace DMemory.Core;


public class CudaMemory
{
  private DuplexMemory _memory;
  string _nameTypeRecord;

  private ISerializer _iserializer;
  private IDeserializer _ideserializer;
  private readonly string _cudaTemperature = nameof(CudaTemperature).ToLower();
  private readonly string _arrCudaTemperature = nameof(CudaTemperature).ToLower()+"[]";
  private CudaTemperature[] _temperatureArr;
  private CudaTemperature _temperature;
  public CudaMemory()
  {
    _memory = new DuplexMemory("CUDA", ServerClient.Server, CallbackCommandDatAction);
    _nameTypeRecord = nameof(CudaTemperature);
    _iserializer = new SerializerBuilder().Build();
    _ideserializer = new DeserializerBuilder().Build();

  }

/*
   private void CallbackCommandDatAction(RecDataMetaData dMetaData)
   {


     Task.Run(() =>
     {
       var v = dMetaData;
       var dMeta = v.MetaData;
       if (v.Bytes.Sum(x => x) != long.Parse(dMeta["control_sum"]))
       {
         throw new MyException("Error in memory sum bytes", -34);
         return;
       }
       var typeName = dMeta["type"];

 
 */
  private void CallbackCommandDatAction(RecDataMetaData dMetaData)
  {
    if (dMetaData == null)
      return;
    // Здесь нужно писать в очередь

    Task.Run(() =>
    {
      var v = dMetaData;
      var dMeta = v.MetaData;
      if (!dMeta.ContainsKey("control_sum") || v.Bytes.Sum(x => x) != long.Parse(dMeta["control_sum"]))
      {
        throw new MyException("Error in memory sum bytes", -34);
        return;
      }
      var typeName = dMeta["type"];

      //     Например: 21.06.2025 22:01:00.333    "yyyy'-'MM'-'dd' 'HH':'mm':'ss'.'fffffffzzz". Например, 2015-07-17T17:04:43.4092892+03:00
      string format = "HH':'mm':'ss'.'fff";

      try
      {
        switch (typeName)
        {
          case not null when typeName == MemStatic.StCudaTemperature:  //_cudaTemperature:
          {
            _temperature = MessagePackSerializer.Deserialize<CudaTemperature>(v.Bytes);
            CudaDtTemperature _CudaDtTemp = new CudaDtTemperature(
              DateTime.ParseExact(_temperature.Dt, format, CultureInfo.InvariantCulture), _temperature.Temp);
            break;
          }
          case not null when typeName == MemStatic.StArrCudaTemperature:  //_arrCudaTemperature:
          {
            _temperatureArr = MessagePackSerializer.Deserialize<CudaTemperature[]>(v.Bytes);
            List<CudaDtTemperature> _lsCudaDtTemp = _temperatureArr.Select(x => 
                new CudaDtTemperature(DateTime.ParseExact(x.Dt, format, CultureInfo.InvariantCulture) ,x.Temp)).ToList();
            PrintCudaTemperatures(_temperatureArr);
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

  public void TestDataMemory()
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

    _nameTypeRecord = arr.GetType().Name.ToLower();
    var bytesTemp = MessagePackSerializer.Serialize(arr);

    _memory.WriteDataToMwmory(bytesTemp);
    long sumByte = bytesTemp.Sum(x => x);
    var size = "" + bytesTemp.Length;
    var dict = new Dictionary<string, string>();
    dict.TryAdd("type", _nameTypeRecord);
    dict.TryAdd("size", size);
    dict.TryAdd("control_sum", sumByte.ToString());
    string sd = dict.ToString();
    string yamlString = _iserializer.Serialize(dict);
    _memory.CommandControl(dict);
  }

  public static void PrintCudaTemperatures(CudaTemperature[] temperatures)
  {
    if (temperatures == null || temperatures.Length == 0)
    {
      Console.WriteLine("No temperature data available");
      return;
    }

    // Вариант 1: Простой вывод
    Console.WriteLine("Temperature Data:");
    Console.WriteLine("----------------");
    foreach (var temp in temperatures)
    {
      Console.WriteLine($"Date: {temp.Dt}, Temperature: {temp.Temp}°C");
    }

    // Вариант 2: Табличный вывод
    Console.WriteLine("\nFormatted Table:");
    Console.WriteLine("| {0,-20} | {1,-10} |", "Date", "Temperature");
    Console.WriteLine("|{0}|{1}|", new string('-', 22), new string('-', 12));
    foreach (var temp in temperatures)
    {
      Console.WriteLine("| {0,-20} | {1,-10:F2} |", temp.Dt, temp.Temp);
    }

    // Вариант 3: JSON-вывод (для логов)
    Console.WriteLine("\nJSON Format:");
    Console.WriteLine(MessagePackSerializer.ConvertToJson(MessagePackSerializer.Serialize(temperatures)));
  }


}



//[MessagePackObject]
//public record CudaTemperature(
//  [property: Key(0)] string Dt,
//  [property: Key(1)] float Temp
//);
//public enum ServerClient
//{
//  Server,
//  Client
//}

