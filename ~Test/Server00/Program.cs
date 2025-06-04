// See https://aka.ms/new-console-template for more information

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;


Console.WriteLine("Test Server 127.0.0.1 port 20000");


IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
int port = 20000;

TcpListener listener = new TcpListener(ipAddress, port);

try
{
    listener.Start();
    Console.WriteLine($"Сервер запущен на {ipAddress}:{port}. Ожидание подключений...");

    while (true)
    {
        using TcpClient client = listener.AcceptTcpClient();
        Console.WriteLine($"Подключен клиент: {client.Client.RemoteEndPoint}");

        NetworkStream stream = client.GetStream();

        byte[] buffer = new byte[1024];
        int bytesRead = stream.Read(buffer, 0, buffer.Length);

        string received = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        Console.WriteLine($"Получено: {received}");

        string response = "Сообщение получено сервером";
        byte[] responseBytes = Encoding.UTF8.GetBytes(response);
        stream.Write(responseBytes, 0, responseBytes.Length);
    }
}
catch (SocketException ex)
{
    Console.WriteLine($"Ошибка сокета: {ex.Message}");
}
finally
{
    listener.Stop();
}





////  !!!!!   РАБОЧИЙ ВАРИАНТ
//// Создаем локальную конечную точку с IP 127.0.1.1 и портом 20000
//IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
//IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 20000);

//// Создаем TCP сокет
//Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

//try
//{
//  // Привязываем сокет к локальной конечной точке и начинаем слушать
//  listener.Bind(localEndPoint);
//  listener.Listen(10);

//  Console.WriteLine($"Сервер запущен на {localEndPoint}. Ожидание подключений...");

//  while (true)
//  {
//    // Принимаем входящее соединение
//    Socket handler = listener.Accept();
//    Console.WriteLine($"Подключен клиент: {handler.RemoteEndPoint}");

//    // Буфер для приема данных
//    byte[] buffer = new byte[1024];
//    int bytesRec = handler.Receive(buffer);

//    string data = Encoding.UTF8.GetString(buffer, 0, bytesRec);
//    Console.WriteLine($"Получено: {data}");

//    // Отправляем ответ клиенту
//    string reply = "Сообщение получено сервером";
//    byte[] msg = Encoding.UTF8.GetBytes(reply);
//    handler.Send(msg);

//    // Закрываем соединение с клиентом
//    handler.Shutdown(SocketShutdown.Both);
//    handler.Close();
//  }
//}
//catch (Exception e)
//{
//  Console.WriteLine($"Ошибка: {e.Message}");
//}



//IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
//int port = 10000;

//TcpListener listener = new TcpListener(ipAddress, port);
//listener.Start();
//Console.WriteLine($"Сервер запущен на {ipAddress}:{port}");

//while (true)
//{
//  TcpClient client = listener.AcceptTcpClient();
//  Console.WriteLine($"Подключен клиент: {client.Client.RemoteEndPoint}");

//  NetworkStream stream = client.GetStream();

//  byte[] buffer = new byte[1024];
//  int bytesRead = stream.Read(buffer, 0, buffer.Length);

//  string received = Encoding.UTF8.GetString(buffer, 0, bytesRead);
//  Console.WriteLine($"Получено: {received}");

//  string response = "Сообщение получено сервером";
//  byte[] responseBytes = Encoding.UTF8.GetBytes(response);
//  stream.Write(responseBytes, 0, responseBytes.Length);

//  client.Close();
//}

