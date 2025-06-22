// See https://aka.ms/new-console-template for more information
using Common.Core.Property;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Interop;
using WatsonTcp;
using Windows.Media.Protection.PlayReady;
using YamlDotNet.Serialization;

Console.WriteLine("WatsonTcpClient!");

var _serializer = new SerializerBuilder().Build();

//var responseYaml = _serializer.Serialize(message);

var router = new PortEventRouter();

// Регистрируем специальные обработчики для портов
router.RegisterHandler(20000, new Port20000Handler());
router.RegisterHandler(20010, new Port20010Handler()); // Пример другого обработчика
router.RegisterHandler(20020, new Port20020Handler()); // Пример другого обработчика
router.RegisterHandler(20030, new Port20030Handler()); // Пример другого обработчика
router.RegisterHandler(20040, new Port20040Handler()); // Пример другого обработчика

var clients = new List<NetworkClient>
        {
            new NetworkClient("127.0.0.1", 20000, router),
            new NetworkClient("127.0.0.1", 20010, router),
            new NetworkClient("127.0.0.1", 20020, router),
            new NetworkClient("127.0.0.1", 20030, router),
            new NetworkClient("127.0.0.1", 20040, router) // Будет использовать дефолтную обработку
        };

foreach (var client in clients)
{
    client.Connect();
    await client.SendAsync("Тестовое сообщение");
}
await clients[0].SendAsync(_serializer.Serialize(new myMessage() { Text = "START -0 ", Number = 101 }));
await clients[1].SendAsync(_serializer.Serialize(new myMessage() { Text = "START -1 ", Number = 102 }));
await clients[2].SendAsync(_serializer.Serialize(new myMessage() { Text = "START -2 ", Number = 103 }));
await clients[3].SendAsync(_serializer.Serialize(new myMessage() { Text = "START -3 ", Number = 104 }));
await clients[4].SendAsync(_serializer.Serialize(new myMessage() { Text = "START -4 ", Number = 105 }));
Console.ReadLine();


public interface IPortEventHandler
{
    void OnConnected(Guid clientId, string server, int port);
    void OnDisconnected(Guid clientId, string server, int port, DisconnectReason reason);
    void OnMessageReceived(Guid clientId, string server, int port, string message);
    void OnError(Guid clientId, string server, int port, Exception exception);
}

public class NetworkClient : IDisposable
{
    private readonly IPortEventHandler _handler;
    public WatsonTcpClient Client { get; }
    public string ServerAddress { get; }
    public int ServerPort { get; }
    public Guid ClientId => Client.Settings.Guid;

    public NetworkClient(string address, int port, IPortEventHandler handler)
    {
        ServerAddress = address;
        ServerPort = port;
        _handler = handler;
        Client = new WatsonTcpClient(address, port);

        Client.Events.ServerConnected += OnConnected;
        Client.Events.ServerDisconnected += OnDisconnected;
        Client.Events.MessageReceived += OnMessageReceived;
        Client.Events.ExceptionEncountered += OnException;
    }

    public void Connect() => Client.Connect();
    public async Task SendAsync(string message) => await Client.SendAsync(message);
    public void Dispose() => Client?.Dispose();

    private void OnConnected(object sender, ConnectionEventArgs e)
        => _handler.OnConnected(ClientId, ServerAddress, ServerPort);

    private void OnDisconnected(object sender, DisconnectionEventArgs e)
        => _handler.OnDisconnected(ClientId, ServerAddress, ServerPort, e.Reason);

    private void OnMessageReceived(object sender, MessageReceivedEventArgs e)
        => _handler.OnMessageReceived(ClientId, ServerAddress, ServerPort, Encoding.UTF8.GetString(e.Data));

    private void OnException(object sender, ExceptionEventArgs e)
        => _handler.OnError(ClientId, ServerAddress, ServerPort, e.Exception);
}

public class PortEventRouter : IPortEventHandler
{
    private readonly ConcurrentDictionary<int, IPortEventHandler> _portHandlers = new();

    public void RegisterHandler(int port, IPortEventHandler handler)
        => _portHandlers.TryAdd(port, handler);

    public void OnConnected(Guid clientId, string server, int port)
    {
        if (_portHandlers.TryGetValue(port, out var handler))
            handler.OnConnected(clientId, server, port);
        else
            Console.WriteLine($"[DEFAULT] Клиент {clientId} подключен к {server}:{port}");
    }

    public void OnDisconnected(Guid clientId, string server, int port, DisconnectReason reason)
    {
        if (_portHandlers.TryGetValue(port, out var handler))
            handler.OnDisconnected(clientId, server, port, reason);
        else
            Console.WriteLine($"[DEFAULT] Клиент {clientId} отключен от {server}:{port} (Причина: {reason})");
    }

    public void OnMessageReceived(Guid clientId, string server, int port, string message)
    {
        if (_portHandlers.TryGetValue(port, out var handler))
            handler.OnMessageReceived(clientId, server, port, message);
        else
            Console.WriteLine($"[DEFAULT] Получено от {clientId}@{server}:{port}: {message}");
    }

    public void OnError(Guid clientId, string server, int port, Exception exception)
    {
        if (_portHandlers.TryGetValue(port, out var handler))
            handler.OnError(clientId, server, port, exception);
        else
            Console.WriteLine($"[DEFAULT] Ошибка у {clientId}@{server}:{port}: {exception.Message}");
    }
}

// Пример специализированного обработчика для порта 20000
public class Port20000Handler : IPortEventHandler
{
    public void OnConnected(Guid clientId, string server, int port)
        => Console.WriteLine($"[SPECIAL 20000] Установлено соединение с клиентом {clientId}");

    public void OnDisconnected(Guid clientId, string server, int port, DisconnectReason reason)
        => Console.WriteLine($"[SPECIAL 20000] Разрыв соединения с {clientId} (Причина: {reason})");

    public void OnMessageReceived(Guid clientId, string server, int port, string message)
    {
        Console.WriteLine($"[SPECIAL 20000] Сообщение от {clientId}: {message}");
        // Специфичная логика обработки для порта 20000
    }

    public void OnError(Guid clientId, string server, int port, Exception exception)
        => Console.WriteLine($"[SPECIAL 20000] Критическая ошибка: {exception.Message}");
}
// Пример специализированного обработчика для порта 20010
public class Port20010Handler : IPortEventHandler
{
    public void OnConnected(Guid clientId, string server, int port)
        => Console.WriteLine($"[SPECIAL 20010] Установлено соединение с клиентом {clientId}");

    public void OnDisconnected(Guid clientId, string server, int port, DisconnectReason reason)
        => Console.WriteLine($"[SPECIAL 20010] Разрыв соединения с {clientId} (Причина: {reason})");

    public void OnMessageReceived(Guid clientId, string server, int port, string message)
    {
        Console.WriteLine($"[SPECIAL 20010] Сообщение от {clientId}: {message}");
        // Специфичная логика обработки для порта 20000
    }
    public void OnError(Guid clientId, string server, int port, Exception exception)
        => Console.WriteLine($"[SPECIAL 20010] Критическая ошибка: {exception.Message}");
}
// Пример специализированного обработчика для порта 20020
public class Port20020Handler : IPortEventHandler
{
    public void OnConnected(Guid clientId, string server, int port)
        => Console.WriteLine($"[SPECIAL 20020] Установлено соединение с клиентом {clientId}");

    public void OnDisconnected(Guid clientId, string server, int port, DisconnectReason reason)
        => Console.WriteLine($"[SPECIAL 20020] Разрыв соединения с {clientId} (Причина: {reason})");

    public void OnMessageReceived(Guid clientId, string server, int port, string message)
    {
        Console.WriteLine($"[SPECIAL 20020] Сообщение от {clientId}: {message}");
        // Специфичная логика обработки для порта 20000
    }

    public void OnError(Guid clientId, string server, int port, Exception exception)
        => Console.WriteLine($"[SPECIAL 20020] Критическая ошибка: {exception.Message}");
}
// Пример специализированного обработчика для порта 20030
public class Port20030Handler : IPortEventHandler
{
    public void OnConnected(Guid clientId, string server, int port)
        => Console.WriteLine($"[SPECIAL 20030] Установлено соединение с клиентом {clientId}");

    public void OnDisconnected(Guid clientId, string server, int port, DisconnectReason reason)
        => Console.WriteLine($"[SPECIAL 20030] Разрыв соединения с {clientId} (Причина: {reason})");

    public void OnMessageReceived(Guid clientId, string server, int port, string message)
    {
        Console.WriteLine($"[SPECIAL 20030] Сообщение от {clientId}: {message}");
        // Специфичная логика обработки для порта 20000
    }

    public void OnError(Guid clientId, string server, int port, Exception exception)
        => Console.WriteLine($"[SPECIAL 20030] Критическая ошибка: {exception.Message}");
}
// Пример специализированного обработчика для порта 20040
public class Port20040Handler : IPortEventHandler
{
    public void OnConnected(Guid clientId, string server, int port)
        => Console.WriteLine($"[SPECIAL 20040] Установлено соединение с клиентом {clientId}");

    public void OnDisconnected(Guid clientId, string server, int port, DisconnectReason reason)
        => Console.WriteLine($"[SPECIAL 20040] Разрыв соединения с {clientId} (Причина: {reason})");

    public void OnMessageReceived(Guid clientId, string server, int port, string message)
    {
        Console.WriteLine($"[SPECIAL 20040] Сообщение от {clientId}: {message}");
        // Специфичная логика обработки для порта 20000
    }
    public void OnError(Guid clientId, string server, int port, Exception exception)
        => Console.WriteLine($"[SPECIAL 20040] Критическая ошибка: {exception.Message}");
}



/*
===>>  РАБОЧИЙ ВАРИАНТ <<===

// Конфигурация подключений (сервер -> порты)
var serverEndpoints = new Dictionary<string, int[]>
{
    ["127.0.0.1"] = new[] { 20000, 20010, 20020, 20030, 20040 }
    // другой IP:Port
//    ,    ["192.168.1.100"] = new[] { 21000, 21010 } // Пример внешнего сервера
};

using var pool = new ClientPool();

// Создаем по 3 клиента для каждого порта каждого сервера
pool.CreateClients(serverEndpoints, clientsPerEndpoint: 1);

Console.WriteLine("Все клиенты запущены. Нажмите Enter для остановки...");
Console.ReadLine();


public class NetworkClient : IDisposable
{
    public WatsonTcpClient Client { get; }
    public string ServerAddress { get; }
    public int ServerPort { get; }
    public Guid ClientId => Client.Settings.Guid;

    public NetworkClient(string address, int port)
    {
        ServerAddress = address;
        ServerPort = port;
        Client = new WatsonTcpClient(address, port);

        Client.Events.ServerConnected += OnConnected;
        Client.Events.ServerDisconnected += OnDisconnected;
        Client.Events.MessageReceived += OnMessageReceived;
        Client.Events.ExceptionEncountered += OnException;
    }

    public void Connect() => Client.Connect();
    public async Task SendAsync(string message) => await Client.SendAsync(message);
    public void Dispose() => Client?.Dispose();

    private void OnConnected(object sender, ConnectionEventArgs e)
        => Console.WriteLine($"[Client {ClientId}] Подключено к {ServerAddress}:{ServerPort}");

    private void OnDisconnected(object sender, DisconnectionEventArgs e)
        => Console.WriteLine($"[Client {ClientId}] Отключено: {e.Reason}");

    private void OnMessageReceived(object sender, MessageReceivedEventArgs e)
        => Console.WriteLine($"[Client {ClientId}] Получено: {Encoding.UTF8.GetString(e.Data)}");

    private void OnException(object sender, ExceptionEventArgs e)
        => Console.WriteLine($"[Client {ClientId}] Ошибка: {e.Exception.Message}");
}

public class ClientPool : IDisposable
{
    private readonly List<NetworkClient> _clients = new List<NetworkClient>();
    private readonly CancellationTokenSource _cts = new CancellationTokenSource();

    public void CreateClients(Dictionary<string, int[]> serverEndpoints, int clientsPerEndpoint)
    {
        foreach (var endpoint in serverEndpoints)
        {
            string ip = endpoint.Key;
            foreach (int port in endpoint.Value)
            {
                for (int i = 0; i < clientsPerEndpoint; i++)
                {
                    var client = new NetworkClient(ip, port);
                    client.Connect();
                    _clients.Add(client);
                    Console.WriteLine($"Создан клиент #{_clients.Count} к {ip}:{port}");

                    // Имитация работы клиента
                    _ = Task.Run(() => ClientWorker(client, _cts.Token));
                }
            }
        }
    }

    private async Task ClientWorker(NetworkClient client, CancellationToken ct)
    {
        try
        {
            var rnd = new Random();
            while (!ct.IsCancellationRequested)
            {
                await client.SendAsync($"Сообщение от {client.ClientId} в {DateTime.Now:T}");
                await Task.Delay(rnd.Next(1000, 5000), ct);
            }
        }
        catch (OperationCanceledException) {  } /// Корректное завершение 
        catch (Exception ex)
        {
            Console.WriteLine($"[ОШИБКА] Клиент {client.ClientId}: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        foreach (var client in _clients)
        {
            client?.Dispose();
        }
    }
}

*/






/*
===>>  РАБОЧИЙ ВАРИАНТ <<===

// Создаём клиента, подключаемся к 127.0.0.1:9000
WatsonTcpClient client = new WatsonTcpClient("127.0.0.1", 20000);

client.Events.ServerConnected += (s, e) =>
    Console.WriteLine("Подключено к серверу");

client.Events.ServerDisconnected += (s, e) =>
    Console.WriteLine("Отключено от сервера");

client.Events.MessageReceived += (s, e) =>
    Console.WriteLine("Сообщение от сервера: " + Encoding.UTF8.GetString(e.Data));

client.Connect();

for (int i = 0; i < 50; i++)
{
    client.SendAsync($"  {i}  START 20.000");

}
//Console.WriteLine("Введите сообщение для отправки серверу:");
//string msg = Console.ReadLine();
//client.SendAsync(msg);

Console.WriteLine("Для выхода нажмите Enter.");
Console.ReadLine();

*/