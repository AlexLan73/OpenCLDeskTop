namespace DMemory.Core;

// Наследник для ClientMetaData
public class ClientMetaData : BaseMetaData
{
  private readonly string _clientName;

  public ClientMetaData(MetaSettings meta, MemoryDataProcessor processor)
    : base(meta, processor, "client", "server",
      meta.MetaEventServer, meta.MetaEventClient)
  {
    _clientName = "server" + meta.MemoryName;
  }
}



/*
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
    { // действия каждые 0.25 сек //
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
//    if (_txQueue.TryPeek(out var data))
    if (_txQueue.TryDequeue(out var data))
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
    _metadataSend.Add(MdCommand.Data.AsKey(), "_");
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

    map.Remove(MdCommand.State.AsKey());

    Console.WriteLine($"[{_nameModule}] Получено от {stateValue}:");

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
            Console.WriteLine($">>> [{_nameModule}] Handshake подтверждён, переход в Work");
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
            Console.WriteLine($">>> [{_nameModule}] Отправили ok для завершения handhsake");
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
        Console.WriteLine($">>> [{_nameModule}] Отправили пустой command  client -> server");
        break;
      }
      case SateMode.Work:
        {
          // Здесь будет основная логика работы: приём данных, реакции, управление
          // 👇 Пока ничего не шлём, ждём команды подтверждения
          //  Когда будут посылаться данные ставится TransferWaiting.Waiting !!
          // Здесь будет основная логика работы: приём данных, реакции, управление
          Console.WriteLine($">>> [{_nameModule}]  Работаем: получили данные в режиме CLIENT Work");
          // 👇 Пока ничего не шлём, ждём команды подтверждения

          if (map.Count < 2) return;
          _timer.ResetWork();
          _timer.ResetWorkSendCount();

          bool _isSend = false;   //  признак передачи
          MapCommands mapSend = new()
          {
            [MdCommand.State.AsKey()] = _nameModule,
          };
          //  Обработка Command
          if (map.ContainsKey(MdCommand.Command.AsKey()))
          {
            Console.WriteLine($">>> [{_nameModule}] map[Command] работаем в Work ");
            //  Обработка Command
            switch (map[MdCommand.Command.AsKey()])
            {
              case var key when key == MdCommand.DataOk.AsKey():
              {
                //  _mode = SateMode.Work; Это пришло подтверждение действий 
                _transferWaiting = TransferWaiting.Transfer; // подтверждение, что данные были приняты
                Console.WriteLine($">>> [{_nameModule}] map[Command] = Ok   ");
                break;
              }
              case "_":
              {
                if(mapSend.TryAdd(MdCommand.Command.AsKey(), MdCommand.Ok.AsKey()))
                  mapSend.Add(MdCommand.Command.AsKey(), MdCommand.Ok.AsKey());
                Console.WriteLine($">>> [{_nameModule}] map[Command] = _   ");
                _isSend = true;
                break;
              }
              case var key when key == MdCommand.Error.AsKey():
              {
                Console.WriteLine($">>> [{_nameModule}] map[Command] = error   ");
                break;
              }
            }
            map.Remove(MdCommand.Command.AsKey());
          }
          //  Обработка Data
          if (map.ContainsKey(MdCommand.Data.AsKey()))
          {
            Console.WriteLine($">>> [{_nameModule}] map[Data]  ");

            //  Обработка Data
            var _data = map.TryGetValue(MdCommand.Data.AsKey(), out var dataVal) ? dataVal : "";
            switch (_data)
            {
              case "_" :
              { // получили данные и должны проверить 
                Console.WriteLine($">>> [{_nameModule}] map[Data] = _  ");
                _isSend = true;

                  var sendReturn = _processor.ProcessMetaData(map);
                if (string.IsNullOrEmpty(sendReturn))
                {
                  if (map.TryAdd(MdCommand.Data.AsKey(), MdCommand.Error.AsKey()))
                    map.Add(MdCommand.Data.AsKey(), MdCommand.Error.AsKey());
                }
                else
                {
                  if (map.TryAdd(MdCommand.Data.AsKey(), sendReturn))
                    map.Add(MdCommand.Data.AsKey(), sendReturn);
                }
                break;
              }
              case var key when key == MdCommand.DataOk.AsKey() && _transferWaiting == TransferWaiting.Waiting:   // MdCommand.DataOk.AsKey():
              {
                _transferWaiting = TransferWaiting.Transfer;
                Console.WriteLine($">>> [{_nameModule}] map[Data] = DataOk  ");
                TrySendNext();
                break;
              }
              case var key when key == MdCommand.Error.AsKey():   // MdCommand.DataOk.AsKey():
              {  // должны повторить посылку 
                Console.WriteLine($">>> [{_nameModule}] map[Data] = Error  ");
                _transferWaiting = TransferWaiting.Transfer;
                  _processor.ResendData();
                break;
              }
            }
            map.Remove(MdCommand.Data.AsKey());
          }

          if (map.Any())
          { // Если есть не обработанные ключи. 
            Console.WriteLine($">>> [{_nameModule}] map еще ключи отладка  ");

            var searchTerms = new List<string> { MdCommand.State.AsKey(), "id" };
            var matchedKeys = map.Keys.ToList()
              .Where(key => searchTerms.Any(term => key.Contains(term, StringComparison.OrdinalIgnoreCase)))
              .ToList();
            if (matchedKeys.Count > 0)
            {
              //if()
              foreach (var kv in matchedKeys)
              {
                Console.WriteLine($" - внешний уровень [{_nameModule}] !!!!  в SERVER  == >  {kv} = {map[kv]}");
                map.Remove(kv);
              }

              if (mapSend.TryAdd(MdCommand.Command.AsKey(), MdCommand.Ok.AsKey()))
                mapSend.Add(MdCommand.Command.AsKey(), MdCommand.Ok.AsKey());
              _transferWaiting = TransferWaiting.Transfer;
              _isSend = true;
            }

            //            Md.WriteMetaMap(map);

          }

          // Проверяем, если _isSend=true отвечаем
          if (_isSend)
            Md.WriteMetaMap(map);
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

*/
/////////////////////////////////////////////////////////////////////////////////////
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

/*
 
case SateMode.Work:
   {
       // Лог для входа в режим работы — можно убрать или оставить для отладки
       Console.WriteLine(">>> Работаем: получили данные в режиме CLIENT Work");
   
       // Если метаданные почти пусты — скорее всего не нужно ничего делать
       if (map == null || map.Count < 1)
           break;
   
       // Сброс таймера активности (по твоему примеру)
       _timer.ResetWork();
       _timer.ResetWorkSendCount();
   
       // Получаем состояние от сервера (например, "servrCUDA")
       map.TryGetValue(MdCommand.State.AsKey(), out var stateValue);
   
       // Обрабатываем команды подтверждений и данные
   
       // Обработка команды подтверждения MdCommand.Ok
       if (map.TryGetValue(MdCommand.Ok.AsKey(), out var okValue))
       {
           if (!string.IsNullOrEmpty(okValue) && okValue.Equals("ok", StringComparison.OrdinalIgnoreCase))
           {
               Console.WriteLine(">>> [CLIENT] Получено подтверждение Ok от сервера");
   
               // Подтверждение, что предыдущий шаг принят
               _transferWaiting = TransferWaiting.Transfer;
   
               // Возможно, нужно удалить или сместить очередь на следующий пакет
               // Например:
               _dataQueue.TryDequeue(out _);
   
               // Запускаем проверку/отправку следующих данных
               TrySendNext();
   
               // Если есть дополнительные действия для Ok — сюда добавить
           }
       }
   
       // Обработка команды MdCommand.Data
       if (map.TryGetValue(MdCommand.Data.AsKey(), out var dataValue))
       {
           if (string.Equals(dataValue, "dataok", StringComparison.OrdinalIgnoreCase))
           {
               Console.WriteLine(">>> [CLIENT] Получено подтверждение данных dataok");
   
               // Данные успешно получены сервером — можно смещать очередь и формировать следующий пакет
               _dataQueue.TryDequeue(out _);
   
               TrySendNext();
           }
           else if (string.Equals(dataValue, "error", StringComparison.OrdinalIgnoreCase))
           {
               Console.WriteLine(">>> [CLIENT] Получена ошибка передачи данных — повторяем");
   
               // Повторяем отправку текущих данных (не удаляем из очереди!)
               // Можно вызвать повторный вызов или установить флаг повтора
               TrySendCurrentOrRetry();
           }
           else if (!string.IsNullOrEmpty(dataValue))
           {
               Console.WriteLine($">>> [CLIENT] Получены специальные данные: {dataValue}");
   
               // Если dataValue содержит другую информацию — возможно, нужно работать с памятью,
               // обработать служебные команды.
   
               // Если есть команда Ok, обработать её совместно
               if (okValue == null)
               {
                   // Если только data (без ok), всё равно стоит проверить очередь и отправить следующее
                   TrySendNext();
               }
               // Для сложной логики с памятью сюда вписать дополнительные кейсы
           }
           else
           {
               // Если dataValue пустой — возможно, просто информативная команда
               Console.WriteLine(">>> [CLIENT] Получены данные, но без специального действия");
           }
       }
   
       // Если ни Ok, ни Data не пришли — можно проверить другие команды, если необходимо
   
       // В конце можно следить за перезапуском таймеров или состояния
       break;
   }
   

 */
