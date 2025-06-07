// See https://aka.ms/new-console-template for more information
//// Создаем локальную конечную точку с IP 127.0.1.1 и портом 20000

using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;


// Создаём словарь с данными
Dictionary<int, IpAddressDict> ipDict = new Dictionary<int, IpAddressDict>
{
  {0, new IpAddressDict { Id = 0, Name = "Server", IpAddress = "127.0.0.1", Port = 20000 }},
  {1, new IpAddressDict { Id = 1, Name = "TclFFT", IpAddress = "127.0.0.1", Port = 20010 }},
  {2, new IpAddressDict { Id = 2, Name = "TmFFT", IpAddress = "127.0.0.1", Port = 20020 }},
  {3, new IpAddressDict { Id = 3, Name = "OpenCL", IpAddress = "127.0.0.1", Port = 20030 }},
  {4, new IpAddressDict { Id = 4, Name = "CUDA", IpAddress = "127.0.0.1", Port = 20040 }}
};

string filePath = "ipAddresses.yaml";

// Создаём сериализатор с CamelCase конвенцией
var serializer = new SerializerBuilder()
  .WithNamingConvention(CamelCaseNamingConvention.Instance)
  .Build();

// Сохраняем словарь в YAML-файл
using (var writer = new StreamWriter(filePath))
{
  serializer.Serialize(writer, ipDict);
}

Console.WriteLine($"Данные сохранены в файл {filePath}");

// Создаём десериализатор с той же конвенцией
var deserializer = new DeserializerBuilder()
  .WithNamingConvention(CamelCaseNamingConvention.Instance)
  .Build();

// Читаем YAML из файла и десериализуем обратно в словарь
Dictionary<int, IpAddressDict> loadedDict;
using (var reader = new StreamReader(filePath))
{
  loadedDict = deserializer.Deserialize<Dictionary<int, IpAddressDict>>(reader);
}

Console.WriteLine("Данные, считанные из файла:");
foreach (var kvp in loadedDict)
{
  Console.WriteLine($"Key: {kvp.Key}, Name: {kvp.Value.Name}, IP: {kvp.Value.IpAddress}, Port: {kvp.Value.Port}");
}


var server = new TcpServer("127.0.0.1", 20000);
_ = server.StartAsync();

Console.WriteLine("Нажмите Enter для отправки сообщения клиенту с портом 20010");
Console.ReadLine();

await server.SendToClientAsync(20010, "Привет, клиент 20010!");

class IpAddressDict
{
  public int Id { get; set; }
  public string Name { get; set; }
  public string IpAddress { get; set; }
  public int Port { get; set; }
}


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

/*
   using YamlDotNet.Serialization;
   using YamlDotNet.Serialization.NamingConventions;
   ...
   
    var person = new Person
   {
       Name = "Abe Lincoln",
       Age = 25,
       HeightInInches = 6f + 4f / 12f,
       Addresses = new Dictionary<string, Address>{
           { "home", new  Address() {
                   Street = "2720  Sundown Lane",
                   City = "Kentucketsville",
                   State = "Calousiyorkida",
                   Zip = "99978",
               }},
           { "work", new  Address() {
                   Street = "1600 Pennsylvania Avenue NW",
                   City = "Washington",
                   State = "District of Columbia",
                   Zip = "20500",
               }},
       }
   };
   
   var serializer = new SerializerBuilder()
       .WithNamingConvention(CamelCaseNamingConvention.Instance)
       .Build();
   var yaml = serializer.Serialize(person);
   System.Console.WriteLine(yaml);
   // Output: 
   // name: Abe Lincoln
   // age: 25
   // heightInInches: 6.3333334922790527
   // addresses:
   //   home:
   //     street: 2720  Sundown Lane
   //     city: Kentucketsville
   //     state: Calousiyorkida
   //     zip: 99978
   //   work:
   //     street: 1600 Pennsylvania Avenue NW
   //     city: Washington
   //     state: District of Columbia
   //     zip: 20500


using YamlDotNet.Serialization;
   using YamlDotNet.Serialization.NamingConventions;
   ...
   
   var yml = @"
   name: George Washington
   age: 89
   height_in_inches: 5.75
   addresses:
     home:
       street: 400 Mockingbird Lane
       city: Louaryland
       state: Hawidaho
       zip: 99970
   ";
   
   var deserializer = new DeserializerBuilder()
       .WithNamingConvention(UnderscoredNamingConvention.Instance)  // see height_in_inches in sample yml 
       .Build();
   
   //yml contains a string containing your YAML
   var p = deserializer.Deserialize<Person>(yml);
   var h = p.Addresses["home"];
   System.Console.WriteLine($"{p.Name} is {p.Age} years old and lives at {h.Street} in {h.City}, {h.State}.");
   // Output:
   // George Washington is 89 years old and lives at 400 Mockingbird Lane in Louaryland, Hawidaho.
 */ 