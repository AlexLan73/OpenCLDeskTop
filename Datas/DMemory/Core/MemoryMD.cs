using Common.Event;
using DMemory.Enum;


namespace DMemory.Core;
using MapCommands = System.Collections.Generic.Dictionary<string, string>;

public class MemoryMd : IDisposable
{
  #region ==== Data =====
  #region ===_ Constant _===
  private readonly ClientServer _role;              //  client server 
  private readonly string _nameRole;
  public string NameClient  { get; } =  "client";
  public string NameServer { get; } = "server";
  public string State { get; } = "state";
  public string Command { get; } = "command";
  public string Work { get; } = "work";
  public string Ok { get; } = "ok";
  public string No { get; } = "no";
  public string Error { get; } = "error";
  public string Crc{ get; } = "crc";
  #endregion
  #region ===_ Memory _===
  private string NameMemory { get; }          //  ключевое название CUDA, clFFT
  private string NameMdControl { get; }       //  полное название
  private string EventNameMdRead { get; }  //  название для события для сервера 
  private EventWaitHandle EventMdRead { get; }       // событие
  private string EventNameMdWrite { get; }  //  название для события для client 
  private EventWaitHandle EventMdWrite { get; }       // событие  client
  private readonly Task _waiteEventReadServer;                         // ожидание завершения потока

  private readonly MemoryMappedFile _memoryMappedFile;  // handler файла 
  private Action<MapCommands> _callBack;            // Инициализация пустым делегатом передать данные на верх
  private readonly CancellationTokenSource _cts;                 // завершение потока
  private bool _disposed = false;
  private readonly object _syncLock = new object();
  #endregion
  #region ===_ Time _===
  private int _oneSecCounter = 0;
  private int _fiveSecCounter = 0;
  private int _missedWorkAcks = 0; // сколько раз не получили ack
  private int _workOkExpecting = 0;
  private int _initTimer = 0;
  #endregion
  #region ===_ SateMode _===
  // Храним как int для атомарного доступа
  private int _sateMode = (int)SateMode.None;
  public SateMode SateMode
  {
    get => (SateMode)Interlocked.CompareExchange(ref _sateMode, 0, 0);
    set => Interlocked.Exchange(ref _sateMode, (int)value);
  }
  #endregion
  #endregion

  private MapCommands _controlMap = new();
  #region ===- Constructor -===
  public MemoryMd(string nameMemory, ClientServer role, Action<MapCommands> callBack = null)
  {
    SateMode = SateMode.None;
    NameMemory = nameMemory;
    _role = role;
    _nameRole = _role.ToString().ToLower();
    _controlMap.Add(State, _nameRole);
    NameMdControl = $"{NameMemory}Control";                     // имя metadata контроль
    EventNameMdRead = @$"Global\Event{NameMemory}Read";     // событие сервер
    EventNameMdWrite = @$"Global\Event{NameMemory}Write";     // событие сервер
                                                                //    EventMdRead = @$"Global\Event{NameMdControl}"; // событие
    string xxx = "";
    _memoryMappedFile = MemoryMappedFile.CreateOrOpen(NameMdControl, MemStatic.SizeDataControl);

    EventMdRead = new EventWaitHandle(
      false,
      EventResetMode.AutoReset,
//      EventNameMdRead,
      role == ClientServer.Server ? EventNameMdRead : EventNameMdWrite,
      out var createdNewServer
    );
    Trace.WriteLine(createdNewServer ? $"Создано новое событие {NameMemory}Server" : $"Подключено к существующему событию {NameMemory}");


    EventMdWrite = new EventWaitHandle(
      false,
      EventResetMode.AutoReset,
      role != ClientServer.Server ? EventNameMdWrite:EventNameMdRead,
      out var createdNewClient
    );
    Trace.WriteLine(createdNewClient ? $"Создано новое событие {NameMemory}Client" : $"Подключено к существующему событию {NameMemory}");

    //    
    _callBack = callBack;
    SateMode = SateMode.Initialization;
    _cts = new CancellationTokenSource();
    var token = _cts.Token;
    _waiteEventReadServer = ReadDataCallBack(token);
    ResetAllTimer();
    SystemPulseTimer.On250MilSec += () =>
    { /* действия каждые 0.5 сек */
      if(SateMode.Work == SateMode)
        _initTimer = IncInit();
    };
    SystemPulseTimer.On1Second += ComparisonTimer; 

//    SystemPulseTimer.On1Second += () => { /* проверки или запрос [work]="" */ };
//    SystemPulseTimer.On5Seconds += () => { /* переход в init или reset таймеров */ };

    SystemPulseTimer.Start();
  }

  public void InitializationCallBack(Action<MapCommands> callBack)
  {
    if (callBack == null)
      return;

    _callBack = callBack;
  }
  #endregion

  #region ===-- ComparisonTimer ---
  private void ComparisonTimer()
  {
    if (SateMode.Work != SateMode)
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

  }
  #endregion

  #region ===-- WriteInMemoryMd - Get - Clear --===
  /// <summary>
  /// Передаем командную строку для записи в раздел контроль и запускаем event
  /// </summary>
  public void WriteInMemoryMd(MapCommands dict)
  {
//    EventMdRead.Reset();
    if (_role == ClientServer.Server)
      EventMdWrite.Reset();
    else
      EventMdRead.Reset();

    ClearMemoryMd();
    using (var accessor = _memoryMappedFile.CreateViewAccessor())
    {
      var data = Encoding.UTF8.GetBytes(ConvertDictToString(dict));
      accessor.WriteArray(0, data, 0, data.Length);
    }

    if (_role == ClientServer.Server)
      EventMdWrite.Set();
    else
      EventMdRead.Set();

  }
  public MapCommands ReadMemoryMd()
  {
    using var accessor = _memoryMappedFile.CreateViewAccessor();
    // 2. Чтение данных
    var data = new byte[accessor.Capacity];
    accessor.ReadArray(0, data, 0, data.Length);

    // Убираем "пустые" байты в конце, если строка была короче буфера
    var actualLength = Array.FindLastIndex(data, b => b != 0) + 1;
    if (actualLength == 0 && data[0] != 0) actualLength = data.Length; // Если весь массив заполнен
    if (actualLength == 0) return new MapCommands(); // Память пуста

    // 3. Декодирование из UTF8
    var sData = Encoding.UTF8.GetString(data, 0, actualLength);

    // 4. Десериализация строки обратно в словарь
    return ConvertStringToDict(sData);
  }
  public (bool, MapCommands?) ReadMemoryMdAndClear()
  {
    using var accessor = _memoryMappedFile.CreateViewAccessor();
    // 2. Чтение данных
    var data = new byte[accessor.Capacity];
    accessor.ReadArray(0, data, 0, data.Length);

    // Убираем "пустые" байты в конце, если строка была короче буфера
    var actualLength = Array.FindLastIndex(data, b => b != 0) + 1;
    if (actualLength == 0 && data[0] != 0) actualLength = data.Length; // Если весь массив заполнен
    if (actualLength == 0) return (false, null); // Память пуста

    // 3. Декодирование из UTF8
    var sData = Encoding.UTF8.GetString(data, 0, actualLength);
    
    // 4. Десериализация строки обратно в словарь
    var _map = ConvertStringToDict(sData);

    if (_map.ContainsKey(State) && _map[State] == _nameRole) 
      return (false, _map);

    using (var accessor1 = _memoryMappedFile.CreateViewAccessor())
    {
      // 2. Записываем пустой массив байтов по всему объему памяти
      accessor1.WriteArray(0, MemStatic.EmptyBuffer, 0, MemStatic.SizeDataControl);
    }

    return (true, _map);
  }
  public void ClearMemoryMd()
  {
    Trace.WriteLine("[Команда] Очистка разделяемой памяти...");

    // 1. Получаем доступ к памяти
    using (var accessor = _memoryMappedFile.CreateViewAccessor())
    {
      // 2. Записываем пустой массив байтов по всему объему памяти
      accessor.WriteArray(0, MemStatic.EmptyBuffer, 0, MemStatic.SizeDataControl);
    }

    // 3. Подаем сигнал, чтобы "разбудить" всех ожидающих читателей
    // Они проснутся и прочитают пустые данные.
    Trace.WriteLine("[Команда] Память очищена. Подаю сигнал.");
  }
  #endregion

  /// <summary>
  /// Постоянный цикл, проверяем событие на получение данных
  /// данные получены возвращает строку с данными для парсинга
  /// </summary>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  private async Task ReadDataCallBack(CancellationToken cancellationToken = default)
  {
    await Task.Run(() =>
    {
      try
      {
        {
          var (@is, map) = ReadMemoryMdAndClear();
          if (!@is)
          {
            if(map==null)
              ClearMemoryMd();

            //if (_role == ClientServer.Server)
            //  EventMdWrite.Reset();
            //else
            //  EventMdRead.Reset();
          }
          map = new()
          {
            [State] = _nameRole,
//            [Command] = "",
          };
          SateMode = SateMode.Initialization;
          ResetAllTimer();
          WriteInMemoryMd(map);
        }

        while (!cancellationToken.IsCancellationRequested)
        {
          if (!EventMdRead.WaitOne(1000)) continue;

          // Блокируем доступ на время чтения
          lock (_syncLock) // Добавляем объект для синхронизации
          {

            var (@is, map) = ReadMemoryMdAndClear();
            if(!@is)
            {
              //EventMdRead.Reset();
              continue;
            }

            if (map.TryAdd("satemode", SateMode.ToString()))
              map["satemode"] = SateMode.ToString();

            switch (SateMode)
            {
              case SateMode.Initialization:
              {
                // инициализация проходит [commanda]=ok переходим в SateMode.Work:
                //SateMode= SateMode.Work;
                if (map.TryGetValue(State, out string receivedState) && receivedState!=null)
                {
                  if (_role == ClientServer.Client)
                  {
                    if (receivedState == NameServer)
                    {
                      if (map.TryGetValue(Command, out var receivedCommand) && receivedCommand!=null)
                      {
                        if (receivedCommand == Ok)
                        {
                          SateMode = SateMode.Work;
                          if (_role == ClientServer.Server)
                            EventMdWrite.Reset();
                          else
                            EventMdRead.Reset();
                        }

                      }
                      else
                      {
                        map = new()
                        {
                          [State] = NameClient,
                          [Command] = Ok,
                        };
                        SateMode = SateMode.Work;
                        ResetAllTimer();
                        WriteInMemoryMd(map);
                      }
                    }
                  }
                  else if (_role == ClientServer.Server)
                  {
                    if (receivedState == NameClient)
                    {
                      if (map.TryGetValue(Command, out string receivedCommand) && receivedCommand != null)
                      {
                        if (receivedCommand == Ok)
                        {
                          SateMode = SateMode.Work;
                          ClearMemoryMd();
                          ResetAllTimer();
                          if (_role == ClientServer.Server)
                            EventMdWrite.Reset();
                          else
                            EventMdRead.Reset();
                        }
                      }
                      else
                      {
                        map = new()
                        {
                          [State] = NameServer,
                          [Command] = Ok
                        };
                        SateMode = SateMode.Work;
//                        ClearMemoryMd();
                        WriteInMemoryMd(map);
                        ResetAllTimer();
                      }
                    }
                  }

                }
//                _callBack?.Invoke(map);   EventMdRead.Reset();
                break;
              }
              case SateMode.Work:
              {
                if (!map.TryGetValue(State, out var receivedState) && receivedState!=null)
                { // сработало прерывание, но State нет, сбрасываем прерывание и выходим
                  if (_role == ClientServer.Server)
                    EventMdWrite.Reset();
                  else
                    EventMdRead.Reset();
                  break;
                }

                if (receivedState == _nameRole)
                {
                  EventMdRead.Reset();
                  break;
                }

                if (!map.TryGetValue(Work, out string receivedWork) && receivedWork!=null)
                { // ищем, есть ли запрос на подтверждение работы work="" если есть посылаем подтверждение work="ok"
                  map = new()
                  {
                    [Work] = Ok,
                    [State] = _nameRole
                  };
                  WriteInMemoryMd(map);
                  break;
                }

                ResetAllTimer();
                _callBack?.Invoke(map);
                if (_role == ClientServer.Server)
                  EventMdWrite.Reset();
                else
                  EventMdRead.Reset();
                break;
              }
              case SateMode.Dispose:
                ResetAllTimer();
                _callBack?.Invoke(map);
                if (_role == ClientServer.Server)
                  EventMdWrite.Reset();
                else
                  EventMdRead.Reset();
                continue;

                break;

              case SateMode.None:
              default:
                if (_role == ClientServer.Server)
                  EventMdWrite.Reset();
                else
                  EventMdRead.Reset();
                continue;
            }
//            _callBack?.Invoke(map);
          }

          // Для AutoReset режима это не обязательно, но для надежности:
         // EventMdRead.Reset();
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Ошибка в ReadDataCallBack: {ex}");
        // Можно добавить уведомление об ошибке через callback
        //        _callBack.Invoke($"ERROR: {ex.Message}");
        _callBack?.Invoke(null);
      }
    }, cancellationToken);
  }

  #region ===-- Convert --===
  public string ConvertDictToString(MapCommands dic)
  {
    if (dic == null || !dic.Any())
      return null;

    return string.Join(";", dic.Select(x => $"{x.Key}={x.Value}")) + ";";
  }
  public MapCommands ConvertStringToDict(string str)
  {
    if (string.IsNullOrEmpty(str))
      return null;

    return str.Split(';', StringSplitOptions.RemoveEmptyEntries)
      .Select(s => s.Split('=', 2, StringSplitOptions.RemoveEmptyEntries))
      .Where(parts => parts.Length == 2)
      .ToDictionary(parts => parts[0], parts => parts[1]);
  }
  #endregion

  #region ===-- Dispose --===
  public virtual void Dispose(bool disposing)
  {
    if (_disposed) return;

    if (disposing)
    {
      _cts?.Cancel();
      _waiteEventReadServer?.Wait(TimeSpan.FromSeconds(1)); // Ограниченное ожидание
      EventMdRead?.Dispose();
      _memoryMappedFile?.Dispose();
      _cts?.Dispose();
    }
    _disposed = true;
  }
  public void Dispose()
  {
    Dispose(true);
    GC.SuppressFinalize(this);
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
  }

  // Все методы увеличения/сброса - так же через Interlocked
  public int IncOneSec() => Interlocked.Increment(ref _oneSecCounter);
  public int IncFiveSec() => Interlocked.Increment(ref _fiveSecCounter);
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

  public SateMode GetSateMode() => SateMode;
  public void SetSateMode(SateMode sm) => SateMode = sm;
  #endregion




}


