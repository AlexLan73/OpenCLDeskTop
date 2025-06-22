// See https://aka.ms/new-console-template for more information

using Common.Enum;
using DMemory.Core;
using MessagePack;
using System;
using System;
using System.Threading;
using YamlDotNet.Serialization;
using static System.Threading.Tasks.Task;

Console.WriteLine("Тест памяти!");

// 2. Создаем экземпляр record
var user = new UserRecord(Id: 1, Username: "JohnDoe");
// 3. Сериализуем в массив байт
byte[] bytes = MessagePackSerializer.Serialize(user);
Console.WriteLine($"Размер в байтах: {bytes.Length}");
// 4. Десериализуем обратно в record
UserRecord deserializedUser = MessagePackSerializer.Deserialize<UserRecord>(bytes);
// 5. Проверяем, что данные восстановились корректно
Console.WriteLine(deserializedUser); // Вывод: UserRecord { Id = 1, Username = JohnDoe }
Console.WriteLine($"Объекты равны: {user == deserializedUser}"); // Вывод: true


CudaMemory _cuda = new CudaMemory();
CudaClientTest _clientTest = new CudaClientTest();
_clientTest.TestData();
_cuda.TestDataMemory();


Thread.Sleep(5000);
int hh = 1;
Console.ReadLine();




void TestTaskDataControl()
{
  Task _task0 = Run(() =>
  {
    Console.WriteLine("=== ПРОЦЕСС 1 (Инициатор) ===");

    // Создаем экземпляр для записи
    using var memoryWrite = new MemoryBase("MyCUDA", TypeBlockMemory.Write, ReceiveCallback);

    // Отправляем первое сообщение
    memoryWrite.SetCommandControl("Memory test 01");
    Console.WriteLine("Отправлено: Memory test 01");

    // Ждем ответа
    Thread.Sleep(5000);

    Console.WriteLine("Нажмите Enter для выхода...");
    Console.ReadLine();

    void ReceiveCallback(string message)
    {
      Console.WriteLine($" TASK 0   Получен ответ: {message}");
    }

  });


  Task _task1 = Run(() =>
  {
    Console.WriteLine("=== ПРОЦЕСС 2 (Обработчик) ===");

    // Создаем экземпляр для чтения
    using var memoryRead = new MemoryBase("MyCUDA", TypeBlockMemory.Read, ProcessMessage);

    Console.WriteLine("Ожидаем сообщения TASK 1...");
    Console.ReadLine();

    void ProcessMessage(string message)
    {
      Console.WriteLine($"Получено сообщение: {message}");

      // Создаем временный экземпляр для ответа
      using var memoryResponse = new MemoryBase("MyCUDA", TypeBlockMemory.Write);

      // Формируем и отправляем ответ
      string response = $"{message} == Return ALL OK!!! == ";
      memoryResponse.SetCommandControl(response);
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



