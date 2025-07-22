using Common.Event;
using DMemory.Enum;

namespace DMemory.Core;
using MapCommands = Dictionary<string, string>;


public class ServerMetaData : IDisposable
{
  public BasicMemoryMd Md;
  public EventWaitHandle SendToClient;

  private readonly CancellationTokenSource _cts;
  public readonly Task WaiteEvent;

  private readonly string _nameModule;
  private readonly string _clientName;

  public SateMode _mode;
  public TransferWaiting _transferWaiting;// Используется только для подтверждения ответа в режиме Work

  #region ===_ Time _===
  private int _oneSecCounter = 0;
  private int _fiveSecCounter = 0;
  private int _missedWorkAcks = 0; // сколько раз не получили ack
  private int _workOkExpecting = 0;
  private int _initTimer = 0;
  private int _timeGeneralWork = 0;
  private int _timeWork = 0;
  private int _timeInitialization = 0;

  private const int _CompeGeneralWork = 1*12*5; // раз в 5 сек *12 минута * 5=> 5мин
  private const int _CompelWork = (int) 6;      // 6 раза с интервалом 0.25
  private const int _CompeInitialization = 6;   //  каждые 5 сек запрос на контроль.

  #endregion



  public ServerMetaData(MetaSettings meta)
  {
    _nameModule = "server" + meta.MemoryName;
    _clientName = "client" + meta.MemoryName;

    _cts = new CancellationTokenSource();

    SendToClient = new EventWaitHandle(false, EventResetMode.AutoReset, meta.MetaEventClient);

    Md = new BasicMemoryMd(
        meta.MetaEventServer,
        meta.MetaSize,
        meta.ControlName,
        CallBackMetaData,
        SendToClient
    );
    
    _mode = SateMode.Initialization;
    var initAck = new MapCommands
    {
      [MdCommand.State.AsKey()] = _nameModule,
    };
    Md.WriteMetaMap(initAck);

    _transferWaiting = TransferWaiting.Transfer;
    
    ResetAllTimer();
    SystemPulseTimer.On250MilSec += () =>
    { /* действия каждые 0.25 сек */
      _timeWork = IncWork();
    };

    SystemPulseTimer.On250MilSec += () =>
    { /* действия каждые 0.25 сек */
      _timeWork = IncWork();
    };
    SystemPulseTimer.On250MilSec += Comparison250MilSec; 

    SystemPulseTimer.On1Second += () =>
    {
      _timeInitialization = IncInitialization();
    };
    SystemPulseTimer.On1Second += Comparison1SecTimer;

    SystemPulseTimer.On5Seconds += () =>
    {
      _timeGeneralWork = IncGeneralWork();
    };

    SystemPulseTimer.Start();
    // Старт фона (будет использоваться при добавлении таймов)
    WaiteEvent = Task.CompletedTask;
  }


  private void CallBackMetaData(MapCommands map)
  {
    if (map == null || map.Count == 0)
      return;

    if (!map.TryGetValue(MdCommand.State.AsKey(), out var stateValue))
      return;

    if (stateValue == _nameModule)
      return;

    Console.WriteLine($"[Server] Получено от {stateValue}:");

    foreach (var kv in map)
      Console.WriteLine($" - {kv.Key} = {kv.Value}");

    switch (_mode)
    {
      case SateMode.Initialization:
      {
        if (map.TryGetValue(MdCommand.Command.AsKey(), out var cmdVal))
        {
          ResetWorkProtocol();
          if (cmdVal == MdCommand.Ok.AsKey())
          {
            _mode = SateMode.Work;
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
            Md.WriteMetaMap(reply);
            Console.WriteLine(">>> Server Отправили ok для завершения handhsake");
            _mode = SateMode.Work;
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
        Console.WriteLine(">>> Отправили пустой command server → client");
        break;
    }

    case SateMode.Work:
      {
          //  Когда будут посылаться данные ставится TransferWaiting.Waiting !!
          // Здесь будет основная логика работы: приём данных, реакции, управление
          Console.WriteLine(">>> Работаем: получили данные в режиме Work");
          // 👇 Пока ничего не шлём, ждём команды подтверждения

          if (map.TryGetValue(MdCommand.Command.AsKey(), out var cmdVal))
            if (cmdVal == MdCommand.Ok.AsKey())
            {
//              _mode = SateMode.Work;
              _transferWaiting = TransferWaiting.Transfer;   // подтверждение, что данные были приняты
              Console.WriteLine(">>> Handshake подтверждён, переход в Work");
              return;
            }

      }


    break;

      case SateMode.Dispose:
        Console.WriteLine(">>> Завершаем работу");
        break;

      case SateMode.None:
      default:
        throw new ArgumentOutOfRangeException(nameof(_mode), _mode, null);
    }
  }

  public void Dispose()
  {
    Console.WriteLine($"ServerMetaData -- Dispose()");
    _cts.Cancel();
    Md?.Dispose();
    SendToClient?.Dispose();
  }

  #region ===-- Comparison1SecTimer ---
  private void Comparison250MilSec()
  {
    //private const int _CompeGeneralWork = 1 * 12 * 5; // раз в 5 сек *12 минута * 5=> 5мин
    //private const int _CompelWork = (int)6;      // 6 раза с интервалом 0.25
    //private const int _CompeInitialization = 6; 

  }
  private void Comparison1SecTimer()
  {
/*
    if (SateMode.Work != _mode)
      return;

    if (_initTimer % 3 == 0)
    {
      if (!_controlMap.TryAdd(Work, ""))
      {
        // если счетчик не сбрасывается мы должны послать каждые примерено 1.5 сек
        _controlMap = new()
          {
            [State] = _nameRole,
            [Work] = "",
            [$"-- просрочка по времени {_nameRole}   !!!-- Test "] = "!-!-!-!",
          }
          ;
        WriteInMemoryMd(_controlMap);
        //    ResetInit();
      }
    }

    if (_initTimer < 20) return;

    //    SateMode = SateMode.Initialization;
    //    ResetInit();
*/
  }
  #endregion

  #region ===-- Work Timer --===
  // Общий сброс всех таймеров и счётчиков
  public void ResetAllTimer()
  {
    Interlocked.Exchange(ref _oneSecCounter, 0);
    Interlocked.Exchange(ref _fiveSecCounter, 0);
    Interlocked.Exchange(ref _missedWorkAcks, 0);
    Interlocked.Exchange(ref _workOkExpecting, 0);
    Interlocked.Exchange(ref _initTimer, 0);

    Interlocked.Exchange(ref _timeGeneralWork, 0);
    Interlocked.Exchange(ref _timeWork, 0);
    Interlocked.Exchange(ref _timeInitialization, 0);
  }

  void ResetWorkProtocol()
  {
    ResetGeneralWork();
    ResetWork();
    ResetInitialization();

  }

  // Все методы увеличения/сброса - так же через Interlocked
  public int IncOneSec() => Interlocked.Increment(ref _oneSecCounter);
  public int IncFiveSec() => Interlocked.Increment(ref _fiveSecCounter);

  public int IncGeneralWork() => Interlocked.Increment(ref _timeGeneralWork);
  public void ResetGeneralWork() => Interlocked.Exchange(ref _timeGeneralWork, 0);
  public int GetGeneralWork() => Interlocked.CompareExchange(ref _timeGeneralWork, 0, 0);

  public int IncWork() => Interlocked.Increment(ref _timeWork);
  public void ResetWork() => Interlocked.Exchange(ref _timeWork, 0);
  public int GetWork() => Interlocked.CompareExchange(ref _timeWork, 0, 0);
  
  public int IncInitialization() => Interlocked.Increment(ref _timeInitialization);
  public void ResetInitialization() => Interlocked.Exchange(ref _timeInitialization, 0);
  public int GetInitialization() => Interlocked.CompareExchange(ref _timeInitialization, 0, 0);


  public void ResetOneSec() => Interlocked.Exchange(ref _oneSecCounter, 0);
  public void ResetTenSec() => Interlocked.Exchange(ref _fiveSecCounter, 0);
  public int GetOneSec() => Interlocked.CompareExchange(ref _oneSecCounter, 0, 0);
  public int GetTenSec() => Interlocked.CompareExchange(ref _fiveSecCounter, 0, 0);
  // Счетчик "work-ответов"
  public int IncWorkAckMissed() => Interlocked.Increment(ref _missedWorkAcks);
  public void ResetWorkAckMissed() => Interlocked.Exchange(ref _missedWorkAcks, 0);
  public int GetWorkAckMissed() => Interlocked.CompareExchange(ref _missedWorkAcks, 0, 0);
  // Для перехода в Initialization если таймаут
  public int IncInit() => Interlocked.Increment(ref _initTimer);
  public void ResetInit() => Interlocked.Exchange(ref _initTimer, 0);
  public int GetInit() => Interlocked.CompareExchange(ref _initTimer, 0, 0);

  public SateMode GetSateMode() => _mode;
  public void SetSateMode(SateMode sm) => _mode = sm;
  #endregion

}




//public class ServerMetaData
//{
//  private readonly MetaSettings _meta;
//  public BasicMemoryMd Md;
//  public EventWaitHandle SendToClient;
//  private readonly CancellationTokenSource _cts;                 // завершение потока
//  public readonly Task WaiteEvent;
//  private Action<MapCommands> _callBack;            // Инициализация пустым делегатом передать данные на верх
//  private readonly string _nameModule;
//  private readonly string _clientName;

//  private readonly ConcurrentQueue<MapCommands> _queue = new();
//  private readonly ManualResetEventSlim _signal = new(false);
//  private SateMode _mode;
//  public ServerMetaData(MetaSettings meta)
//  {
//    _meta = meta;
//    _nameModule = "server" + _meta.MemoryName;
//    _clientName = "client" + _meta.MemoryName;
//    _cts = new CancellationTokenSource();
//    var token = _cts.Token;
//    SendToClient = new EventWaitHandle(false, EventResetMode.AutoReset, _meta.MetaEventClient);
//    Md = new BasicMemoryMd(_meta.MetaEventServer, _meta.MetaSize, _meta.ControlName, CallBackMetaData, SendToClient);
//    //    WaiteEvent = ReadDataCallBack(token);
//    _mode = SateMode.Initialization;

//    WaiteEvent = ReadDataCallBack(_cts.Token);

//  }
//  private async Task ReadDataCallBack(CancellationToken cancellationToken = default)
//  {
//    int i = 0;
//    try
//    {
//      while (!cancellationToken.IsCancellationRequested)
//      {
//        // Ждём новых данных или таймаута 1 сек
//        if (_signal.Wait(1000, cancellationToken))
//        {
//          _signal.Reset();

//          while (_queue.TryDequeue(out var map))
//          {
//            // Проверка данные пришли от client
//            if (map.TryGetValue(MdCommand.State.AsKey(), out var stateValue))
//            {
//              if(_clientName != stateValue) continue;

//              switch (_mode)
//              {
//                case SateMode.Initialization:
//                {
//                  // Проверка в наличии command если "" посылаем запрос "ok" подтверждение
//                  if (map.TryGetValue(MdCommand.Command.AsKey(), out string commandValue))
//                  {
//                    if (commandValue == MdCommand.Ok.AsKey())
//                    {
//                      _mode = SateMode.Work;
//                      continue;
//                    }
//                    else
//                    {
//                      var mapCommand = new MapCommands()
//                      {
//                        [MdCommand.State.AsKey()] = _nameModule,
//                        [MdCommand.Command.AsKey()] = MdCommand.Ok.AsKey(),
//                      };
//                      Md.WriteMetaMap(mapCommand);
//                      continue;
//                    }
//                  }
//                  else
//                  {// если нет Command а связь установлена посылаем command = ""
//                      var mapCommand = new MapCommands()
//                      {
//                        [MdCommand.State.AsKey()] = _nameModule,
//                        [MdCommand.Command.AsKey()] = "",
//                      };
//                      Md.WriteMetaMap(mapCommand);
//                      continue;
//                  }
//                  break;
//                }
//                case SateMode.Work:
//                { // если SateMode.Work разбираем map на управляющие значения для обработки данных

//                    break;
//                }
//                case SateMode.Dispose:
//                {
//                  break;
//                }

//                case SateMode.None:
//                default:
//                  throw new ArgumentOutOfRangeException();
//              }

//            }

//          }
//        }

//        // Ваши действия по циклу каждую секунду
//        var mapToWrite = new MapCommands()
//        {
//          [MdCommand.State.AsKey()] = _nameModule,
//          ["id_server"] = i.ToString(),
//        };
//        Md.WriteMetaMap(mapToWrite); // пишем pong
//        i++;
//      }
//    }
//    catch (OperationCanceledException)
//    {
//      // Обычное завершение
//    }
//    catch (Exception ex)
//    {
//      Console.WriteLine($"Ошибка в ReadDataCallBack: {ex}");
//    }
//  }

//  //private void ParserComman
//  private void CallBackMetaData(MapCommands map)
//  {
//    if(map == null || map.Count()==0)
//      return;
//    // Помещаем данные в очередь, сигналим цикл
//    _queue.Enqueue(map);
//    _signal.Set();

//    foreach (var kv in map)
//      Console.WriteLine($" - внешний уровень server == >  {kv.Key} = {kv.Value}");
//  }

//  public void Dispose()
//  {
//    Console.WriteLine($"ServerPong  -- Dispose() ");
//    _cts.Cancel();
//    Task.WaitAll(WaiteEvent);
//    Md?.Dispose();
//    _signal.Dispose();
//  }
//}

