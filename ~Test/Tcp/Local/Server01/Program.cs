// See https://aka.ms/new-console-template for more information

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;


Console.WriteLine("Test sSERVER 02 по шагам от ИИ");

var listener = new TcpListener(IPAddress.Parse("127.0.0.1"), 20000);
listener.Start();
Console.WriteLine("Сервер запущен. Ожидание подключения...");

using var client = listener.AcceptTcpClient();
Console.WriteLine($"Клиент подключен: {client.Client.RemoteEndPoint}");

var stream = client.GetStream();
byte[] buffer = new byte[1024];
int bytesRead = stream.Read(buffer, 0, buffer.Length);
Console.WriteLine($"Получено: {Encoding.UTF8.GetString(buffer, 0, bytesRead)}");

stream.Write(Encoding.UTF8.GetBytes("Привет от сервера!"));
Console.WriteLine("Ответ отправлен");
