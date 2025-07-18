//using YamlDotNet.Serialization;

namespace DMemory.Core.Test;

public class CudaTest01 : CudaMem01
{
  //private MemoryNome _memory;
  //string _nameTypeRecord;
  //private ISerializer _iserializer;
  //private IDeserializer _ideserializer;// = new DeserializerBuilder().Build();
  //private CudaTemperature _temperature;
  //private CudaTemperature[] _temperatureArr;

  public CudaTest01(ServerClient serverClient) : base(serverClient)
  {
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

    var _nameTypeRecord =  arr.GetType().Name.ToLower();
    var bytesTemp = MessagePackSerializer.Serialize(arr);

    long sumByte = bytesTemp.Sum(x=>x);
    var size = "" + bytesTemp.Length;
    var dict = new Dictionary<string, string>();
    dict.TryAdd("type", _nameTypeRecord);
    dict.TryAdd("size", size);
    dict.TryAdd("control_sum", sumByte.ToString());
    WriteDataToMemory(bytesTemp, dict);

//    string sd = dict.ToString();

//    string yamlString = _iserializer.Serialize(dict);
//    _memory.CommandControlWrite(dict);

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


////      CudaTemperature[] deserializedUsertemp = MessagePackSerializer.Deserialize<CudaTemperature[]>(bytesTemp);


////      using var memoryWrite = new MemoryBase("MyCUDA", TypeBlockMemory.Write);
////      using var memoryRead = new MemoryBase("MyCUDA", TypeBlockMemory.Read, callBackSetCommand);
//      memoryWrite.SendCommand(yamlString);

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




