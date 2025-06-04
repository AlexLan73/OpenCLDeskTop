// See https://aka.ms/new-console-template for more information
//// Создаем локальную конечную точку с IP 127.0.1.1 и портом 20000

using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;


var server = new TcpServer("127.0.0.1", 20000);
_ = server.StartAsync();

Console.WriteLine("Нажмите Enter для отправки сообщения клиенту с портом 20010");
Console.ReadLine();

await server.SendToClientAsync(20010, "Привет, клиент 20010!");


class TcpServer
{
  private TcpListener listener;
  // Хранилище клиентов: ключ — порт клиента, значение — TcpClient
  private ConcurrentDictionary<int, TcpClient> clients = new ConcurrentDictionary<int, TcpClient>();

  public TcpServer(string ip, int port)
  {
    listener = new TcpListener(IPAddress.Parse(ip), port);
  }

  public async Task StartAsync()
  {
    listener.Start();
    Console.WriteLine($"Сервер запущен на {listener.LocalEndpoint}");

    while (true)
    {
      TcpClient client = await listener.AcceptTcpClientAsync();
      int clientPort = ((IPEndPoint)client.Client.RemoteEndPoint).Port;

      Console.WriteLine($"Подключен клиент с портом {clientPort}");

      clients[clientPort] = client;

      // Обрабатываем клиента в отдельной задаче
      _ = HandleClientAsync(client, clientPort);
    }
  }

  private async Task HandleClientAsync(TcpClient client, int clientPort)
  {
    NetworkStream stream = client.GetStream();
    byte[] buffer = new byte[1024];

    try
    {
      while (true)
      {
        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
        if (bytesRead == 0) break; // Клиент закрыл соединение

        string received = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        Console.WriteLine($"Получено от {clientPort}: {received}");

        // Здесь можно обработать данные и отправить ответ
        // Например, отправим подтверждение
        string response = $"Сервер получил сообщение: {received}";
        byte[] responseBytes = Encoding.UTF8.GetBytes(response);
        await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Ошибка с клиентом {clientPort}: {ex.Message}");
    }
    finally
    {
      clients.TryRemove(clientPort, out _);
      client.Close();
      Console.WriteLine($"Клиент {clientPort} отключился");
    }
  }

  // Метод для отправки сообщения конкретному клиенту по его порту
  public async Task SendToClientAsync(int clientPort, string message)
  {
    if (clients.TryGetValue(clientPort, out TcpClient client))
    {
      NetworkStream stream = client.GetStream();
      byte[] msgBytes = Encoding.UTF8.GetBytes(message);
      await stream.WriteAsync(msgBytes, 0, msgBytes.Length);
      Console.WriteLine($"Отправлено клиенту {clientPort}: {message}");
    }
    else
    {
      Console.WriteLine($"Клиент с портом {clientPort} не найден");
    }
  }
}

