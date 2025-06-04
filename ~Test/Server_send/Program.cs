// See https://aka.ms/new-console-template for more information

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

Console.WriteLine("Test Server - SEND 127.0.0.1 port 20000");

IPAddress localIP = IPAddress.Parse("127.0.0.1");
int localPort = 15000; // фиксированный исходящий порт клиента

IPAddress serverIP = IPAddress.Parse("127.0.0.1");
int serverPort = 20000;

Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

// Привязываем клиентский сокет к локальному IP и фиксированному порту
socket.Bind(new IPEndPoint(localIP, localPort));

// Подключаемся к серверу
socket.Connect(new IPEndPoint(serverIP, serverPort));

string message = "Hi SERVER!";
byte[] msgBytes = Encoding.UTF8.GetBytes(message);

socket.Send(msgBytes);

Console.WriteLine($"Сообщение отправлено с локального порта {localPort}");

socket.Shutdown(SocketShutdown.Both);
socket.Close();


//// Адрес и порт сервера
//IPAddress ipAddress = IPAddress.Parse("127.0.1.1");
//int port = 10000;
//IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

//// Создаем TCP сокет
//using Socket client = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

//try
//{
//  // Подключаемся к серверу
//  await client.ConnectAsync(remoteEP);
//  Console.WriteLine("Connected to server.");

//  // Сообщение для отправки
//  string message = "Привет, сервер!";
//  byte[] messageBytes = Encoding.UTF8.GetBytes(message);

//  // Отправляем данные
//  int bytesSent = await client.SendAsync(messageBytes, SocketFlags.None);
//  Console.WriteLine($"Sent {bytesSent} bytes: {message}");

//  // Буфер для ответа
//  byte[] buffer = new byte[1024];
//  int bytesReceived = await client.ReceiveAsync(buffer, SocketFlags.None);

//  string response = Encoding.UTF8.GetString(buffer, 0, bytesReceived);
//  Console.WriteLine($"Received from server: {response}");

//  // Завершаем соединение
//  client.Shutdown(SocketShutdown.Both);
//}
//catch (Exception ex)
//{
//  Console.WriteLine($"Exception: {ex.Message}");
//}


