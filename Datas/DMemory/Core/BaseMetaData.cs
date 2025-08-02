using Common.Core.Converter;
using DMemory.Core.Converter;
using DMemory.Enums;
using MapCommands = System.Collections.Generic.Dictionary<string, string>;
namespace DMemory.Core;

public abstract class BaseMetaData : IDisposable
{
  protected readonly MemoryDataProcessor _processor;
  protected readonly SemaphoreSlim _sendSemaphore = new(1, 1);
  protected readonly ConcurrentQueue<RamData> _txQueue = new();
  protected MapCommands? _metadataSend = null;

  protected readonly string _nameModule;
  protected readonly string _peerName;
  protected BasicMemoryMd Md;
  protected EventWaitHandle SendEvent;

  protected TransferWaiting _transferWaiting = TransferWaiting.None;
  protected SateMode _mode = SateMode.Initialization;
  protected readonly ServerMetaDataTimer _timer = new();
  private readonly List<IBaseToChannelConverter> _baseToChannelConverters;
  protected BaseMetaData(
      MetaSettings meta, MemoryDataProcessor processor,
      string myPrefix, string peerPrefix, string sendEventName, string mdEventName)
  {
    _nameModule = myPrefix + meta.MemoryName;
    _peerName = peerPrefix + meta.MemoryName;
    _processor = processor ?? throw new ArgumentNullException(nameof(processor));

    SendEvent = new EventWaitHandle(false, EventResetMode.AutoReset, sendEventName);
    Md = new BasicMemoryMd(mdEventName, meta.MetaSize, meta.ControlName, CallBackMetaData, SendEvent);
    _processor.MetaReady += OnMetaReady;
    _baseToChannelConverters = GetToChannelConverters;
    //  Таймеры инициализации — как в исходных файлах
    SystemPulseTimer.On250MilSec += () =>
    {
      if (_mode == SateMode.Work)
        _timer._timeWork = _timer.IncWork();
      else
        _timer.ResetWork();
      Comparison250MilSec();
    };
    SystemPulseTimer.On1Second += () =>
    {
      if (_mode == SateMode.Initialization)
        _timer._timeInitialization = _timer.IncInitialization();
      else _timer.ResetInitialization();
      Comparison1SecTimer();
    };
    SystemPulseTimer.On5Seconds += () => { _timer._timeGeneralWork = _timer.IncGeneralWork(); };
    SystemPulseTimer.Start();


    // "Рукопожатие" — отправка начального ack
    var initAck = new MapCommands { [MdCommand.State.AsKey()] = _nameModule };
    Md.WriteMetaMap(initAck);
    _timer.ResetAll();
  }

  private List<IBaseToChannelConverter> GetToChannelConverters =>
  [
    new DtVariableToChannelConverter(),
    new VDtValuesToChannelConverter(),
    new LoggerBaseToChannelConverter()
  ];

  // Теперь поддерживаем возможность передачи только MD-команд (data/dataType == null)
  /*
  public async Task EnqueueToSendAsync(RamData data)
  {
    if (data.Data != null && data.DataType != null)
    {
      _txQueue.Enqueue(data);
      await TrySendNextAsync();
      return;
    }

    // MD-команда без данных! Ждем, пока канал не занят
    await _sendSemaphore.WaitAsync();
    try
    {
      _processor.SendMetaCommand(data.MetaData); // <<== будет вызвано событие MetaReady, обработается ниже
    }
    catch     //  стало

    //    finally  было
    {
      _sendSemaphore.Release();
    }
  }
  */

  
    public async Task EnqueueToSendAsync(RamData data)
    {
      if (data.Data != null && data.DataType != null)
      {

        object objToSerialize = data.Data;
        Type typeToSerialize = data.DataType;

        // Ищем обратный конвертер — если тип совпадает с SourceType базового типа
        var forwardConverter = _baseToChannelConverters
          .FirstOrDefault(conv => conv.SourceType == data.DataType);

        if (forwardConverter != null)
        {
          objToSerialize = forwardConverter.Convert(data.Data);
          typeToSerialize = forwardConverter.TargetType;
        }

        // Создаем новый RamData с преобразованными типом и объектом
        var ramDataToSend = new RamData(objToSerialize, typeToSerialize, new MapCommands(data.MetaData));

        // Передаём дальше — как обычно
        _txQueue.Enqueue(ramDataToSend);
        await TrySendNextAsync();
        return;
      }
      //if (data == null) throw new ArgumentNullException(nameof(data));
      // MD-команда без данных! Ждем, пока канал не занят
      await _sendSemaphore.WaitAsync();
      try
      {
        _processor.SendMetaCommand(data.MetaData); // <<== будет вызвано событие MetaReady, обработается ниже
      }
      catch     //  стало

        //    finally  было
      {
        _sendSemaphore.Release();
      }

    }
  

  protected virtual async Task TrySendNextAsync()
  {
    if (!_txQueue.TryDequeue(out var data)) return;
    await _sendSemaphore.WaitAsync();
    try
    {
      _metadataSend = null;
      _processor.SerializeAndPrepare(data);
      // После окончания передачи MetaReady "освободит" семафор
    }
    finally
    {
      // Освобождение семафора будет только в OnMetaReady, чтобы не было гонки!
    }
  }

  protected virtual void OnMetaReady(object sender, MapCommands meta)
  {
    // Здесь мы освобождаем семафор для следующих команд или передачи
    _sendSemaphore.Release();

    if (_mode != SateMode.Work || _transferWaiting != TransferWaiting.Transfer)
      return;

    _metadataSend =  new MapCommands(meta);
//      _metadataSend[MdCommand.Data.AsKey()] = "_";  !!!!  нужно для чегото
    if (_metadataSend.TryAdd(MdCommand.State.AsKey(), _nameModule))
      _metadataSend[MdCommand.State.AsKey()] = _nameModule;

    _transferWaiting = _metadataSend.Values.Contains("_")? TransferWaiting.Waiting: TransferWaiting.Transfer;
//    _transferWaiting = TransferWaiting.Waiting;
    _processor.CommitWrite();
    Md.WriteMetaMap(_metadataSend);
  }

  protected virtual void CallBackMetaData(MapCommands map)
  {
    if (map == null || map.Count == 0) return;
    if (!map.TryGetValue(MdCommand.State.AsKey(), out var stateValue)) return;
    if (stateValue == _nameModule) return;
    map.Remove(MdCommand.State.AsKey());

    _timer.ResetGeneralWork();
    switch (_mode)
    {
      case SateMode.Initialization: HandleInitialization(map); break;
      case SateMode.Work: HandleWork(map); break;
      case SateMode.Dispose: Console.WriteLine($">>> [{_nameModule}] Завершение работы"); break;
      default: throw new ArgumentOutOfRangeException();
    }
  }

  protected virtual void HandleInitialization(MapCommands map)
  {
    if (map.TryGetValue(MdCommand.Command.AsKey(), out var cmdVal))
    {
      if (cmdVal == MdCommand.Ok.AsKey())
      {
        _mode = SateMode.Work;
        _transferWaiting = TransferWaiting.Transfer;
        _timer.ResetInitialization();
        Console.WriteLine($">>> [{_nameModule}] Handshake подтверждён, переход в Work");
        return;
      }
      else if (cmdVal == "_")
      {
        var reply = new MapCommands { [MdCommand.State.AsKey()] = _nameModule, [MdCommand.Command.AsKey()] = MdCommand.Ok.AsKey() };
        Md.WriteMetaMap(reply);
        _mode = SateMode.Work;
        _timer.ResetInitialization();
        _transferWaiting = TransferWaiting.Transfer;
        Console.WriteLine($">>> [{_nameModule}] Отправили ok для завершения handhsake");
        return;
      }
    }
    var initAck = new MapCommands { [MdCommand.State.AsKey()] = _nameModule, [MdCommand.Command.AsKey()] = "_" };
    Md.WriteMetaMap(initAck);
    Console.WriteLine($">>> [{_nameModule}] Sent empty command");
  }

  protected virtual void HandleWork(MapCommands map)
  {
    if (map.Count < 1) return;

    _timer.ResetWork();
    _timer.ResetWorkSendCount();
    bool isSend = false;
    var mapSend = new MapCommands { [MdCommand.State.AsKey()] = _nameModule };

    // --- Command branch
    if (map.TryGetValue(MdCommand.Command.AsKey(), out var command))
    {
      switch (command)
      {
        case var c when c == MdCommand.DataOk.AsKey():
          _transferWaiting = TransferWaiting.Transfer;
          Console.WriteLine($">>> [{_nameModule}] map[Command] = DataOk");
          break;
        case "_":
          mapSend.TryAdd(MdCommand.Command.AsKey(), MdCommand.Ok.AsKey());
          isSend = true;
          break;
        case var c when c == MdCommand.Error.AsKey():
          Console.WriteLine($">>> [{_nameModule}] map[Command] = error");
          break;
      }
      map.Remove(MdCommand.Command.AsKey());
    }

    // --- Data branch
    if (map.TryGetValue(MdCommand.Data.AsKey(), out var dataVal) || map.TryGetValue(MdCommand.Control.AsKey(), out var controlVal))
    {

//      var dataMap = string.IsNullOrEmpty(dataVal)? (string.IsNullOrEmpty(controlVal)? null: controlVal) : dataVal;// !=null? dataVal: controlVal!=null? controlVal:;
//      var dataMap = string.IsNullOrEmpty(dataVal)
//        ? (string.IsNullOrEmpty(controlVal) ? null : controlVal): dataVal;

      bool hasData = map.TryGetValue(MdCommand.Data.AsKey(), out dataVal);
      bool hasControl = map.TryGetValue(MdCommand.Control.AsKey(), out controlVal);

      if (hasData || hasControl)
      {
        var dataMap = !string.IsNullOrEmpty(dataVal)
          ? dataVal
          : (!string.IsNullOrEmpty(controlVal) ? controlVal : null);

        switch (dataMap)
        {
          case "_": // получили данные, должны обработать
          {
            isSend = true;
            var sendReturn = _processor.ProcessMetaData(map);
            if (string.IsNullOrEmpty(sendReturn))
            {
              mapSend[MdCommand.Data.AsKey()] = MdCommand.Error.AsKey();
            }
            else
            {
              mapSend[MdCommand.Data.AsKey()] = sendReturn;
            }

            // _arrKey — массив ключей для удаления
            string[] _arrKey = { MdCommand.Crc.AsKey(), MdCommand.Size.AsKey(), MdCommand.Type.AsKey() };
            // Собираем ключи для удаления, которые есть в словаре
            var keysToRemove = map.Keys.Intersect(_arrKey).ToList();
            // Удаляем по списку
            foreach (var key in keysToRemove)
              map.Remove(key);
            _transferWaiting = TransferWaiting.Transfer;

            break;
          }
          case var dv when dv == MdCommand.DataOk.AsKey() && _transferWaiting == TransferWaiting.Waiting:
          {
            _transferWaiting = TransferWaiting.Transfer;
            TrySendNextAsync().Wait(); // запустить следующий пакет
            break;
          }
          case var dv when dv == MdCommand.Error.AsKey():
          {
            _transferWaiting = TransferWaiting.Transfer;
            _processor.ResendData();
            break;
          }
        }
      }

      map.Remove(MdCommand.Data.AsKey());
    }

    if (map.TryGetValue(MdCommand.Control.AsKey(), out var controlValur))
    {

      
      map.Remove(MdCommand.Control.AsKey());
    }
    // --- Отладочные и лишние ключи
    if (map.Count > 0)
    {
      var keysToRemove = new List<string>();
      var searchTerms = new List<string> { MdCommand.State.AsKey(), "id" };
      foreach (var key in map.Keys)
      {
        Console.WriteLine($"Ключ: {key}, Значение: {map[key]}");
        if (searchTerms.Exists(term => key.Contains(term, StringComparison.OrdinalIgnoreCase)))
          keysToRemove.Add(key);

      }
      foreach (var key in keysToRemove)
        map.Remove(key);
    }

    if (isSend)
    {
      mapSend.TryAdd(MdCommand.Command.AsKey(), MdCommand.Ok.AsKey());
      _transferWaiting = TransferWaiting.Transfer;
      Md.WriteMetaMap(mapSend);
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
        // Сброс ожидания и таймера — нет связи
        _transferWaiting = TransferWaiting.None;
        _timer.ResetWork();

        // Пытаемся прочитать метаданные из памяти (без удаления)
        var currentMap = Md?.PeekMetaMap();

        // Проверка: чужое состояние и входящий "reset" — уходим в Work
        if (currentMap != null &&
            currentMap.TryGetValue(MdCommand.State.AsKey(), out var stateVal) &&
            stateVal != _nameModule &&
            currentMap.TryGetValue(MdCommand.Command.AsKey(), out var cmdVal) &&
            cmdVal == "_")
        {
          // Переход в режим работы и отправка ACK ("Ok") обратно
          _mode = SateMode.Work;
          var ack = new MapCommands
          {
            [MdCommand.State.AsKey()] = _nameModule,
            [MdCommand.Command.AsKey()] = MdCommand.Ok.AsKey()
          };
          Md.WriteMetaMap(ack);
          return;
        }

        // Если не сработало — отправляем инициализацию (наш "reset")
        var initAck = new MapCommands
        {
          [MdCommand.State.AsKey()] = _nameModule,
          [MdCommand.Command.AsKey()] = "_"
        };
        Md?.WriteMetaMap(initAck);
        return;
      }

      /*
            case SateMode.Initialization when (_timer.GetInitialization() % 5 == 1):
            {
              // Пропускаем resetInitialization, если надо - добавить

              _transferWaiting = TransferWaiting.None;
              _timer.ResetWork();

              var currentMap = Md?.PeekMetaMap();

              if (currentMap != null)
              {
                if (currentMap.TryGetValue(MdCommand.State.AsKey(), out var stateVal)
                    && stateVal != _nameModule
                    && currentMap.TryGetValue(MdCommand.Command.AsKey(), out var commandVal)
                    && commandVal == "_")
                {
                  // Переходим в режим работы
                  _mode = SateMode.Work;

                  // Формируем acknowledgement — аккуратнее поставить значение, а не ключ!
                  var ack = new MapCommands
                  {
                    [MdCommand.State.AsKey()] = _nameModule,
                    [MdCommand.Command.AsKey()] = MdCommand.Ok.AsKey() // Вероятно, имелось в виду "Ok"
                  };

                  Md.WriteMetaMap(ack);
                  return;
                }
              }

              // Если условия не сработали — отправляем пустой handshake "_"
              var initAck = new MapCommands
              {
                [MdCommand.State.AsKey()] = _nameModule,
                [MdCommand.Command.AsKey()] = "_"
              };
              Md?.WriteMetaMap(initAck);
              return;
            }
      */
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
        return;
        //throw new ArgumentOutOfRangeException();
    }
  }
  #endregion

  public virtual void Dispose()
  {
    Md?.Dispose();
    SendEvent?.Dispose();
    _processor.MetaReady -= OnMetaReady;
  }
  
  //protected virtual void On250MilSec() { }
  //protected virtual void On1Second() { }

  public TransferWaiting GeTransferWaiting() => _transferWaiting;
  public SateMode GetSateMode() => _mode;

}




//public abstract class BaseMetaData : IDisposable
//{
//  protected readonly MemoryDataProcessor _processor;

//  protected readonly ConcurrentQueue<RamData> _txQueue = new();

//  // Используем для временной памяти метаданных при передаче
//  protected MapCommands? _metadataSend = null;

//  protected BasicMemoryMd Md;

//  protected EventWaitHandle SendEvent;

//  protected TransferWaiting _transferWaiting = TransferWaiting.None;

//  protected SateMode _mode = SateMode.Initialization;

//  protected readonly ServerMetaDataTimer _timer = new ServerMetaDataTimer();

//  protected readonly string _nameModule;

//  protected readonly string _peerName;

//  protected CancellationTokenSource _cts;

//  protected Task _workerTask;

//  protected BaseMetaData(MetaSettings meta, MemoryDataProcessor processor,
//      string myPrefix, string peerPrefix,
//      string sendEventName, string mdEventName)
//  {
//    if (meta == null) throw new ArgumentNullException(nameof(meta));
//    if (processor == null) throw new ArgumentNullException(nameof(processor));

//    _nameModule = myPrefix + meta.MemoryName;
//    _peerName = peerPrefix + meta.MemoryName;

//    _processor = processor ?? throw new ArgumentNullException(nameof(processor));

//    // Ивент для сигнала другим процессам
//    SendEvent = new EventWaitHandle(false, EventResetMode.AutoReset, sendEventName);

//    // BasicMemoryMd — сюда передаем вызов коллбэка CallBackMetaData
//    Md = new BasicMemoryMd(mdEventName, meta.MetaSize, meta.ControlName, CallBackMetaData, SendEvent);

//    _processor.MetaReady += OnMetaReady;

//    _cts = new CancellationTokenSource();

//    // Запускаем системные таймеры (можно настроить твой SystemPulseTimer аналог)
//    SystemPulseTimer.On250MilSec += SystemPulseTimer_250ms;
//    SystemPulseTimer.On1Second += SystemPulseTimer_1s;
//    SystemPulseTimer.On5Seconds += SystemPulseTimer_5s;

//    SystemPulseTimer.Start();

//    // Отсылка начального сообщения (handshake)
//    var initAck = new MapCommands
//    {
//      [MdCommand.State.AsKey()] = _nameModule
//    };
//    Md.WriteMetaMap(initAck);

//    _timer.ResetAll();

//    _mode = SateMode.Initialization;
//    _transferWaiting = TransferWaiting.None;
//  }

//  private void SystemPulseTimer_250ms()
//  {
//    if (_mode == SateMode.Work)
//      _timer._timeWork = _timer.IncWork();
//    else
//      _timer.ResetWork();

//    On250MilSec();
//  }

//  private void SystemPulseTimer_1s()
//  {
//    if (_mode == SateMode.Initialization)
//      _timer._timeInitialization = _timer.IncInitialization();
//    else
//      _timer.ResetInitialization();

//    On1Second();
//  }

//  private void SystemPulseTimer_5s()
//  {
//    _timer._timeGeneralWork = _timer.IncGeneralWork();
//  }

//  // Позволяет наследникам при необходимости обработать эти таймеры
//  protected virtual void On250MilSec() { }
//  protected virtual void On1Second() { }

//  // Обработка события готовности метаданных из MemoryDataProcessor
//  protected virtual void OnMetaReady(object sender, MapCommands meta)
//  {
//    if (_mode != SateMode.Work || _transferWaiting != TransferWaiting.Transfer)
//      return;

//    if (_metadataSend == null)
//      _metadataSend = new MapCommands();

//    _metadataSend[MdCommand.State.AsKey()] = _nameModule;
//    _metadataSend[MdCommand.Data.AsKey()] = "_";

//    _transferWaiting = TransferWaiting.Waiting;

//    _processor.CommitWrite();

//    Md.WriteMetaMap(_metadataSend);
//  }

//  // Основной callback от BasicMemoryMd (при чтении метаданных)
//  protected virtual void CallBackMetaData(MapCommands map)
//  {
//    if (map == null || map.Count == 0) return;

//    if (!map.TryGetValue(MdCommand.State.AsKey(), out var stateValue)) return;

//    if (stateValue == _nameModule) return; // Игнорируем свои сообщения

//    map.Remove(MdCommand.State.AsKey());

//    Console.WriteLine($"[{_nameModule}] Получено от {stateValue}:");
//    foreach (var kv in map)
//      Console.WriteLine($" - {kv.Key} = {kv.Value}");

//    _timer.ResetGeneralWork();

//    switch (_mode)
//    {
//      case SateMode.Initialization:
//        HandleInitialization(map);
//        break;

//      case SateMode.Work:
//        HandleWork(map);
//        break;

//      case SateMode.Dispose:
//        Console.WriteLine($">>> [{_nameModule}] Режим Dispose");
//        break;

//      default:
//        throw new ArgumentOutOfRangeException();
//    }
//  }

//  protected virtual void HandleInitialization(MapCommands map)
//  {
//    if (map.TryGetValue(MdCommand.Command.AsKey(), out var cmdVal))
//    {
//      if (cmdVal == MdCommand.Ok.AsKey())
//      {
//        _mode = SateMode.Work;
//        _transferWaiting = TransferWaiting.Transfer;
//        _timer.ResetInitialization();
//        Console.WriteLine($">>> [{_nameModule}] Handshake подтверждён, переключение в Work");
//        return;
//      }
//      else if (cmdVal == "_")
//      {
//        var reply = new MapCommands
//        {
//          [MdCommand.State.AsKey()] = _nameModule,
//          [MdCommand.Command.AsKey()] = MdCommand.Ok.AsKey()
//        };
//        Md.WriteMetaMap(reply);
//        _mode = SateMode.Work;
//        _transferWaiting = TransferWaiting.Transfer;
//        _timer.ResetInitialization();
//        Console.WriteLine($">>> [{_nameModule}] Отправили Ok (завершение handshake)");
//        return;
//      }
//    }

//    // Если ничего, просто посылаем пустое подтверждение "_"
//    var initAck = new MapCommands
//    {
//      [MdCommand.State.AsKey()] = _nameModule,
//      [MdCommand.Command.AsKey()] = "_"
//    };
//    Md.WriteMetaMap(initAck);
//    Console.WriteLine($">>> [{_nameModule}] Отправили пустой command");
//  }

//  protected virtual void HandleWork(MapCommands map)
//  {
//    if (map.Count < 2) return;

//    _timer.ResetWork();
//    _timer.ResetWorkSendCount();

//    bool isSend = false;
//    var mapSend = new MapCommands
//    {
//      [MdCommand.State.AsKey()] = _nameModule
//    };

//    if (map.TryGetValue(MdCommand.Command.AsKey(), out var command))
//    {
//      switch (command)
//      {
//        case var cm when cm == MdCommand.DataOk.AsKey():
//          _transferWaiting = TransferWaiting.Transfer;
//          Console.WriteLine($">>> [{_nameModule}] Получена команда DataOk");
//          break;
//        case "_":
//          if (!mapSend.ContainsKey(MdCommand.Command.AsKey()))
//            mapSend.Add(MdCommand.Command.AsKey(), MdCommand.Ok.AsKey());
//          Console.WriteLine($">>> [{_nameModule}] Получена команда _ (пустая)");
//          isSend = true;
//          break;
//        case var cm when cm == MdCommand.Error.AsKey():
//          Console.WriteLine($">>> [{_nameModule}] Получена команда Error");
//          break;
//      }
//      map.Remove(MdCommand.Command.AsKey());
//    }

//    if (map.TryGetValue(MdCommand.Data.AsKey(), out var dataVal))
//    {
//      switch (dataVal)
//      {
//        case "_":
//          Console.WriteLine($">>> [{_nameModule}] Получены данные '_': должно быть подтверждение");
//          isSend = true;
//          var sendReturn = _processor.ProcessMetaData(map);
//          if (string.IsNullOrEmpty(sendReturn))
//            map[MdCommand.Data.AsKey()] = MdCommand.Error.AsKey();
//          else
//            map[MdCommand.Data.AsKey()] = sendReturn;
//          break;

//        case var dv when dv == MdCommand.DataOk.AsKey() && _transferWaiting == TransferWaiting.Waiting:
//          _transferWaiting = TransferWaiting.Transfer;
//          Console.WriteLine($">>> [{_nameModule}] Получены данные DataOk — продолжаем отправку");
//          TrySendNext();
//          break;

//        case var dv when dv == MdCommand.Error.AsKey():
//          Console.WriteLine($">>> [{_nameModule}] Получены данные Error — требуется повтор");
//          _transferWaiting = TransferWaiting.Transfer;
//          _processor.ResendData();
//          break;

//        default:
//          Console.WriteLine($">>> [{_nameModule}] Получены данные: {dataVal}");
//          break;
//      }
//      map.Remove(MdCommand.Data.AsKey());
//    }

//    // Удаляем некоторые ключи для чистоты
//    if (map.Count > 0)
//    {
//      var keysToRemove = new List<string>();
//      var searchTerms = new List<string> { MdCommand.State.AsKey(), "id" };

//      foreach (var key in map.Keys)
//      {
//        if (searchTerms.Exists(term => key.Contains(term, StringComparison.OrdinalIgnoreCase)))
//          keysToRemove.Add(key);
//      }

//      foreach (var key in keysToRemove)
//      {
//        Console.WriteLine($"  индекс удален из map: {key} = {map[key]}");
//        map.Remove(key);
//      }
//    }

//    if (isSend)
//    {
//      if (!mapSend.ContainsKey(MdCommand.Command.AsKey()))
//        mapSend.Add(MdCommand.Command.AsKey(), MdCommand.Ok.AsKey());

//      _transferWaiting = TransferWaiting.Transfer;
//      Md.WriteMetaMap(mapSend);
//    }
//  }

//  public virtual void EnqueueToSend(RamData data)
//  {
//    _txQueue.Enqueue(data);
//    TrySendNext();
//  }

//  protected abstract void TrySendNext();

//  public TransferWaiting GeTransferWaiting() => _transferWaiting;
//  public SateMode GetSateMode() =>  _mode;

//public virtual void Dispose()
//  {
//    Console.WriteLine($"[{_nameModule}] Dispose");

//    _cts?.Cancel();
//    Md?.Dispose();
//    SendEvent?.Dispose();

//    SystemPulseTimer.Stop();
//  }
//}

// Наследник для ClientMetaData
//public class ClientMetaData : BaseMetaData
//{
//  private readonly string _clientName;

//  public ClientMetaData(MetaSettings meta, MemoryDataProcessor processor)
//      : base(meta, processor, "client", "server",
//            meta.MetaEventServer, meta.MetaEventClient)
//  {
//    _clientName = "server" + meta.MemoryName;
//  }

//  protected override void TrySendNext()
//  {
//    if (_txQueue.TryDequeue(out var data))
//    {
//      _metadataSend = null;
//      _processor.SerializeAndPrepare(data); // реакция по событию MetaReady
//    }
//  }

//  // Можно добавить переопределения On250MilSec и On1Second, если нужно
//}

//// Наследник для ServerMetaData
//public class ServerMetaData : BaseMetaData
//{
//  private readonly string _clientName;

//  public ServerMetaData(MetaSettings meta, MemoryDataProcessor processor)
//      : base(meta, processor, "server", "client",
//            meta.MetaEventClient, meta.MetaEventServer)
//  {
//    _clientName = "client" + meta.MemoryName;
//  }

//  protected override void TrySendNext()
//  {
//    if (_txQueue.TryDequeue(out var data))
//    {
//      _metadataSend = null;
//      _processor.SerializeAndPrepare(data);
//    }
//  }
//}

