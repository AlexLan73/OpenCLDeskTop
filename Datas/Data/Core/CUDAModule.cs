#nullable enable
using Common.Core.Channel;
using Common.Core.Property;
using DMemory.Core;
using DMemory.Core.Channel;         // Ваши MessagePack-Channel-структуры
using DMemory.Core.Converter;       // Конвертеры Channel <-> ChannelBase
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using DMemory.Enums;

namespace Data.Core;
//public record RamData(object Data, Type DataType, MapCommands MetaData);

public interface ICudaModule
{
  SourceCache<DataTimeVariable, long> Id1Temper { get; set; }
  SourceCache<DataTimeVariable, long> Id2Temper { get; set; }
}
                                                                          
/// <summary>
/// Модуль для приёма, конвертации и буферизации данных CUDA.
/// </summary>
public class CudaModule : ICudaModule, IDisposable
{
  // SourceCache для id=1 и id=2 температур; видимы всей программе (подписчики могут слушать обновления)
  public SourceCache<DataTimeVariable, long> Id1Temper { get; set; }
  public SourceCache<DataTimeVariable, long> Id2Temper { get; set; }
  public SourceCache<LoggerBase, long> LoggerRx { get; set; }

  // Очередь для приёма RamData из DMemory
  private readonly BlockingCollection<RamData> _inputQueue = new();

  // Очередь для асинхронных отправок RamData в DMemory
  private readonly BlockingCollection<RamData> _outputQueue = new();

  // Токен для управления завершением всех потоков
  private readonly CancellationTokenSource _cts = new();
  private readonly Task _inputWorker;
  private readonly Task _outputWorker;
  private Task _loopWrite;

  //  В DataContext -> CUDAModule -> всегда сервер
  private readonly ServerMetaData _server;

  // Внешний делегат для отправки (обычно ServerMetaData.EnqueueToSendAsync)
  public Func<RamData, Task>? SendToDMemoryAsync { get; set; }

  public CudaModule()
  {
    // Привязка к памяти и запуск приёмника сырого RamData
    MetaSettings mata = new("CUDA");
    var processor = new MemoryDataProcessor(mata.MemoryName, EnqueueRaw);
    _server = new ServerMetaData(mata, processor);

    Id1Temper = new(t => t.Tik);
    Id2Temper = new(t => t.Tik);
    LoggerRx = new(t => t.Tik);

    // Подписка к SourceCache — пример реактивного (UI, логи)
    SubscribeCache(Id1Temper, "ID1");
    SubscribeCache(Id2Temper, "ID2");
    SubscribeLogger(LoggerRx);

    // Запуск тасков на фоне
    _inputWorker = Task.Run(ProcessInputLoop, _cts.Token);
    _outputWorker = Task.Run(ProcessOutputLoop, _cts.Token);
    _loopWrite = Task.Run(LoopWrite, _cts.Token);
  }

  /// <summary>
  /// Внешний приём RamData — кладём в очередь (потоко-безопасно, никогда не теряем данные)
  /// </summary>
  public void EnqueueRaw(RamData data) => _inputQueue.Add(data);

  /// <summary>
  /// Положить данные в очередь на отправку в DMemory (back channel)
  /// </summary>
  public void EnqueueToOutput(RamData data) => _outputQueue.Add(data);

  /// <summary>
  /// Асинхронный наружный вызов отправки (для интеграции с сервером)
  /// </summary>
  public async Task EnqueueToSendAsync(RamData data)
  {
    _outputQueue.Add(data);
    await Task.CompletedTask;
  }

  /// <summary>
  /// Поток обработки всех входящих данных (разбор по типам, конвертация, распределение по SourceCache)
  /// </summary>
  private void ProcessInputLoop()
  {
    foreach (var ram in _inputQueue.GetConsumingEnumerable(_cts.Token))
    {
      switch (ram.DataType)
      {
        case var t when t == typeof(LoggerChannel):
          var logger = new LoggerChannelConverter().Convert(ram.Data) as LoggerBase;
          if (logger != null) LoggerRx.AddOrUpdate(logger);
          break;

        case var t when t == typeof(DtValuesChannel):
          var baseObj = new DtValuesChannelConverter().Convert(ram.Data) as DataTimeVariable;
          if (baseObj != null) Id1Temper.AddOrUpdate(baseObj);
          break;

        case var t when t == typeof(VDtValuesChannel):
          var vObj = new VDtValuesChannelConverter().Convert(ram.Data) as DataTimeVariableV;
          if (vObj != null) Id2Temper.AddOrUpdate(vObj.DataTimeVariable);
          break;

          // Добавляй другие типы данных по мере необходимости
      }
    }
  }

  /// <summary>
  /// Поток для асинхронной передачи данных в DMemory (outbound)
  /// </summary>
  private void ProcessOutputLoop()
  {
    foreach (var ram in _outputQueue.GetConsumingEnumerable(_cts.Token))
    {
      if (SendToDMemoryAsync != null)
      {
        try { SendToDMemoryAsync(ram).Wait(); }
        catch (Exception ex) { Console.WriteLine($"[CUDAModule] Ошибка при отправке RamData: {ex}"); }
      }
      else
      {
        // Fallback: пишем напрямую через _server
        _ = _server.EnqueueToSendAsync(ram);
        Console.WriteLine($"[CUDAModule]  {DateTime.Now:HH:mm:ss,ffff}   Отправка данных RamData!  клиенту");
      }
    }
  }

  /// <summary>
  /// Реактивный вывод для SourceCache чисел
  /// </summary>
  private void SubscribeCache(SourceCache<DataTimeVariable, long> cache, string label)
  {
    _ = cache.Connect()
        .ObserveOn(RxApp.MainThreadScheduler)
        .Subscribe(itemChangeSet =>
        {
          foreach (var change in itemChangeSet)
          {
            if (change.Reason != ChangeReason.Add &&
                change.Reason != ChangeReason.Refresh) continue;
            var data = change.Current;
            var dt = DateTimeOffset.FromUnixTimeMilliseconds(data.Tik);
            Console.WriteLine($"{label} --> Tik: {dt:yyyy.MM.dd HH:mm:ss,fffff}, Variable: {data.Variable}");
          }
        });
  }

  /// <summary>
  /// Реактивный вывод логов
  /// </summary>
  private void SubscribeLogger(SourceCache<LoggerBase, long> cache)
  {
    _ = cache.Connect()
        .ObserveOn(RxApp.MainThreadScheduler)
        .Subscribe(itemChangeSet =>
        {
          foreach (var change in itemChangeSet)
          {
            if (change.Reason != ChangeReason.Add && change.Reason != ChangeReason.Refresh)
              continue;
            var data = change.Current;
            var dt = DateTimeOffset.FromUnixTimeMilliseconds(data.Tik);
            Console.WriteLine($"LOGGER --> Tik: {dt:yyyy.MM.dd HH:mm:ss,fffff}, "
                    + $"Id: {data.Id} , Code: {data.Code}, Log= {data.Log}, Module= {data.Module}");
          }
        });
  }

  /// <summary>
  /// Циклический мониторинг/анализ (пример, можно убрать)
  /// </summary>
  private void LoopWrite()
  {
    while (!_cts.IsCancellationRequested)
    {
      if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape)
      {
        Console.WriteLine("Выход из цикла по ESC.");
        break;
      }
//      if(_server.GetSateMode() != SateMode.Work || _server.GeTransferWaiting() == TransferWaiting.None)
          Console.WriteLine($"Tick: {DateTime.Now:HH:mm:ss} [DcServer] -> {_server.GetSateMode()} [ПЕРЕДАЧА] {_server.GeTransferWaiting()} ");
      Thread.Sleep(500);
    }
  }

  /// <summary>
  /// Корректное завершение всех воркеров и ресурсов
  /// </summary>
  public void Dispose()
  {
    _cts.Cancel();
    _inputQueue.CompleteAdding();
    _outputQueue.CompleteAdding();
    try { _inputWorker.Wait(1000); }
    catch
    {
      // ignored
    }

    try { _outputWorker.Wait(1000); }
    catch
    {
      // ignored
    }

    try { _loopWrite.Wait(1000); }
    catch
    {
      // ignored
    }

    _cts.Dispose();
  }
}



/*
public class CUDAModule : ICUDAModule, IDisposable
{
  // SourceCache по каждому ID (id — uint, Tik — ulong)
//  private readonly ConcurrentDictionary<uint, SourceCache<DataTimeVariable, ulong>> _idCaches = new();
  public SourceCache<DataTimeVariable, long> Id1Temper { get; set; }
  public SourceCache<DataTimeVariable, long> Id2Temper { get; set; }
  public SourceCache<LoggerBase, long> LoggerRx { get; set; }


  private Task _worker;

  // Очередь для приёма от DMemory (приём данных)
  private readonly BlockingCollection<RamData> _inputQueue = new();

  // Очередь для отправки обратно в DMemory
  private readonly BlockingCollection<RamData> _outputQueue = new();

  // Потоки-воркеры (можно заменить на таски)
  private readonly CancellationTokenSource _cts = new();
  private Task _inputWorker;
  private Task _outputWorker;
  private Task _loopWrite;
  private readonly ServerMetaData _server;

  // Внешний хэндлер отправки (например, ServerMetaData.EnqueueToSendAsync)
  public Func<RamData, Task>? SendToDMemoryAsync { get; set; }

  public CUDAModule()
  {
    // Инициализация класса ServerMetaData для работы с памятью
    MetaSettings mata = new("CUDA");
    var processor = new MemoryDataProcessor(mata.MemoryName, EnqueueRaw);  //  получаем не конвертированные данные из памяти;
    _server = new ServerMetaData(mata, processor);

    Id1Temper = new(t => t.Tik);
    Id2Temper = new(t => t.Tik);
    LoggerRx = new(t => t.Tik);

    _ = LoggerRx.Connect()
      //.Transform(transformFactory: x => new SLoggerData(ld: x))
      //.Bind(readOnlyObservableCollection: out _lsError)
      //        .ObserveOnDispatcher(priority: DispatcherPriority.Normal)
      .ObserveOn(RxApp.MainThreadScheduler)
      .Subscribe(itemChangeSet =>
      {
        // При каждом обновлении получаем ChangeSet<DataTimeVariable, ulong>
        foreach (var change in itemChangeSet)
        {
          if (change.Reason != DynamicData.ChangeReason.Add &&
              change.Reason != DynamicData.ChangeReason.Refresh) continue;
          var data = change.Current;
          var dateTime = DateTimeOffset.FromUnixTimeMilliseconds((long)data.Tik);
          // Форматирование: "yyyy.MM.dd HH:mm:ss,fffff"
          var formatted = dateTime.ToString("yyyy.MM.dd HH:mm:ss,fffff");
          Console.WriteLine($"  LOGGER -->  Tik: {formatted}, Id: {data.Id} , Code: {(LoggerSendEnumMemory)data.Code}, " +
                            $"Log= {data.Log},  Module= {data.Module}  ");
        }
      });

    _ = Id1Temper.Connect()
        //.Transform(transformFactory: x => new SLoggerData(ld: x))
        //.Bind(readOnlyObservableCollection: out _lsError)
//        .ObserveOnDispatcher(priority: DispatcherPriority.Normal)
        .ObserveOn(RxApp.MainThreadScheduler)
        .Subscribe(itemChangeSet =>
        {
          // При каждом обновлении получаем ChangeSet<DataTimeVariable, ulong>
          foreach (var change in itemChangeSet)
          {
            if (change.Reason == DynamicData.ChangeReason.Add || change.Reason == DynamicData.ChangeReason.Refresh)
            {
              var data = change.Current;
              DateTimeOffset dateTime = DateTimeOffset.FromUnixTimeMilliseconds((long)data.Tik);
              // Форматирование: "yyyy.MM.dd HH:mm:ss,fffff"
              string formatted = dateTime.ToString("yyyy.MM.dd HH:mm:ss,fffff");
              Console.WriteLine($"  ID1 --> Tik: {formatted}, Variable: {data.Variable}");
            }
          }
        });
    _ = Id2Temper.Connect()
      //      .Transform(transformFactory: x => new SLoggerData(ld: x))
      //      .Bind(readOnlyObservableCollection: out _lsError)
//      .ObserveOnDispatcher(priority: DispatcherPriority.Normal)
      .ObserveOn(scheduler: RxApp.MainThreadScheduler)
      .Subscribe(onNext: itemChangeSet =>
      {
        // При каждом обновлении получаем ChangeSet<DataTimeVariable, ulong>
        foreach (var change in itemChangeSet)
        {
          if (change.Reason == DynamicData.ChangeReason.Add || change.Reason == DynamicData.ChangeReason.Refresh)
          {
            var data = change.Current;
            DateTimeOffset dateTime = DateTimeOffset.FromUnixTimeMilliseconds((long)data.Tik);
            // Форматирование: "yyyy.MM.dd HH:mm:ss,fffff"
            string formatted = dateTime.ToString("yyyy.MM.dd HH:mm:ss,fffff");
            Console.WriteLine($"  ID2 --> Tik: {formatted}, Variable: {data.Variable}");
          }
        }
      });


    _inputWorker = Task.Run(ProcessInputLoop, _cts.Token);
    _outputWorker = Task.Run(ProcessOutputLoop, _cts.Token);
    _loopWrite = Task.Run(loopWrite, _cts.Token);
  }


  // Вход данных от DMemory
  public void EnqueueRaw(RamData data)
  {
    _inputQueue.Add(data);
  }

  // Очередь на отправку в DMemory
  public void EnqueueToOutput(RamData data)
  {
    _outputQueue.Add(data);
  }

  // Асинхронная функция для внешней части — можно вызывать для инициативной отправки в DMemory
  public async Task EnqueueToSendAsync(RamData data)
  {
    // Просто кладём в выходную очередь — actual send делается worker'ом
    _outputQueue.Add(data);
    await Task.CompletedTask;
  }

  private void loopWrite()
  {
    while (true)
    {
      // Проверяем: нажата ли клавиша?
      if (Console.KeyAvailable)
      {
        var key = Console.ReadKey(intercept: true);
        if (key.Key == ConsoleKey.Escape)
        {
          Console.WriteLine("Выход из цикла по ESC.");
          break;
        }
      }
      Console.WriteLine($"Tick: {DateTime.Now:HH:mm:ss} [DcServer] -> {_server.GetSateMode()}   [ПЕРЕДАЧА] {_server.GeTransferWaiting()} ");

      Thread.Sleep(500);
    }

  }

  // Цикл обработки входящих RamData
  private void ProcessInputLoop()
  {
    foreach (var ram in _inputQueue.GetConsumingEnumerable(_cts.Token))
    {
      switch (ram.DataType)
      {
        case var t when t == typeof(LoggerChannel):
        {
          var _logger = new LoggerChannelConverter().Convert(ram.Data) as LoggerBase;
          if (_logger ==null) continue;
          //            Id1Temper.AddOrUpdate(new DataTimeVariable(baseObj));
          LoggerRx.AddOrUpdate(_logger);
            break;
        }
        case var t when t == typeof(DtValuesChannel):
          {
            // Можно использовать твой конвертер или логику напрямую
            var _ddd = new DtValuesChannelConverter().Convert(ram.Data) as DataTimeVariable;
            if (new DtValuesChannelConverter().Convert(ram.Data) is not DataTimeVariable baseObj) continue;
//            Id1Temper.AddOrUpdate(new DataTimeVariable(baseObj));
            Id1Temper.AddOrUpdate(_ddd);
            break;
          }
        case var t when t == typeof(VDtValuesChannel):
        {
          var baseObj = new VDtValuesChannelConverter().Convert(ram.Data) as DataTimeVariableV;
          if (baseObj == null) continue;
          Id2Temper.AddOrUpdate(baseObj.DataTimeVariable);
          break;
        }

      // Другие кейсы аналогично (LoggerChannel, VDtValuesChannel и т.д.)
      default:
          break;
      }
    }
  }

  // Цикл обработки отправок (output)
  private void ProcessOutputLoop()
  {
    foreach (var ram in _outputQueue.GetConsumingEnumerable(_cts.Token))
    {
      // Здесь вызываем делегат (установит DATA.dll), который реализует отправку в DMemory
      // Обычно это ServerMetaData.EnqueueToSendAsync
      if (SendToDMemoryAsync != null)
      {
        try
        {
          SendToDMemoryAsync(ram).Wait(); // Или .GetAwaiter().GetResult()
        }
        catch (Exception ex)
        {
          Console.WriteLine($"[CUDAModule] Ошибка при отправке RamData: {ex}");
        }
      }
      else
      {
        _server.EnqueueToSendAsync(ram);
        Console.WriteLine("[CUDAModule] Не назначен обработчик для отправки RamData!");
      }
    }
  }

  // Корректное завершение
  public void Dispose()
  {
    _cts.Cancel();
    _inputQueue.CompleteAdding();
    _outputQueue.CompleteAdding();
    try { _inputWorker?.Wait(1000); } catch { }
    try { _outputWorker?.Wait(1000); } catch { }
    _cts.Dispose();
  }
}
*/








