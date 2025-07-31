// See https://aka.ms/new-console-template for more information

using Data.Core;

Console.WriteLine("Запуск в DataContext  CUDAModule ");
Console.WriteLine(" Test на прием данных от С++  и пересылка управляющих данных в С++ ");

var _cudaServrt = new CUDAModule();

Console.ReadLine();
