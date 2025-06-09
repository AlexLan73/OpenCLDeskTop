using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;
using Common.Core.Property;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
namespace Modules.Core.TCP;

public class TestSocketPrimer
{
  public void Run()
  {
    var serverThread = new Thread(TcpYamlServer.Run);
    serverThread.Start();

    Thread.Sleep(500); // Даем серверу время запуститься

    TcpYamlClient.Run();

    serverThread.Join();
  }
}



class TcpYamlServer
{
  public static void Run()
  {
    var deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();
    var serializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    var ipEndPoint = new IPEndPoint(IPAddress.Loopback, 20010);
    var listener = new TcpListener(ipEndPoint);
    listener.Start();
    Console.WriteLine("Server started, waiting for connection...");

    using var client = listener.AcceptTcpClient();
    using var stream = client.GetStream();

    for (int i = 0; i < 10; i++)
    {
      // Читаем длину сообщения (4 байта, int)
      var lengthBytes = new byte[4];
      stream.Read(lengthBytes, 0, 4);
      int messageLength = BitConverter.ToInt32(lengthBytes, 0);

      // Читаем YAML-сообщение
      var buffer = new byte[messageLength];
      int totalRead = 0;
      while (totalRead < messageLength)
      {
        int read = stream.Read(buffer, totalRead, messageLength - totalRead);
        if (read == 0) throw new Exception("Disconnected");
        totalRead += read;
      }

      var yamlString = Encoding.UTF8.GetString(buffer);
      var message = deserializer.Deserialize<myMessage>(yamlString);
      Console.WriteLine($"Received: Text='{message.Text}', Number={message.Number}");

      // Обработка
      message.Text += " server";
      message.Number += 1;

      var responseYaml = serializer.Serialize(message);
      var responseBytes = Encoding.UTF8.GetBytes(responseYaml);

      // Отправляем длину и сообщение
      var responseLengthBytes = BitConverter.GetBytes(responseBytes.Length);
      stream.Write(responseLengthBytes, 0, 4);
      stream.Write(responseBytes, 0, responseBytes.Length);
      Console.WriteLine($"Sent: Text='{message.Text}', Number={message.Number}");
    }

    listener.Stop();
    Console.WriteLine("Server finished.");
  }
}

class TcpYamlClient
{
  public static void Run()
  {
    var deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();
    var serializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    var client = new TcpClient();
    client.Connect(IPAddress.Loopback, 20010);
    using var stream = client.GetStream();

    var message = new myMessage { Text = "start", Number = 0 };

    for (int i = 0; i < 10; i++)
    {
      var yamlToSend = serializer.Serialize(message);
      var bytesToSend = Encoding.UTF8.GetBytes(yamlToSend);

      // Отправляем длину сообщения (4 байта) и само сообщение
      var lengthBytes = BitConverter.GetBytes(bytesToSend.Length);
      stream.Write(lengthBytes, 0, 4);
      stream.Write(bytesToSend, 0, bytesToSend.Length);
      Console.WriteLine($"Sent: Text='{message.Text}', Number={message.Number}");

      // Читаем длину ответа
      var responseLengthBytes = new byte[4];
      stream.Read(responseLengthBytes, 0, 4);
      int responseLength = BitConverter.ToInt32(responseLengthBytes, 0);

      // Читаем YAML-ответ
      var buffer = new byte[responseLength];
      int totalRead = 0;
      while (totalRead < responseLength)
      {
        int read = stream.Read(buffer, totalRead, responseLength - totalRead);
        if (read == 0) throw new Exception("Disconnected");
        totalRead += read;
      }

      var yamlReceived = Encoding.UTF8.GetString(buffer);
      message = deserializer.Deserialize<myMessage>(yamlReceived);
      message.Text += " !!-client";
      message.Number += 1;

      Console.WriteLine($"Received: Text='{message.Text}', Number={message.Number}");
    }

    client.Close();
    Console.WriteLine("Client finished.");
  }
}
