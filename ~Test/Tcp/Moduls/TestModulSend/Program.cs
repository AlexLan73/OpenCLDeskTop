// See https://aka.ms/new-console-template for more information

using ControlzEx.Standard;
using System;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Common.Core;
using Modules.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Common.Core.Property;
using Modules.Core.TCP;

Console.WriteLine("Test Server - SEND 127.0.0.1 port 20010");


string _pathYaml = "E:\\C#\\OpenCLDeskTop\\Core\\DeskTop\\ipAddresses.yaml";

var _dIp = new ReadWriteYaml(_pathYaml).ReadYaml();

ConcurrentDictionary<int, TcpDuplex> _dTpcDuplexes = new();
foreach (var (key, val1) in _dIp.Where(x=>x.Key==0))
{
  var val = val1;
//  var _ipAddress = val;
  (val.Port1, val.Port2) = (val.Port2, val.Port1);
  _dTpcDuplexes.AddOrUpdate(
    key,
    new TcpDuplex(val), // значение для добавления, если ключ отсутствует
    (Key, Value) => new TcpDuplex(val) // функция обновления, если ключ есть
  );
  _dTpcDuplexes[key].RunSend();
}

string _start = "Start ";
for (int i0 = 0; i0 < 10; i0++)
{
  _start += $"-!- {i0} "; 
  for (int i = 0; i < _dTpcDuplexes.Count; i++)
  {
    _dTpcDuplexes[i].TestSendCommand(new myMessage { Text = $"Port: {_dTpcDuplexes[i].IpAddress.Port1} =>  {_start}", Number = i0 });
  }
}
Thread.Sleep(2000); // Даем серверу время запуститься

for (int i = 0; i < _dTpcDuplexes.Count; i++)
  _dTpcDuplexes[i].Dispose();


int hh = 1;


/*
var _ipAddress = _dIp[1];
(_ipAddress.Port1, _ipAddress.Port2) = (_ipAddress.Port2, _ipAddress.Port1);

TcpDuplex _v0 = new TcpDuplex(_ipAddress);
Task _task = _v0.RunSend();
_v0.TestSendCommand(new myMessage { Text = "start", Number = 0 });
_v0.TestSendCommand(new myMessage { Text = "s-tart", Number = 1 });
_v0.TestSendCommand(new myMessage { Text = "s-t-art", Number = 2 });
_v0.TestSendCommand(new myMessage { Text = "s-t-a-rt", Number = 3 });
_v0.TestSendCommand(new myMessage { Text = "s-t-a-r-t", Number = 4 });
_v0.Dispose();

Task.WaitAll(_task);
int hh = 1;
*/

//IPAddress localIP = IPAddress.Parse("127.0.0.1");
//int recPort = 20011; // фиксированный исходящий порт клиента

//IPAddress serverIP = IPAddress.Parse("127.0.0.1");
//int sendPort = 20010;

///*
//var serverThread = new System.Threading.Thread(TcpYamlServer.RunSend);
//serverThread.Start();

//System.Threading.Thread.Sleep(500); // Даем серверу время запуститься
//*/
//var _client = new TcpYamlClient();
//_client.RunSend();

////serverThread.Join();

public class myClassSend : TcpDuplex
{
  public myClassSend(IpAddressOne ipAddress) : base(ipAddress)
  {
  }
}

public class Message
{
  public string Text { get; set; }
  public int Number { get; set; }
}

public class TcpYamlClient
{
  private TcpClient _clientSend = null;
  private NetworkStream _streamSend = null;
  private Regex _regex;
  private IDeserializer _deserializer;
  private ISerializer _serializer;

  public TcpYamlClient()
  {
    _regex = new Regex(@"[\x00-\x1F\x7F]", RegexOptions.Compiled);
    _deserializer = new DeserializerBuilder()
      .WithNamingConvention(CamelCaseNamingConvention.Instance)
      .Build();
    _serializer = new SerializerBuilder()
      .WithNamingConvention(CamelCaseNamingConvention.Instance)
      .Build();
  }
  private bool ConnectSend()
  {
//  private TcpClient _clientSend = null;
//  private NetworkStream _streamSend = null;

    try
    {
      _clientSend?.Dispose();
      _clientSend = new TcpClient();
      _clientSend.Connect(IPAddress.Loopback, 20010);
      _streamSend = _clientSend.GetStream();
      return true;
    }
    catch
    {
      _clientSend?.Dispose();
      _clientSend = null;
      _streamSend = null;
      return false;
    }
  }

  private bool SendMessage(Message message)
  {
    try
    {
      var yamlToSend = _serializer.Serialize(message);
      var bytesToSend = Encoding.UTF8.GetBytes(yamlToSend);
      var lengthBytes = BitConverter.GetBytes(bytesToSend.Length);

      _streamSend.Write(lengthBytes, 0, 4);
      _streamSend.Write(bytesToSend, 0, bytesToSend.Length);
      _streamSend.Flush();

      // Чтение ответа (пример)
      var responseLengthBytes = new byte[4];
      int read = _streamSend.Read(responseLengthBytes, 0, 4);
      if (read < 4) throw new IOException("Disconnected");

      int responseLength = BitConverter.ToInt32(responseLengthBytes, 0);
      var buffer = new byte[responseLength];
      int totalRead = 0;
      while (totalRead < responseLength)
      {
        int bytesRead = _streamSend.Read(buffer, totalRead, responseLength - totalRead);
        if (bytesRead == 0) throw new IOException("Disconnected");
        totalRead += bytesRead;
      }

      var yamlReceived = Encoding.UTF8.GetString(buffer);
      yamlReceived = _regex.Replace(yamlReceived, "");
      Console.WriteLine($"Received: {yamlReceived}");

      return true;
    }
    catch (IOException)
    {
      // Соединение потеряно — нужно переподключиться
      return false;
    }
    catch (SocketException)
    {
      return false;
    }
  }

public void RunSend()
  {

    var message = new Message { Text = "start", Number = 0 };
    if (!ConnectSend())
    {
      Console.WriteLine("Не удалось подключиться к серверу");
      return;
    }


    for (int i = 0; i < 10; i++)
    {
      if (!SendMessage(message))
      {
        Console.WriteLine("Соединение потеряно, переподключаемся...");
        if (!ConnectSend())
        {
          Console.WriteLine("Не удалось переподключиться, прерываем работу");
          break;
        }
        // Повторить отправку после переподключения
        if (!SendMessage(message))
        {
          Console.WriteLine("Ошибка после переподключения, прерываем работу");
          break;
        }
      }

      message.Number++;
    }
//    client1.Close();
    Console.WriteLine("Client finished.");
  }
}


//using var client = client1;

//using var  client = new TcpClient();
//client1 = client;
//client.Connect(IPAddress.Loopback, 20010);
//using var stream = client.GetStream();


//var yamlToSend = serializer.Serialize(message);
//var bytesToSend = Encoding.UTF8.GetBytes(yamlToSend);

//// Отправляем длину сообщения (4 байта) и само сообщение
//var lengthBytes = BitConverter.GetBytes(bytesToSend.Length);
//stream.Write(lengthBytes, 0, 4);
//stream.Write(bytesToSend, 0, bytesToSend.Length);
//Console.WriteLine($"Sent: Text='{message.Text}', Number={message.Number}");

//// Читаем длину ответа
//var responseLengthBytes = new byte[4];
//stream.Read(responseLengthBytes, 0, 4);
//int responseLength = BitConverter.ToInt32(responseLengthBytes, 0);

//// Читаем YAML-ответ
//var buffer = new byte[responseLength];
//int totalRead = 0;
//while (totalRead < responseLength)
//{
//  int read = stream.Read(buffer, totalRead, responseLength - totalRead);
//  if (read == 0) throw new Exception("Disconnected");
//  totalRead += read;
//}

//var yamlReceived = Encoding.UTF8.GetString(buffer);
//yamlReceived = _regex.Replace(yamlReceived, "");
////message = deserializer.Deserialize<Message>(yamlReceived);
////message.Text += " !!-client";
////message.Number += 1;

//Console.WriteLine($"Received: Text='{yamlReceived}");
//if (yamlReceived=="ok")
//  Console.WriteLine("))))))))))))))");
//else
//  Console.WriteLine("(((((((((((");
