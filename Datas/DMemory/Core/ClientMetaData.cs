using Common.Event;
using DMemory.Enum;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Core.Channel;

/*
 
 public class ClientMetaData
   {
       private readonly MemoryDataProcessor _processor;
       private readonly ConcurrentQueue<RamData> _txQueue = new();
   
       public ClientMetaData(MemoryDataProcessor processor)
       {
           _processor = processor;
           _processor.MetaReady += OnMetaReady;
       }
   
       public void EnqueueToSend(RamData data)
       {
           _txQueue.Enqueue(data);
           TrySendNext();
       }
   
       private void TrySendNext()
       {
           if (_txQueue.TryPeek(out var data))
           {
               _processor.SerializeAndPrepare(data);
               // Дальше не ждём — реакция будет по событию!
           }
       }
   
       private void OnMetaReady(object sender, MapCommands meta)
       {
           // Здесь можем добавить управляющие ключи к meta
           // Далее — отправить meta на сервер
   
           // Получили "разрешение на запись" (по логике канала):
           if (_processor.CommitWrite())
           {
               // После подтверждения сервера — FinishSend()
               // Удалить данные из очереди, повторить TrySendNext, если есть
           }
       }
   }
   
 */
namespace DMemory.Core;
using MapCommands = Dictionary<string, string>;

public class ClientMetaData : IDisposable
{
  private MapCommands? _metadataSend = null;
  private readonly MemoryDataProcessor _processor;
  private readonly ConcurrentQueue<RamData> _txQueue = new();

  public BasicMemoryMd Md;
  public EventWaitHandle sendToServer;
  //  private readonly CancellationTokenSource _cts;
  public readonly Task WaiteEvent;

  private readonly string _nameModule;
  private readonly string _clientName;
  public TransferWaiting _transferWaiting;// Используется только для подтверждения ответа в режиме Work
  private MemoryDataProcessor _memDP;
  public SateMode _mode;
  private ConcurrentQueue<RamData> _dataQueue = new ConcurrentQueue<RamData>();
  #region ===_ Time _===
  private readonly ServerMetaDataTimer _timer = new ServerMetaDataTimer();
  #endregion

  private readonly CancellationTokenSource _cts = new();
  private readonly Task _workerTask;

  public ClientMetaData(MetaSettings meta, MemoryDataProcessor processor)
  {
    _nameModule = "client" + meta.MemoryName;
    _clientName = "server" + meta.MemoryName;

    _processor = processor;
    _processor.MetaReady += OnMetaReady;
    _cts = new CancellationTokenSource();

    sendToServer = new EventWaitHandle(false, EventResetMode.AutoReset, meta.MetaEventServer);

    Md = new BasicMemoryMd(
        meta.MetaEventClient,
        meta.MetaSize,
        meta.ControlName,
        CallBackMetaData,
        sendToServer
    );

    _mode = SateMode.Initialization;
//    _memDP = new MemoryDataProcessor(meta.MemoryName);

    _transferWaiting = TransferWaiting.None;
    // Старт фона (будет использоваться при добавлении таймов)

    SystemPulseTimer.On250MilSec += () =>
    { /* действия каждые 0.25 сек */
      if (_mode == SateMode.Work)
      {
        _timer._timeWork = _timer.IncWork();
      }
      else
        _timer.ResetWork();
    };

    SystemPulseTimer.On250MilSec += Comparison250MilSec;

    SystemPulseTimer.On1Second += () =>
    {
      if (_mode == SateMode.Initialization)
        _timer._timeInitialization = _timer.IncInitialization();
      else
        _timer.ResetInitialization();
    };
    SystemPulseTimer.On1Second += Comparison1SecTimer;

    SystemPulseTimer.On5Seconds += () =>
    {
      _timer._timeGeneralWork = _timer.IncGeneralWork();
    };

    SystemPulseTimer.Start();

    Thread.Sleep(200);
    var initAck = new MapCommands
    {
      [MdCommand.State.AsKey()] = _nameModule,
    };
    Md.WriteMetaMap(initAck);

    _timer.ResetAll();

//    WaiteEvent = Task.CompletedTask;
//    _workerTask = Task.Run(() => ProcessQueueAsync(_cts.Token));

  }
  public void EnqueueToSend(RamData data)
  {
    _txQueue.Enqueue(data);
    TrySendNext();
  }

  private void TrySendNext()
  {
    if (_txQueue.TryPeek(out var data))
    {
      _metadataSend = null;
      _processor.SerializeAndPrepare(data);
      // Дальше не ждём — реакция будет по событию!
    }
  }

  private void OnMetaReady(object sender, MapCommands e)
  {
    _metadataSend = e;

    if (_mode != SateMode.Work || _transferWaiting != TransferWaiting.Transfer)
      return;

    _metadataSend.Add(MdCommand.State.AsKey(), _nameModule);
    _metadataSend.Add(MdCommand.Data.AsKey(), "");
    _transferWaiting = TransferWaiting.Waiting;
    // Получили "разрешение на запись" (по логике канала):
    _processor.CommitWrite();
    Md.WriteMetaMap(_metadataSend);
  }

  private void CallBackMetaData(MapCommands map)
  {
    if (map == null || map.Count == 0)
      return;

    if (!map.TryGetValue(MdCommand.State.AsKey(), out var stateValue))
      return;

    if (stateValue == _nameModule)
      return;

    Console.WriteLine($"[Client] Получено от {stateValue}:");

    foreach (var kv in map)
      Console.WriteLine($" - {kv.Key} = {kv.Value}");
    
    _timer.ResetGeneralWork();
    
    switch (_mode)
    {
      case SateMode.Initialization:
      {
        if (map.TryGetValue(MdCommand.Command.AsKey(), out var cmdVal))
        {
          if (cmdVal == MdCommand.Ok.AsKey())
          {
            _mode = SateMode.Work;
            _mode = SateMode.Work;
            _transferWaiting = TransferWaiting.Transfer;
            _timer.ResetInitialization();
            Console.WriteLine(">>> Handshake подтверждён, переход в Work");
            return;
          }
          else if (cmdVal == "_")
          {
            // Отвечаем ok
            var reply = new MapCommands
            {
              [MdCommand.State.AsKey()] = _nameModule,
              [MdCommand.Command.AsKey()] = MdCommand.Ok.AsKey()
            };
            Console.WriteLine(">>> Client Отправили ok для завершения handhsake");
            _mode = SateMode.Work;
            _timer.ResetInitialization();
            _transferWaiting = TransferWaiting.Transfer;
            Md.WriteMetaMap(reply);
            return;
          }
        }

        // Если команд нет — шлём пустое подтверждение
        var initAck = new MapCommands
        {
          [MdCommand.State.AsKey()] = _nameModule,
          [MdCommand.Command.AsKey()] = "_"
        };
        Md.WriteMetaMap(initAck);
        Console.WriteLine(">>> Отправили пустой command  client -> server");
        break;
      }

      case SateMode.Work:
        {
          // Здесь будет основная логика работы: приём данных, реакции, управление
          // 👇 Пока ничего не шлём, ждём команды подтверждения
          //  Когда будут посылаться данные ставится TransferWaiting.Waiting !!
          // Здесь будет основная логика работы: приём данных, реакции, управление
          Console.WriteLine(">>> Работаем: получили данные в режиме CLIENT Work");
          // 👇 Пока ничего не шлём, ждём команды подтверждения

          if (map.Count < 2) return;
          _timer.ResetWork();
          _timer.ResetWorkSendCount();

          if (map.TryGetValue(MdCommand.Command.AsKey(), out var cmdVal))
          {
            if (cmdVal == MdCommand.Ok.AsKey())
            {
              //              _mode = SateMode.Work;
              _transferWaiting = TransferWaiting.Transfer; // подтверждение, что данные были приняты
              Console.WriteLine(">>> [CLIENT]  Work подтверждение полученных данных ");
              return;
            }
          }
          else
          {
            var searchTerms = new List<string> { MdCommand.State.AsKey(), "id" };
            var matchedKeys = map.Keys.ToList()
              .Where(key => searchTerms.Any(term => key.Contains(term, StringComparison.OrdinalIgnoreCase)))
              .ToList();
            if (matchedKeys.Count == 0)
              return;

            //if()
            foreach (var kv in matchedKeys)
              Console.WriteLine($" - внешний уровень [client] !!!!  в SERVER  == >  {kv} = {map[kv]}");
            //  обработка данных
            map.Clear();
            map.Add(MdCommand.State.AsKey(), _nameModule);
            map.Add(MdCommand.Command.AsKey(), MdCommand.Ok.AsKey());
            _transferWaiting = TransferWaiting.Transfer;
            Md.WriteMetaMap(map);

          }

          break;
        }
      case SateMode.Dispose:
        Console.WriteLine(">>> Завершаем работу");
        break;

      case SateMode.None:
      default:
        throw new ArgumentOutOfRangeException(nameof(_mode), _mode, null);
    }
  }
  #region ===-- Comparison1SecTimer ---
  private void Comparison250MilSec()
  {
    if (_mode == SateMode.Work && _timer.GetWork() > _timer._CompelWork)
    {
      _timer.ResetWork();
      var initAck = new MapCommands
      {
        [MdCommand.State.AsKey()] = _nameModule,
        [MdCommand.Command.AsKey()] = "_"
      };
      Md.WriteMetaMap(initAck);
      _timer._workSendCount = _timer.IncWorkSendCount();
    }
  }
  private void Comparison1SecTimer()
  {

    switch (_mode)
    {
      case SateMode.Work when _timer.GetInitialization() > _timer._CompeGeneralWork:
      {
        // время вышло связи нет переходим на начальный уровень
        _mode = SateMode.Initialization;
        _timer.ResetWork();
        _timer.ResetInitialization();
        _transferWaiting = TransferWaiting.None;

        var initAck = new MapCommands
        {
          [MdCommand.State.AsKey()] = _nameModule,
          [MdCommand.Command.AsKey()] = "_"
        };
        Md.WriteMetaMap(initAck);
        return;
      }
      case SateMode.Initialization when (_timer.GetInitialization() % 5 == 1):
      {
        // время вышло связи нет переходим на начальный уровень
        //      _mode = SateMode.Initialization;
        _transferWaiting = TransferWaiting.None;

        _timer.ResetWork();
        //      ResetInitialization();
        var initAck = new MapCommands
        {
          [MdCommand.State.AsKey()] = _nameModule,
          [MdCommand.Command.AsKey()] = "_"
        };
        Md.WriteMetaMap(initAck);
        return;
      }
      case SateMode.Work when _timer.GetWorkSendCount() > _timer._CompelWorkSendCount:
      {
        _mode = SateMode.Initialization;
        _timer.ResetWork();
        _timer.ResetInitialization();
        _timer.ResetWorkSendCount();
        var initAck = new MapCommands
        {
          [MdCommand.State.AsKey()] = _nameModule,
          [MdCommand.Command.AsKey()] = "_"
        };
        Md.WriteMetaMap(initAck);
        _timer._workSendCount = _timer.IncWorkSendCount();
        break;
      }
      case SateMode.None:
      case SateMode.Dispose:
        break;
      default:
        throw new ArgumentOutOfRangeException();
    }
  }
  #endregion


  public void Dispose()
  {
    Console.WriteLine($"ServerMetaData -- Dispose()");
    _cts.Cancel();
    Md?.Dispose();
    sendToServer?.Dispose();
  }

  public void WriteMetaMap(MapCommands map1)
  {
    Md.WriteMetaMap(map1);
    _transferWaiting = TransferWaiting.Transfer;
  }
}

/*
  private async Task ProcessQueueAsync(CancellationToken ct)
  {
    while (!ct.IsCancellationRequested)
    {
      if (_dataQueue.TryDequeue(out var ramData))
      {
        try
        {
          await SerializeAndWriteAsync(ramData);
        }
        catch (Exception ex)
        {
          Console.WriteLine($"Ошибка в обработке данных: {ex}");
        }
      }
      else
      {
        // Очередь пуста — подождать 1 секунду
        await Task.Delay(1000, ct);
      }
    }
  }
  */
//private void MetaDataCallback(MapCommands map)
//{
//  // получаем MD для пересылки дынных 
//  // если канал свободен посылаем

//}
