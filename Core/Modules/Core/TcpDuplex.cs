using Common.Core;
using Common.Enum;
using ControlzEx.Standard;
using Microsoft.AspNetCore.Html;
using System;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows.Services.Maps;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Common.Core.Property;

namespace Modules.Core;

public class TcpDuplex
{
  private IDeserializer _deserializer;
  private ISerializer _serializer;
  private IpAddressOne _ipAddress;
  private readonly string _sIpAddress;
  private readonly int _portRead;
  private IPEndPoint _ipEndPointRead;
  private readonly int _portSend;
  private IPEndPoint _ipEndPointSend;
  private TcpListener _listenerRead;
  private CancellationTokenSource _cts;
  private CancellationToken _token;
  private Regex _regex;
  private TcpClient _clientSend = null;
  private NetworkStream _streamSend = null;
  private ConcurrentQueue<(byte[] Send, byte[] mBytes)> _queueSend = new ConcurrentQueue<(byte[], byte[])>();
  private static ManualResetEvent _waitHandle = new ManualResetEvent(false);

  //  private readonly int _countByte = 1024 * 8;
  public TcpDuplex(IpAddressOne ipAddress)
  {
    _regex = new Regex(@"[\x00-\x1F\x7F]", RegexOptions.Compiled);
    ConcurrentQueue<(byte[] First, byte[] Second)> queue = new ConcurrentQueue<(byte[], byte[])>();

    _ipAddress = ipAddress;
    _sIpAddress = ipAddress.IpAddress;
    _portRead = _ipAddress.Port1;  // поот сервера чтение
    _portSend = _ipAddress.Port2;      // порт клиента куда передавать

  _deserializer = new DeserializerBuilder()
      .WithNamingConvention(CamelCaseNamingConvention.Instance)
      .Build();
    _serializer = new SerializerBuilder()
      .WithNamingConvention(CamelCaseNamingConvention.Instance)
      .Build();

    _ipEndPointRead = new IPEndPoint(IPAddress.Parse(_sIpAddress), _portRead);
    _ipEndPointSend = new IPEndPoint(IPAddress.Parse(_sIpAddress), _portSend);

    _listenerRead = new TcpListener(_ipEndPointRead);

    _cts = new CancellationTokenSource();
    _token = _cts.Token;

  }

  public void TestSendCommand(myMessage message)
  {
    var responseYaml = _serializer.Serialize(message);
    SendData(responseYaml);
  }

  protected virtual void SendData(string data)
  {
//    var yamlToSend = _serializer.Serialize(message);
    var bytesToSend = Encoding.UTF8.GetBytes(data);
    var lengthBytes = BitConverter.GetBytes(bytesToSend.Length);
    _queueSend.Enqueue((bytesToSend, lengthBytes));
    _waitHandle.Set(); // Сигнализируем, что есть данные в очереди
  }
  public void StopReadServer()
  {
    _cts.Cancel();
    _listenerRead.Stop();
  }

  protected virtual bool ParserReadString(string sRead)
  {
    var message = _deserializer.Deserialize<myMessage>(sRead);
    Console.WriteLine($"Received: Text='{message.Text}', Number={message.Number}");

    // Обработка
//    message.Text += " server";
//    message.Number += 1;

//    var responseYaml = _serializer.Serialize(message);
//    var responseBytes = Encoding.UTF8.GetBytes(responseYaml);

    return true;
  }

  public async Task RunRead()
  {
    await Task.Run(() =>
    {
      _listenerRead.Start();
      try
      {
        while (!_token.IsCancellationRequested)
        {
          using var clientRead = _listenerRead.AcceptTcpClient();
          // Работа с клиентом
          using var streamRead = clientRead.GetStream();

          var lengthBytes = new byte[4];
          var i = streamRead.Read(lengthBytes, 0, 4);
          var messageLength = BitConverter.ToInt32(lengthBytes, 0);

          // Читаем YAML-сообщение
          var buffer = new byte[messageLength];
          var totalRead = 0;
          while (totalRead < messageLength)
          {
            int read = streamRead.Read(buffer, totalRead, messageLength - totalRead);
            if (read == 0) throw new Exception("Disconnected");
            totalRead += read;
          }

          var ok = "not";
          var yamlString = Encoding.UTF8.GetString(buffer);
          if (ParserReadString(yamlString))
            ok = "ok";
          var responseYaml = _serializer.Serialize(ok);
          var responseBytes = Encoding.UTF8.GetBytes(responseYaml);

          // Отправляем длину и сообщение
          var responseLengthBytes = BitConverter.GetBytes(responseBytes.Length);
          streamRead.Write(responseLengthBytes, 0, 4);
          streamRead.Write(responseBytes, 0, responseBytes.Length);
        }
      }
      catch (SocketException)
      {
        // Слушатель остановлен, выходим из цикла
        _listenerRead.Stop();
//        Console.WriteLine("Server finished.");
        return Task.CompletedTask;

      }
      catch (ObjectDisposedException)
      {
        // Слушатель уничтожен, выходим из цикла
      }

      return Task.CompletedTask;                                                                              
    }, _token);

  }

  private bool ConnectSend()
  {
    try
    {
      _clientSend?.Dispose();
      _clientSend = new TcpClient();
//      _ipEndPointRead = new IPEndPoint(IPAddress.Parse(_sIpAddress), _portRead);
//      _ipEndPointSend = new IPEndPoint(IPAddress.Parse(_sIpAddress), _portSend);
      _clientSend.Connect(_ipEndPointSend);
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
          
  private int SendMessage()
  {

    try
    {
      //      var yamlToSend = _serializer.Serialize(message);
      //      var bytesToSend = Encoding.UTF8.GetBytes(yamlToSend);
      //      var lengthBytes = BitConverter.GetBytes(bytesToSend.Length);

      if (_queueSend.TryPeek(out var tuple)) 
      {
        _streamSend.Write(tuple.mBytes, 0, 4);
        _streamSend.Write(tuple.Send , 0, tuple.Send.Length);
        _streamSend.Flush();
        _queueSend.TryDequeue(out _);
      }
      else
      {
        return -2;
      }

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
      };

      var yamlReceived = _regex.Replace(Encoding.UTF8.GetString(buffer), "");
      Console.WriteLine($"Received: {yamlReceived}");

      return yamlReceived=="ok"?1:-3;
    }
    catch (IOException)
    {
      // Соединение потеряно — нужно переподключиться
      return -1;
    }
    catch (SocketException)
    {
      return -1;
    }
  }

  public async Task RunSend()
  {
    await Task.Run(() =>
    {
      var repeatQueueRead = 3;
      while (!_token.IsCancellationRequested)
      {
        _waitHandle.WaitOne(1000);

        if(!_queueSend.IsEmpty)
        {
          if (!ConnectSend())
          {
            Console.WriteLine("Не удалось подключиться к серверу");
            return Task.CompletedTask;
          }
        }

        while (!_queueSend.IsEmpty)
        {
          var errorCode = SendMessage();
          switch (errorCode)
          {
            case -1:
              if (!ConnectSend())
              {
                Console.WriteLine("Не удалось подключиться к серверу");
                return Task.CompletedTask;
              }
              break;
            case -2:
              // проблема с очередью не читается
              // послать сообщение c кол-вом оставшихся попыток
              repeatQueueRead -= 1;
              if (repeatQueueRead <= 0)
              {
                _queueSend.Clear();
                repeatQueueRead = 3;
              }
              break;
          }
        }
      }
      return Task.CompletedTask;
    }, _token);
    var message = new myMessage { Text = "start", Number = 0 };


    //    client1.Close();
    Console.WriteLine("Client finished.");
  }


}


//if (!ConnectSend())
//{
//  Console.WriteLine("Не удалось подключиться к серверу");
//  return Task.CompletedTask;
//}

//for (int i = 0; i < 10; i++)
//{
//  if (!SendMessage(message))
//  {
//    Console.WriteLine("Соединение потеряно, переподключаемся...");
//    if (!ConnectSend())
//    {
//      Console.WriteLine("Не удалось переподключиться, прерываем работу");
//      break;
//    }
//    // Повторить отправку после переподключения
//    if (!SendMessage(message))
//    {
//      Console.WriteLine("Ошибка после переподключения, прерываем работу");
//      break;
//    }
//  }
//}
