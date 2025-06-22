// See https://aka.ms/new-console-template for more information

using Grpc.Core;
using Grpc.Net.Client;
using GrpcService1;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

Console.WriteLine("Запуск gRPC клиента...");

// 1. Создаём канал к серверу
using var channel = GrpcChannel.ForAddress("http://localhost:20010");
var client = new VectorService.VectorServiceClient(channel);

// 2. Настройка таймаута
using var cts = new CancellationTokenSource();
cts.CancelAfter(TimeSpan.FromSeconds(30));

try
{
    // 3. Открываем дуплексный поток
    using var call = client.ProcessVector(cancellationToken: cts.Token);
    Console.WriteLine("Подключение к серверу установлено");

    // 4. Задача для чтения ответов от сервера
    var readTask = Task.Run(async () =>
    {
        try
        {
            await foreach (var response in call.ResponseStream.ReadAllAsync(cts.Token))
            {
                // Получаем исходный вектор от сервера
                var originalVector = response.ModifiedNumbers.ToList();
                Console.WriteLine($"Получен вектор от сервера: [{string.Join(", ", originalVector)}]");

                // Модифицируем вектор (+10 к каждому элементу)
                var modifiedVector = originalVector.Select(x => x + 10).ToList();

                // Отправляем модифицированный вектор обратно
                await call.RequestStream.WriteAsync(new VectorRequest
                {
                    Numbers = { modifiedVector }
                });
                Console.WriteLine($"Отправлен модифицированный вектор: [{string.Join(", ", modifiedVector)}]");

                // Закрываем поток после отправки
                await call.RequestStream.CompleteAsync();
            }
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
        {
            Console.WriteLine("Чтение отменено");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при чтении: {ex.Message}");
        }
    });

    // 5. Ожидаем завершения
    await readTask;
}
catch (RpcException ex)
{
    Console.WriteLine($"Ошибка gRPC: {ex.Status.StatusCode} - {ex.Status.Detail}");
}
catch (Exception ex)
{
    Console.WriteLine($"Неожиданная ошибка: {ex.Message}");
}

Console.WriteLine("Клиент завершил работу");


//var input00 = new HelloRequest() { Name = "!!!  HI !!!!! 20000" };
//var channel00 = GrpcChannel.ForAddress("https://localhost:20000");
//var client00 = new Greeter.GreeterClient(channel00);
//var reply00 = await client00.SayHelloAsync(input00);
//Console.WriteLine(reply00.Message);

//var channel = GrpcChannel.ForAddress("https://localhost:20000");
//var client = new Greeter.GreeterClient(channel);

//// Создаем дуплексное соединение
//using var call = client.Chat();

//// Запускаем прием сообщений в фоне
//var readTask = Task.Run(async () => {
//    await foreach (var response in call.ResponseStream.ReadAllAsync())
//    {
//        Console.WriteLine($"Сервер ответил: {response.Message}");
//    }
//});

//// Отправляем 5 сообщений
//for (int i = 0; i < 5; i++)
//{
//    await call.RequestStream.WriteAsync(new HelloRequest
//    {
//        Name = $"Сообщение {i}"
//    });
//    await Task.Delay(1000);
//}

//// Закрываем отправку
//await call.RequestStream.CompleteAsync();
//await readTask;

/// wwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwww
/*
var input10 = new HelloRequest() { Name = "!!!  HI !!!!! 20010" };
var channel10 = GrpcChannel.ForAddress("https://localhost:20010");
var client10 = new Greeter.GreeterClient(channel10);
var reply10 = await client10.SayHelloAsync(input10);
Console.WriteLine(reply10.Message);


var input20 = new HelloRequest() { Name = "!!!  HI !!!!! 20020" };
var channel20 = GrpcChannel.ForAddress("https://localhost:20000");
var client20 = new Greeter.GreeterClient(channel20);
var reply20 = await client20.SayHelloAsync(input20);
Console.WriteLine(reply20.Message);
*/
Console.ReadLine();

