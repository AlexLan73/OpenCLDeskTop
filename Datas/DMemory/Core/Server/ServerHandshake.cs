
using DynamicData;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Finance.Implementations;

namespace DMemory.Core.Server;

public class ServerHandshake
{
  public string NameModule { get; }
  private readonly MemoryNome _memoryNome;
  private readonly string moduleCode;
  private readonly string clientKey;
  private readonly string serverKey;

  public ServerHandshake(string nameModule)
  {
    // "Cuda" — с любым твоим названием памяти/проекта
    NameModule = nameModule;
    moduleCode = NameModule;
    _memoryNome = new MemoryNome(NameModule, ServerClient.Server);
    clientKey = "client" + moduleCode; // "clientCUDA"
    serverKey = "server" + moduleCode; // "serverCUDA"
  }

  public void Init()
  {
    Console.WriteLine("[СЕРВЕР] Анализируем MD: ожидаем или устанавливаем handshake.");

    var md = _memoryNome.ReadCommandControlWrite();

    if (md.TryGetValue("state", out string value))
    {
      // MD уже существует! Проверяем что ждал клиент
      if (value == clientKey)
      {
        Console.WriteLine($"[СЕРВЕР] Обнаружен клиент: {clientKey}");
        // Важно! Отвечаем "ok" в MD
        md["command"] = "ok";
        _memoryNome.CommandControlWrite(md);
        Console.WriteLine("[СЕРВЕР] Отправлен ответ 'ok', ожидаем данных от клиента...");
        // дальше: переход к приёму данных
      }
      else
      {
        Console.WriteLine($"[СЕРВЕР] В MD уже есть state, но это не клиент: {value}");
        // При желании: здесь можно перезаписать или инициировать reset
      }
    }
    else
    {
      // MD не существует или пусто — записываем, что мы сервер
      var state = new Dictionary<string, string>
      {
        ["state"] = serverKey
      };
      _memoryNome.CommandControlWrite(state);
      Console.WriteLine($"[СЕРВЕР] В MD записано: {serverKey}. Ждём handshake от клиента...");

      // Читаем обратно
      var read = _memoryNome.ReadCommandControlWrite();
      Console.WriteLine("Прочитано из MD: " + string.Join(";", read.Select(kv => $"{kv.Key}={kv.Value}")));
      Console.WriteLine("Ожидаем информации от C++");
      Console.ReadLine();

      var read1 = _memoryNome.ReadCommandControlWrite();
      Console.WriteLine("Прочитано из MD: " + string.Join(";", read1.Select(kv => $"{kv.Key}={kv.Value}")));

      int kk11 = 1;
      Console.ReadLine();

      //// Проверяем конкретное поле
      //if (_memoryNome.TryGetCommand("command", out var v))
      //  Console.WriteLine("Обнаружена команда: " + v);

      //// Очищаем
      //_memoryNome.ClearCommandControlWrite();
      //var afterClear = _memoryNome.ReadCommandControlWrite();
      //Console.WriteLine("После очистки MD: " + (afterClear.Any() ? "не пусто" : "ПУСТО"));

      //_memoryNome.Dispose();





      int kk = 1;
      // здесь: можно реализовать "ждать появления команды/ответа" в loop
    }
  }

  public void WaitForClientOk()
  {
    while (true)
    {
      var md = _memoryNome.ReadCommandControlWrite();
      if (md.TryGetValue("command", out string value) && value == "ok")
      {
        Console.WriteLine("[СЕРВЕР] Получен 'ok' от клиента. Обмен разрешён!");
        break;
      }
      Thread.Sleep(100); // маленькая пауза для опроса  
    }
  }
}


