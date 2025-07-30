using Common.Core.Channel;
using Common.Core.Property;
using DMemory.Core;
using DMemory.Core.Channel;         // Ваши MessagePack-Channel-структуры
using DMemory.Core.Converter;       // Конвертеры Channel <-> ChannelBase
using DynamicData;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive.Concurrency;
using DynamicData;
using ReactiveUI;
using MapCommands = System.Collections.Generic.Dictionary<string, string>;

namespace Data.Core;
//public record RamData(object Data, Type DataType, MapCommands MetaData);

public interface ICUDAModule
{
  SourceCache<DataTimeVariable, ulong> Id1Temper { get; set; }
  SourceCache<DataTimeVariable, ulong> Id2Temper { get; set; }
}

public class CUDAModule : ICUDAModule, IDisposable
{
  // SourceCache по каждому ID (id — uint, Tik — ulong)
//  private readonly ConcurrentDictionary<uint, SourceCache<DataTimeVariable, ulong>> _idCaches = new();
  public SourceCache<DataTimeVariable, ulong> Id1Temper { get; set; }
  public SourceCache<DataTimeVariable, ulong> Id2Temper { get; set; }

  private Task _worker;

  // Очередь для приёма от DMemory (приём данных)
  private readonly BlockingCollection<RamData> _inputQueue = new();

  // Очередь для отправки обратно в DMemory
  private readonly BlockingCollection<RamData> _outputQueue = new();

  // Потоки-воркеры (можно заменить на таски)
  private readonly CancellationTokenSource _cts = new();
  private Task _inputWorker;
  private Task _outputWorker;

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

    _ = Id1Temper.Connect()
        //.Transform(transformFactory: x => new SLoggerData(ld: x))
        //.Bind(readOnlyObservableCollection: out _lsError)
        .ObserveOnDispatcher(priority: DispatcherPriority.Normal)
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
              Console.WriteLine($"Новое значение Tik: {formatted}, Variable: {data.Variable}");
            }
          }
        });
    _ = Id2Temper.Connect()
      //      .Transform(transformFactory: x => new SLoggerData(ld: x))
      //      .Bind(readOnlyObservableCollection: out _lsError)
      .ObserveOnDispatcher(priority: DispatcherPriority.Normal)
      .ObserveOn(scheduler: RxApp.MainThreadScheduler)
      .Subscribe(onNext: _ => { });


    _inputWorker = Task.Run(ProcessInputLoop, _cts.Token);
    _outputWorker = Task.Run(ProcessOutputLoop, _cts.Token);
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

  // Цикл обработки входящих RamData
  private void ProcessInputLoop()
  {
    foreach (var ram in _inputQueue.GetConsumingEnumerable(_cts.Token))
    {
      switch (ram.DataType)
      {
        case var t when t == typeof(LoggerChannel):
        {
          // передача в logger 
          break;
        }
        case var t when t == typeof(DtVariableChannel):
          {
            // Можно использовать твой конвертер или логику напрямую
            if (new DtVariableChannelConverter().Convert(ram.Data) is not IDataTimeVariable baseObj) continue;
            Id1Temper.AddOrUpdate(new DataTimeVariable(baseObj));
            break;
          }
        case var t when t == typeof(VDtValuesChannel):
        {
          var baseObj = new VDtValuesChannelConverter().Convert(ram.Data) as DataTimeVariableV;
          if (baseObj == null) continue;
          Id1Temper.AddOrUpdate(baseObj.DataTimeVariable);
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



/*
public class CUDAModule : ICUDAModule, IDisposable
{
  public SourceCache<DataTimeVariable, ulong> Id1Temper { get; set; }
  public SourceCache<DataTimeVariable, ulong> Id2Temper { get; set; }
  
  private readonly BlockingCollection<RamData> _inputQueue = new();
 
  private readonly CancellationTokenSource _cts = new();
  private Task _worker;

  public CUDAModule()
  {
    Id1Temper = new(t => t.Tik);
    Id2Temper = new(t => t.Tik);

    _ = Id1Temper.Connect()
//      .Transform(transformFactory: x => new SLoggerData(ld: x))
//      .Bind(readOnlyObservableCollection: out _lsError)
      .ObserveOnDispatcher(priority: DispatcherPriority.Normal)
      .ObserveOn(scheduler: RxApp.MainThreadScheduler)
      .Subscribe(onNext: _ => { });

    _ = Id2Temper.Connect()
      //      .Transform(transformFactory: x => new SLoggerData(ld: x))
      //      .Bind(readOnlyObservableCollection: out _lsError)
      .ObserveOnDispatcher(priority: DispatcherPriority.Normal)
      .ObserveOn(scheduler: RxApp.MainThreadScheduler)
      .Subscribe(onNext: _ => { });

    _worker = Task.Run(ProcessLoop, _cts.Token);
  }

  // DataTimeVariable, DataTimeVariableV

  // Вход данных из DataContext
  public void EnqueueRaw(RamData data) => _inputQueue.Add(data);

  // Главный обработчик: data → convert → SourceCache

  private void ProcessLoop()
  {
    foreach (var ram in _inputQueue.GetConsumingEnumerable(_cts.Token))
    {

      switch (ram.DataType.Name)
      {
        case nameof(LoggerChannel):
        {
          // передача в logger 
          break;
        }
        case nameof(DtVariableChannel):
        {
          if (new DtVariableChannelConverter().Convert(ram.Data) is not IDataTimeVariable baseObj) continue;
          Id1Temper.AddOrUpdate(new DataTimeVariable(baseObj));
          break;
        }
        case nameof(VDtValuesChannel):
        {
          var baseObj = new VDtValuesChannelConverter().Convert(ram.Data) as DataTimeVariableV;
          if (baseObj == null) continue;
          Id1Temper.AddOrUpdate(baseObj.DataTimeVariable);
          break;
        }
      }
    }
  }

  public void Dispose()
  {
    _cts.Cancel();
    _inputQueue.CompleteAdding();
    try { _worker?.Wait(500); } catch { }
    _cts.Dispose();
  }
}
*/



   


