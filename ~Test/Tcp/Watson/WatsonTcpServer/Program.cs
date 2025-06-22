// See https://aka.ms/new-console-template for more information

using System;
using System.Text;
using WatsonTcp;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WatsonTcp;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

Console.WriteLine("WatsonTcpServer!");



// Основной класс программы
List<WatsonTcpServer> _servers = new List<WatsonTcpServer>();
 CancellationTokenSource _cts = new CancellationTokenSource();
 IDeserializer _yamlDeserializer = new DeserializerBuilder().Build();
//    .WithNamingConvention(CamelCaseNamingConvention.Instance)


        Console.WriteLine("Запуск YAML-сервера WatsonTcp...");

        // Порты и соответствующие обработчики
        var portHandlers = new Dictionary<int, IPortHandler>
        {
            [20000] = new Port20000Handler(),
            [20010] = new Port20010Handler(),
            [20020] = new Port20020Handler(),
            [20030] = new Port20030Handler(),
            [20040] = new Port20040Handler(),
            [20050] = new LoggingOnlyHandler(),
            [20060] = new DefaultPortHandler()

        };

        // Запуск серверов
        foreach (var portHandler in portHandlers)
        {
            StartServer(portHandler.Key, portHandler.Value);
        }

        Console.WriteLine("Серверы запущены. Нажмите Enter для остановки.");
        Console.ReadLine();

        _cts.Cancel();
        foreach (var server in _servers)
        {
            server?.Dispose();
        }

void StartServer(int port, IPortHandler handler)
    {
        var server = new WatsonTcpServer("127.0.0.1", port);
        _servers.Add(server);

//        server.Events.ClientConnected += (s, e) =>
//            handler.OnClientConnected(e.Client.IpPort, e.Client.Guid);

        server.Events.ClientDisconnected += (s, e) =>
            handler.OnClientDisconnected(e.Client.IpPort, e.Reason);

        server.Events.ClientConnected += (sender, e) =>
        {
            Console.WriteLine($"✅ Клиент подключен: {e.Client.IpPort}");
            Console.WriteLine($"   ID подключения: {e.Client.Guid}");
            Console.WriteLine($"   Время: {DateTime.Now:HH:mm:ss}");

            // Дополнительная проверка
            if (server.IsClientConnected(e.Client.Guid))
            {
                Console.WriteLine("   Статус: Подтверждено (IsClientConnected = true)");
            }
            else
            {
                Console.WriteLine("   ⚠️ Ошибка: клиент не подтвержден сервером");
            }
        };


    server.Events.MessageReceived += async (s, e) =>
        {
            try
            {
                string yaml = Encoding.UTF8.GetString(e.Data);
                var message = _yamlDeserializer.Deserialize<MyMessage>(yaml);

                // Обработка сообщения в зависимости от порта
                var response = handler.ProcessMessage(message, e.Client.IpPort);

                if (!string.IsNullOrEmpty(response))
                {
                    // Правильный вызов SendAsync для последних версий WatsonTcp
                    await server.SendAsync(e.Client.Guid, Encoding.UTF8.GetBytes(response));

                    // Альтернативный вариант с metadata (если нужно)
                    // await server.SendAsync(e.Client.Guid, Encoding.UTF8.GetBytes(response), metadata: null);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{port}] Ошибка обработки: {ex.Message}");
                handler.OnError(ex);
            }
        };

    /*
            server.Events.MessageReceived += async (s, e) =>
            {
                try
                {
                    string yaml = Encoding.UTF8.GetString(e.Data);
                    var message = _yamlDeserializer.Deserialize<MyMessage>(yaml);

                    // Обработка сообщения в зависимости от порта
                    var response = handler.ProcessMessage(message, e.Client.IpPort);

                    if (!string.IsNullOrEmpty(response))
                    {
                        await server.SendAsync(
                            e.Client.Guid,
                            Encoding.UTF8.GetBytes(response), // Должен быть byte[]
                            null,  // metadata
                            _cts.Token);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{port}] Ошибка обработки: {ex.Message}");
                    handler.OnError(ex);
                }
            };
    */
    server.Events.ExceptionEncountered += (s, e) =>
            handler.OnError(e.Exception);

        ThreadPool.QueueUserWorkItem(_ =>
        {
            server.Start();
            Console.WriteLine($"[{port}] Сервер запущен с обработчиком {handler.GetType().Name}");

            while (!_cts.IsCancellationRequested)
            {
                Thread.Sleep(1000);
            }
        });
    }


// Модель сообщения
public class MyMessage
{
    public string Text { get; set; }
    public int Number { get; set; }
}

// Интерфейс для обработчиков портов
public interface IPortHandler
{
    void OnClientConnected(string ipPort, Guid guid);
    void OnClientDisconnected(string ipPort, DisconnectReason reason);
    string ProcessMessage(MyMessage message, string ipPort);
    void OnError(Exception ex);
}

// Примеры специализированных обработчиков
public class Port20000Handler : IPortHandler
{
    public void OnClientConnected(string ipPort, Guid guid)
        => Console.WriteLine($"[20000] Специальное подключение: {ipPort}");

    public void OnClientDisconnected(string ipPort, DisconnectReason reason)
        => Console.WriteLine($"[20000] Отключение (код: {reason})");

    public string ProcessMessage(MyMessage message, string ipPort)
    {
        Console.WriteLine($"[20000] Обработка: {message.Text} | {message.Number}");
        return $"PORT20000_OK: {message.Text.ToUpper()}";
    }

    public void OnError(Exception ex)
        => Console.WriteLine($"[20000] Критическая ошибка: {ex.Message}");
//    public void On

}

public class Port20010Handler : IPortHandler
{
    public void OnClientConnected(string ipPort, Guid guid)
        => Console.WriteLine($"[20010] Специальное подключение: {ipPort}");

    public void OnClientDisconnected(string ipPort, DisconnectReason reason)
        => Console.WriteLine($"[20010] Отключение (код: {reason})");

    public string ProcessMessage(MyMessage message, string ipPort)
    {
        Console.WriteLine($"[20010] Обработка: {message.Text} | {message.Number}");
        return $"PORT20010_OK: {message.Text.ToUpper()}";
    }

    public void OnError(Exception ex)
        => Console.WriteLine($"[20010] Критическая ошибка: {ex.Message}");
}

public class Port20020Handler : IPortHandler
{
    public void OnClientConnected(string ipPort, Guid guid)
        => Console.WriteLine($"[20020] Специальное подключение: {ipPort}");

    public void OnClientDisconnected(string ipPort, DisconnectReason reason)
        => Console.WriteLine($"[20020] Отключение (код: {reason})");

    public string ProcessMessage(MyMessage message, string ipPort)
    {
        Console.WriteLine($"[20020] Обработка: {message.Text} | {message.Number}");
        return $"PORT20020_OK: {message.Text.ToUpper()}";
    }

    public void OnError(Exception ex)
        => Console.WriteLine($"[20020] Критическая ошибка: {ex.Message}");
}

public class Port20030Handler : IPortHandler
{
    public void OnClientConnected(string ipPort, Guid guid)
        => Console.WriteLine($"[20030] Специальное подключение: {ipPort}");

    public void OnClientDisconnected(string ipPort, DisconnectReason reason)
        => Console.WriteLine($"[20030] Отключение (код: {reason})");

    public string ProcessMessage(MyMessage message, string ipPort)
    {
        Console.WriteLine($"[20030] Обработка: {message.Text} | {message.Number}");
        return $"PORT20030_OK: {message.Text.ToUpper()}";
    }

    public void OnError(Exception ex)
        => Console.WriteLine($"[20030] Критическая ошибка: {ex.Message}");
}

public class Port20040Handler : IPortHandler
{
    public void OnClientConnected(string ipPort, Guid guid)
        => Console.WriteLine($"[20040] Специальное подключение: {ipPort}");

    public void OnClientDisconnected(string ipPort, DisconnectReason reason)
        => Console.WriteLine($"[20040] Отключение (код: {reason})");

    public string ProcessMessage(MyMessage message, string ipPort)
    {
        Console.WriteLine($"[20040] Обработка: {message.Text} | {message.Number}");
        return $"PORT20040_OK: {message.Text.ToUpper()}";
    }

    public void OnError(Exception ex)
        => Console.WriteLine($"[20040] Критическая ошибка: {ex.Message}");
}

public class LoggingOnlyHandler : IPortHandler
{
    public void OnClientConnected(string ipPort, Guid guid)
        => Console.WriteLine($"[Логирование] Новое подключение: {ipPort}");

    public void OnClientDisconnected(string ipPort, DisconnectReason reason) { }

    public string ProcessMessage(MyMessage message, string ipPort)
    {
        Console.WriteLine($"[ЛОГ] {DateTime.Now:T}: {message.Text}");
        return null; // Не отправляем ответ
    }

    public void OnError(Exception ex) { }
}

public class DefaultPortHandler : IPortHandler
{
    public void OnClientConnected(string ipPort, Guid guid)
    {

    }

    public void OnClientDisconnected(string ipPort, DisconnectReason reason)
    {

    }

    public string ProcessMessage(MyMessage message, string ipPort)
        => $"DEFAULT_ECHO: {message.Text} ({message.Number})";

    public void OnError(Exception ex)
        => Console.WriteLine($"[ОШИБКА]: {ex.Message}");
}



/*

public class Port20010Handler : IPortHandler
   {
       public void OnClientConnected(string ipPort, Guid guid)
           => Console.WriteLine($"[20010] Подключен аналитический клиент: {ipPort}");
   
       public void OnClientDisconnected(string ipPort, DisconnectReason reason) { }
   
       public string ProcessMessage(MyMessage message, string ipPort)
       {
           int result = message.Number * 2;
           return $"ANALYTICS_RESULT: {result}";
       }
   
       public void OnError(Exception ex)
           => Console.WriteLine($"[20010] Ошибка анализа: {ex.Message}");
   }
   public class Port20020Handler : IPortHandler
   {
       public void OnClientConnected(string ipPort, Guid guid)
           => Console.WriteLine($"[20020] Специальное подключение: {ipPort}");
   
       public void OnClientDisconnected(string ipPort, DisconnectReason reason)
           => Console.WriteLine($"[20020] Отключение (код: {reason})");
   
       public string ProcessMessage(MyMessage message, string ipPort)
       {
           Console.WriteLine($"[20020] Обработка: {message.Text} | {message.Number}");
           return $"PORT20020_OK: {message.Text.ToUpper()}";
       }
   
       public void OnError(Exception ex)
           => Console.WriteLine($"[20020] Критическая ошибка: {ex.Message}");
   }
   public class Port20030Handler : IPortHandler
   {
       public void OnClientConnected(string ipPort, Guid guid)
           => Console.WriteLine($"[20030] Подключен аналитический клиент: {ipPort}");
   
       public void OnClientDisconnected(string ipPort, DisconnectReason reason) { }
   
       public string ProcessMessage(MyMessage message, string ipPort)
       {
           int result = message.Number * 4;
           return $"ANALYTICS_RESULT: {result}";
       }
   
       public void OnError(Exception ex)
           => Console.WriteLine($"[20030] Ошибка анализа: {ex.Message}");
   }
   public class Port20040Handler : IPortHandler
   {
       public void OnClientConnected(string ipPort, Guid guid)
           => Console.WriteLine($"[20040] Подключен аналитический клиент: {ipPort}");
   
       public void OnClientDisconnected(string ipPort, DisconnectReason reason) { }
   
       public string ProcessMessage(MyMessage message, string ipPort)
       {
           int result = message.Number * 2*4;
           return $"ANALYTICS_RESULT: {result}";
       }
   
       public void OnError(Exception ex)
           => Console.WriteLine($"[20040] Ошибка анализа: {ex.Message}");
   }
   



!!! РАБОЙ ВАРИАНТ

List<WatsonTcpServer> _servers = new List<WatsonTcpServer>();
CancellationTokenSource _cts = new CancellationTokenSource();

Console.WriteLine("WatsonTcpServer!");

    // Порты для прослушивания
int[] ports = { 20000, 20010, 20020, 20030, 20040 };

// Запуск серверов на каждом порту
foreach (int port in ports)
{
    StartServer(port);
}

Console.WriteLine("Серверы запущены. Нажмите Enter для остановки.");
Console.ReadLine();

// Остановка серверов
_cts.Cancel();
foreach (var server in _servers)
{
    server.Dispose();
}
                                             

void StartServer(int port)
{
    //var settings = new WatsonTcpServerSettings
    //{
    //    MaxConnections =  30,
    //    IdleClientTimeoutSeconds = 300
    //};


    var server = new WatsonTcpServer("127.0.0.1", port);
    _servers.Add(server);
    server.Events.ClientConnected += (sender, e) =>
        Console.WriteLine($"[Port {port}] Клиент подключен: {e.Client.IpPort} (GUID: {e.Client.Guid})");

    server.Events.ClientDisconnected += (sender, e) =>
        Console.WriteLine($"[Port {port}] Клиент отключен: {e.Client.IpPort} (Причина: {e.Reason})");

    server.Events.MessageReceived += async (sender, e) =>
    {
        try
        {
            string msg = System.Text.Encoding.UTF8.GetString(e.Data);
            Console.WriteLine($"[Port {port}] Получено от {e.Client.IpPort}: {msg}");

            // Асинхронный ответ клиенту (новый API)
                await server.SendAsync(
                e.Client.Guid,  // Требует GUID клиента
                $"Echo: {msg}",
                metadata: null,
                token: _cts.Token);
            //  !!!!  разобраться   timeoutMs: 5000) 
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Port {port}] Ошибка обработки сообщения: {ex.Message}");
        }
    };

    server.Events.ExceptionEncountered += (sender, e) =>
        Console.WriteLine($"[Port {port}] Ошибка: {e.Exception}");

    // Запуск сервера в отдельном потоке
    ThreadPool.QueueUserWorkItem(async _ =>
    {
        try
        {
            server.Start();
            Console.WriteLine($"Сервер запущен на порту {port}");

            while (!_cts.IsCancellationRequested)
            {
                await Task.Delay(1000);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка на порту {port}: {ex.Message}");
        }
    });
}

public class myMessage
{
    public string Text { get; set; }
    public int Number { get; set; }
}
*/

/*  ОТЛАДКА
 
 Вот как можно проверить подключение клиента на сервере WatsonTcp в C# с подробной диагностикой:
   
   1. Базовый способ проверки подключения
   csharp
   server.Events.ClientConnected += (sender, e) => 
   {
       Console.WriteLine($"✅ Клиент подключен: {e.Client.IpPort}");
       Console.WriteLine($"   ID подключения: {e.Client.Guid}");
       Console.WriteLine($"   Время: {DateTime.Now:HH:mm:ss}");
       
       // Дополнительная проверка
       if (server.IsClientConnected(e.Client.Guid))
       {
           Console.WriteLine("   Статус: Подтверждено (IsClientConnected = true)");
       }
       else
       {
           Console.WriteLine("   ⚠️ Ошибка: клиент не подтвержден сервером");
       }
   };
   2. Расширенная диагностика (рекомендуется)
   csharp
   // В конфигурации сервера
   var settings = new WatsonTcpServerSettings
   {
       DebugMessages = true, // Включить подробное логирование
       ClientTimeoutSeconds = 30 // Таймаут неактивных клиентов
   };
   
   var server = new WatsonTcpServer("127.0.0.1", 20000, settings);
   
   // Обработчик подключений с полной диагностикой
   server.Events.ClientConnected += (sender, e) =>
   {
       var clientInfo = new {
           IpPort = e.Client.IpPort,
           Guid = e.Client.Guid,
           ServerTime = DateTime.Now,
           ActiveConnections = server.ListClients().Count
       };
       
       Console.WriteLine($"📡 Новое подключение:\n{JsonConvert.SerializeObject(clientInfo, Formatting.Indented)}");
       
       // Проверка через внутренний список клиентов
       var clients = server.ListClients();
       if (clients.Any(c => c.IpPort == e.Client.IpPort))
       {
           Console.WriteLine("🔍 Клиент найден в списке активных подключений");
       }
       else
       {
           Console.WriteLine("❌ Клиент НЕ найден в активных подключениях!");
       }
   };
   3. Метод для ручной проверки подключения
   csharp
   public bool IsClientReallyConnected(string ipPort)
   {
       try
       {
           // Способ 1: Через встроенный метод
           bool isConnected = server.IsClientConnected(ipPort);
           
           // Способ 2: Через список клиентов
           bool inList = server.ListClients().Exists(c => c.IpPort == ipPort);
           
           // Способ 3: Попытка отправки тестового сообщения
           bool canSend = false;
           try
           {
               server.Send(ipPort, "ping");
               canSend = true;
           }
           catch { }
           
           Console.WriteLine($"Проверка подключения {ipPort}:");
           Console.WriteLine($"  IsClientConnected: {isConnected}");
           Console.WriteLine($"  В списке клиентов: {inList}");
           Console.WriteLine($"  Возможность отправки: {canSend}");
           
           return isConnected && inList && canSend;
       }
       catch (Exception ex)
       {
           Console.WriteLine($"Ошибка проверки подключения: {ex.Message}");
           return false;
       }
   }
   4. Периодическая проверка активных клиентов
   csharp
   // Запустить в отдельном потоке
   async Task MonitorConnectionsAsync()
   {
       while (!_cts.IsCancellationRequested)
       {
           Console.WriteLine("\n=== АКТИВНЫЕ ПОДКЛЮЧЕНИЯ ===");
           var clients = server.ListClients();
           
           if (clients.Count == 0)
           {
               Console.WriteLine("Нет активных подключений");
           }
           else
           {
               foreach (var client in clients)
               {
                   Console.WriteLine($"- {client.IpPort} (ID: {client.Guid})");
                   Console.WriteLine($"  Время подключения: {client.ConnectedTime}");
                   Console.WriteLine($"  Активность: {DateTime.Now - client.ConnectedTime} назад");
               }
           }
           
           await Task.Delay(5000); // Проверка каждые 5 секунд
       }
   }
   
   // Запуск мониторинга
   _ = Task.Run(MonitorConnectionsAsync);
   5. Важные замечания:
   Проблемы с определением отключения:
   
   WatsonTcp может задерживать обнаружение отключения (до 30 сек по умолчанию)
   
   Для оперативного определения используйте:
   
   csharp
   settings.ClientTimeoutSeconds = 5; // Более агрессивный таймаут
   Ложные срабатывания:
   
   IsClientConnected может возвращать true даже после отключения
   
   Всегда комбинируйте несколько методов проверки
   
   Для надежности добавьте обработку отключений:
   
   csharp
   server.Events.ClientDisconnected += (sender, e) => 
   {
       Console.WriteLine($"🚫 Клиент отключен: {e.Client.IpPort}");
       Console.WriteLine($"   Причина: {e.Reason}");
       Console.WriteLine($"   Время подключения: {e.Client.ConnectedTime}");
   };
 
 
 
 */