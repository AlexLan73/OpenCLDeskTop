// See https://aka.ms/new-console-template for more information

using Common.Enum;
using DMemory.Core;
using DMemory.Core.Test;
using MessagePack;
using System;
using System;
using System.Threading;
using YamlDotNet.Serialization;
using static System.Threading.Tasks.Task;

Console.WriteLine("Тест памяти!");


CudaMem01 _cudaMem = new CudaMem01(ServerClient.Server);
//CudaTest _cudaTest = new CudaTest(ServerClient.Server);
//_cudaTest.TestData();




//Thread.Sleep(2000);
Console.ReadLine();
int hh = 1;

_cudaMem.Dispose();
//_cudaTest.Dispose();
int mm = 1;
//Console.ReadLine();






//// 2. Создаем экземпляр record
//var user = new UserRecord(Id: 1, Username: "JohnDoe");
//// 3. Сериализуем в массив байт
//byte[] bytes = MessagePackSerializer.Serialize(user);
//Console.WriteLine($"Размер в байтах: {bytes.Length}");
//// 4. Десериализуем обратно в record
//UserRecord deserializedUser = MessagePackSerializer.Deserialize<UserRecord>(bytes);
//// 5. Проверяем, что данные восстановились корректно
//Console.WriteLine(deserializedUser); // Вывод: UserRecord { Id = 1, Username = JohnDoe }
//Console.WriteLine($"Объекты равны: {user == deserializedUser}"); // Вывод: true

////CudaMem _cudaMem = new CudaMem(ServerClient.Server);
////CudaTest _cudaTest = new CudaTest(ServerClient.Client);
////_cudaTest.TestData();

void TestTaskDataControl()
{
  Task _task0 = Run(() =>
  {
    Console.WriteLine("=== ПРОЦЕСС 1 (Инициатор) ===");

    // Создаем экземпляр для записи
    using var memoryWrite = new MemoryBase("MyCUDA", TypeBlockMemory.Write, ReceiveCallback);

    // Отправляем первое сообщение
    Dictionary<string, string> map = new();
    map.TryAdd("message", "Test Memory canals 1");
    map.TryAdd("size", "0");
    memoryWrite.SetCommandControl(map);
    Console.WriteLine("Отправлено: Memory test 01");

    // Ждем ответа
    Thread.Sleep(5000);

    Console.WriteLine("Нажмите Enter для выхода...");
    Console.ReadLine();

    void ReceiveCallback(RecDataMetaData? message)
    {
      if(message == null || message.MetaData == null || !message.MetaData.Any()) 
        return;
      var meta = message.MetaData;

      if (meta.TryGetValue("message", out var vMes))
      {
        Console.WriteLine($" TASK 0   Получен ответ: {vMes}");
        return;
      }
      if (meta.TryGetValue("type", out var vType))
      {
        Console.WriteLine($" TASK 0   Получен ответ: {vMes}");
        return;
      }

      Console.WriteLine($" TASK 0   Получен ответ: {message.ToString()}");
    }

  });


  Task _task1 = Run(() =>
  {
    Console.WriteLine("=== ПРОЦЕСС 2 (Обработчик) ===");

    // Создаем экземпляр для чтения
    using var memoryRead = new MemoryBase("MyCUDA", TypeBlockMemory.Read, ProcessMessage);

    Console.WriteLine("Ожидаем сообщения TASK 1...");
    Console.ReadLine();



    void ProcessMessage(RecDataMetaData dMetaData)
    {
      if (dMetaData==null || dMetaData.MetaData == null || dMetaData.MetaData.Count()<0) 
        return;
      var v = dMetaData.MetaData;
      if(v.TryGetValue("message", out var vMessedg))
        Console.WriteLine($"Получено сообщение: {vMessedg}");

      // Создаем временный экземпляр для ответа
      using var memoryResponse = new MemoryBase("MyCUDA", TypeBlockMemory.Write);

      // Формируем и отправляем ответ
      string response = $"{vMessedg} == Return ALL OK!!! == ";
      Dictionary<string, string> map = new();
      map.TryAdd("message", "Test Memory canals 1 "+ response);
      map.TryAdd("size", "0");

      memoryResponse.SetCommandControl(map);
      Console.WriteLine($"Отправлен ответ: {response}");
    }
  });


  _task1.Wait();
  _task0.Wait();

}

// 1. Определяем record с атрибутами MessagePack

    [MessagePackObject]
public record UserRecord(
  [property: Key(0)] int Id,
  [property: Key(1)] string Username
);

[MessagePackObject]
public record CudaTemperature(
  [property: Key(0)] string Dt,
  [property: Key(1)] float Temp
);


//waitAll(_task0, _task1);



