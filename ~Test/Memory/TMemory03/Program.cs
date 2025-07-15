// See https://aka.ms/new-console-template for more information
using Common.Enum;
using DMemory.Core;

Console.WriteLine("Тест протокола с С++");
Console.WriteLine(" Test 003 Memory  ");

//Console.WriteLine(" 1. Инициализация участка памяти.");


Console.WriteLine("--- C# СЕРВЕР ЗАПУЩЕН ---");
Console.WriteLine("Ожидание данных от клиента...");

// Убедитесь, что запускаете в режиме Сервера
using var cudaServer = new CudaMem(ServerClient.Server);

Console.WriteLine("Нажмите Enter для завершения работы сервера.");
Console.ReadLine();

