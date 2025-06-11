using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Modules.Core.TCP;

public class TcpDuplex:IDisposable
{
  public Task TaskRead;
  public Task TaskSend;

  private readonly IDeserializer _deserializer;
  private readonly ISerializer _serializer;
  private readonly IPEndPoint _ipEndPointSend;
  private readonly TcpListener _listenerRead;
  private readonly CancellationTokenSource _cts;
  private readonly CancellationToken _token;
  private readonly Regex _regex;
  private TcpClient _clientSend;
  private NetworkStream _streamSend;
  private readonly ConcurrentQueue<(byte[] Send, byte[] mBytes)> _queueSend = new ConcurrentQueue<(byte[], byte[])>();
  private readonly ManualResetEvent _waitHandle = new ManualResetEvent(false);
  public IpAddressOne IpAddress { get; private set; }
  private Func<string, bool> _parserExternalFunction;
  public TcpDuplex(IpAddressOne ipAddress)
  {
    IpAddress = ipAddress;
    _regex = new Regex(@"[\x00-\x1F\x7F]", RegexOptions.Compiled);

    var sIpAddress = ipAddress.IpAddress;
    var portRead = ipAddress.Port1;   // поот сервера чтение
    var portSend = ipAddress.Port2;   // порт клиента куда передавать

    _deserializer = new DeserializerBuilder()
      .WithNamingConvention(CamelCaseNamingConvention.Instance)
      .Build();
    _serializer = new SerializerBuilder()
      .WithNamingConvention(CamelCaseNamingConvention.Instance)
      .Build();

    var ipEndPointRead = new IPEndPoint(IPAddress.Parse(sIpAddress), portRead);
    _ipEndPointSend = new IPEndPoint(IPAddress.Parse(sIpAddress), portSend);

    _listenerRead = new TcpListener(ipEndPointRead);

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
    var bytesToSend = Encoding.UTF8.GetBytes(data);
    var lengthBytes = BitConverter.GetBytes(bytesToSend.Length);
    _queueSend.Enqueue((bytesToSend, lengthBytes));
    _waitHandle.Set();     // Сигнализируем, что есть данные в очереди
  }
  public void InitializationExternalFunction(Func<string, bool> parserFunc) =>_parserExternalFunction = parserFunc;
  
  protected virtual bool ParserReadString(string sRead)
  {
    if (_parserExternalFunction != null)
      // Вызов переданной функции
      return _parserExternalFunction(sRead);
    else
    {
      try
      {
        var message = _deserializer.Deserialize<myMessage>(sRead);
        Console.WriteLine($"Port: {IpAddress.Port1};   Received: Text='{message.Text}', Number={message.Number}");
        return true;
      }
      catch (Exception e)
      {
        Console.WriteLine("Выполняется стандартный парсинг: " + sRead);
        return true; // или другая логика
      }
    }
    return false;
  }
  protected virtual bool ParserReadString1(string sRead)
  {
    var message = _deserializer.Deserialize<myMessage>(sRead);
    Console.WriteLine($"Port: {IpAddress.Port1};   Received: Text='{message.Text}', Number={message.Number}");

    return true;
  }

  public void RunRead()
  {
    Console.WriteLine($"start  Port: {IpAddress.Port1};   Name: {IpAddress.Name}");

    TaskRead =  Task.Run(() =>
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
          // ReSharper disable once UnusedVariable
          var i = streamRead.Read(lengthBytes, 0, 4);
          var messageLength = BitConverter.ToInt32(lengthBytes, 0);

          // Читаем YAML-сообщение
          var buffer = new byte[messageLength];
          var totalRead = 0;
          while (totalRead < messageLength)
          {
            var read = streamRead.Read(buffer, totalRead, messageLength - totalRead);
            if (read == 0)
            {
              //throw new Exception("Disconnected");
              break;
            }
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
        Console.WriteLine($"STOP  Port: {IpAddress.Port1};   Name: {IpAddress.Name}");
        return Task.CompletedTask;

      }
      catch (ObjectDisposedException)
      {
        // Слушатель уничтожен, выходим из цикла
      }

      Console.WriteLine($"STOP  Port: {IpAddress.Port1};   Name: {IpAddress.Name}");

      return Task.CompletedTask;                                                                              
    }, _token);

  }
  private bool ConnectSend()
  {
    try
    {
      _clientSend?.Dispose();
      _clientSend = new TcpClient();
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
      var read = _streamSend.Read(responseLengthBytes, 0, 4);
      if (read < 4) throw new IOException("Disconnected");

      var responseLength = BitConverter.ToInt32(responseLengthBytes, 0);
      var buffer = new byte[responseLength];
      var totalRead = 0;
      while (totalRead < responseLength)
      {
        var bytesRead = _streamSend.Read(buffer, totalRead, responseLength - totalRead);
        if (bytesRead == 0) throw new IOException("Disconnected");
        totalRead += bytesRead;
      }

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
  public void RunSend()
  {
    TaskSend = Task.Run(() =>
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

    //    client1.Close();
    Console.WriteLine("Client finished.");
  }
  public void Dispose()
  {
    _cts.Cancel();
    _listenerRead.Stop();
    _listenerRead?.Dispose();
    _cts?.Dispose();
    _clientSend?.Dispose();
    _streamSend?.Dispose();
    if(TaskRead==null || TaskRead.Status == TaskStatus.Faulted)
      return;
    Task.WaitAll(TaskRead);
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
