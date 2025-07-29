namespace DMemory.Core.Copy;

using System.Threading;
using MapCommands = Dictionary<string, string>;

public class MemoryBaseNew :IDisposable
{
  private string NameMemoryData { get;  }
  private string NameMemoryDataControl { get;  }
  private string EventNameMemoryDataControl { get; }
  private EventWaitHandle EventMemoryDataControl { get; }
  private Task _waiteEventRead;
  private readonly MemoryMappedFile _mmDataControl;
//  private MemoryMappedFile _mmData;
  
  private Action<RecDataMetaData> _callBack = delegate { }; // Инициализация пустым делегатом
  private CancellationTokenSource _cts;
  private bool _disposed = false;
  private readonly TypeBlockMemory _typeBlockMemory;
  private readonly object _syncLock = new object();
  private readonly MemoryMappedFile _mmData; // Убираем '?' - теперь он будет всегда инициализирован
  private readonly long _dataSegmentSize; // Добавляем поле для хранения размера

  public MemoryBaseNew(string nameMemory, TypeBlockMemory typeBlockMemory, Action<RecDataMetaData> callBack=null,
          long dataSegmentSize = MemStatic.SizeDataSegment)
  {
    NameMemoryData = nameMemory;
    _typeBlockMemory = typeBlockMemory;
    _dataSegmentSize = dataSegmentSize; // Сохраняем размер

    NameMemoryDataControl = $"{NameMemoryData}Control";
    EventNameMemoryDataControl = @$"Global\Event{NameMemoryData}";

    _mmDataControl = MemoryMappedFile.CreateOrOpen(NameMemoryDataControl, MemStatic.SizeDataControl);
    // === ГЛАВНОЕ ИЗМЕНЕНИЕ: Инициализируем _mmData ЗДЕСЬ, ОДИН РАЗ ===
    _mmData = MemoryMappedFile.CreateOrOpen(NameMemoryData, _dataSegmentSize);

    EventMemoryDataControl = new EventWaitHandle(
      false,
      EventResetMode.AutoReset,
      EventNameMemoryDataControl,
      out var createdNew
    );
    Console.WriteLine(createdNew ? $"Создано новое событие {NameMemoryData}" : $"Подключено к существующему событию {NameMemoryData}");

    if(callBack==null)
      return;

    InitializationCallBack(_callBack);
  }

  public void InitializationCallBack(Action<RecDataMetaData> callBack)
  {
    if (callBack == null)
      return;

    _callBack = callBack;

    _cts = new CancellationTokenSource();
    var token = _cts.Token;
    if (_typeBlockMemory == TypeBlockMemory.Read)
      _waiteEventRead = ReadDataCallBack(token);

  }

  /// <summary>
  /// Передаем командную строку для записи в раздел контроль и запускаем event
  /// </summary>
  /// <param name="sData"></param>
  public void SetCommandControl(MapCommands dic)
  {
    var sData = ConvertDictToString(dic);
    using (var accessor = _mmDataControl.CreateViewAccessor())
    {
      var data = Encoding.UTF8.GetBytes(sData);
      accessor.WriteArray(0, data, 0, data.Length);
    }
    EventMemoryDataControl.Set();
  }
  public MapCommands GetCommandControl()
  {
    // 1. Ожидание сигнала от записывающего процесса
//    _eventMemoryDataControl.WaitOne();

    using (var accessor = _mmDataControl.CreateViewAccessor())
    {
      // 2. Чтение данных
      // Читаем все данные из памяти. Важно, чтобы размер буфера 
      // был достаточным. accessor.Capacity предоставляет весь размер.
      var data = new byte[accessor.Capacity];
      accessor.ReadArray(0, data, 0, data.Length);

      // Убираем "пустые" байты в конце, если строка была короче буфера
      int actualLength = Array.FindLastIndex(data, b => b != 0) + 1;
      if (actualLength == 0 && data[0] != 0) actualLength = data.Length; // Если весь массив заполнен
      if (actualLength == 0) return new MapCommands(); // Память пуста

      // 3. Декодирование из UTF8
      var sData = Encoding.UTF8.GetString(data, 0, actualLength);

      // 4. Десериализация строки обратно в словарь
      return ConvertStringToDict(sData);
    }
  }

  public void ClearCommandControl()
  {
    Trace.WriteLine("[Команда] Очистка разделяемой памяти...");

    // 1. Получаем доступ к памяти
    using (var accessor = _mmDataControl.CreateViewAccessor())
    {
      // 2. Записываем пустой массив байтов по всему объему памяти
      accessor.WriteArray(0, MemStatic.EmptyBuffer, 0, MemStatic.SizeDataControl);
    }

    // 3. Подаем сигнал, чтобы "разбудить" всех ожидающих читателей
    // Они проснутся и прочитают пустые данные.
    Console.WriteLine("[Команда] Память очищена. Подаю сигнал.");
   
  }
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
        while (!cancellationToken.IsCancellationRequested)
        {
          if (!EventMemoryDataControl.WaitOne(1000)) continue;

          // Блокируем доступ на время чтения
          lock (_syncLock) // Добавляем объект для синхронизации
          {
            using var accessor = _mmDataControl.CreateViewAccessor();
            var buffer = new byte[MemStatic.SizeDataControl];
            accessor.ReadArray(0, buffer, 0, buffer.Length);
            var received = Encoding.UTF8.GetString(buffer).TrimEnd('\0');
            Console.WriteLine("Received ReadDataCallBack: " + received);

            // Сбрасываем данные в памяти после чтения (опционально)
            accessor.WriteArray(0, new byte[MemStatic.SizeDataControl], 0, MemStatic.SizeDataControl);

            var map = ConvertStringToDict(received);
            if (map==null)
            {
              EventMemoryDataControl.Reset();
              return;
            }

            var size = Convert.ToInt32(map["size"]);
            var bytes = ReadMemoryData(size);

            _callBack.Invoke(new RecDataMetaData(bytes, map));
          }

          // Для AutoReset режима это не обязательно, но для надежности:
          EventMemoryDataControl.Reset();
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Ошибка в ReadDataCallBack: {ex}");
        // Можно добавить уведомление об ошибке через callback
//        _callBack.Invoke($"ERROR: {ex.Message}");
        _callBack.Invoke(null);
      }
    }, cancellationToken);
  }

  public string ConvertDictToString(MapCommands dic)
  {
    if (dic == null || !dic.Any())
      return null;

    return string.Join(";", dic.Select(x => $"{x.Key}={x.Value}")) + ";";
  }
  public MapCommands ConvertStringToDict(string str)
  {
    if(string.IsNullOrEmpty(str))
      return null;

    return str.Split(';', StringSplitOptions.RemoveEmptyEntries)
      .Select(s => s.Split('=', 2, StringSplitOptions.RemoveEmptyEntries))
      .Where(parts => parts.Length == 2)
      .ToDictionary(parts => parts[0], parts => parts[1]);
  }


  //////////////////////////////////////
  // Запись данных в общую память
  public virtual void WriteByteData(byte[] bytes)
  {
    if (bytes.Length > _dataSegmentSize)
    {
      throw new ArgumentOutOfRangeException(nameof(bytes), $"Размер данных ({bytes.Length}) превышает выделенный сегмент памяти ({_dataSegmentSize}).");
    }

    // --- УДАЛЯЕМ ЭТУ СТРОКУ ---
    // _mmData = MemoryMappedFile.CreateOrOpen(NameMemoryData, bytes.Length);

    // Используем уже существующий _mmData
    using var accessor = _mmData.CreateViewAccessor();
    accessor.WriteArray(0, bytes, 0, bytes.Length);
  }

  public virtual void WriteByteData(byte[] bytes, MapCommands map)
  {
    if (bytes.Length > _dataSegmentSize)
    {
      throw new ArgumentOutOfRangeException(nameof(bytes), $"Размер данных ({bytes.Length}) превышает выделенный сегмент памяти ({_dataSegmentSize}).");
    }

    // --- УДАЛЯЕМ ЭТУ СТРОКУ ---
    // _mmData = MemoryMappedFile.CreateOrOpen(NameMemoryData, bytes.Length);

    using var accessor = _mmData.CreateViewAccessor();
    accessor.WriteArray(0, bytes, 0, bytes.Length);
    SetCommandControl(map);
  }

  public virtual byte[] ReadMemoryData(int sizeData)
  {
    if (sizeData <= 0)
      throw new ArgumentException("Размер данных должен быть положительным");
    if (sizeData > _dataSegmentSize)
      throw new ArgumentOutOfRangeException(nameof(sizeData), $"Запрошенный размер ({sizeData}) превышает выделенный сегмент памяти ({_dataSegmentSize}).");

    try
    {
      using var accessor = _mmData.CreateViewAccessor();
      var buffer = new byte[sizeData];
      accessor.ReadArray(0, buffer, 0, buffer.Length);
      ClearCommandControl();
      return buffer;
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Ошибка чтения из памяти: {ex}");
      throw;
    }
  }

  public virtual void Dispose(bool disposing)
  {
    if (_disposed) return;

    if (disposing)
    {
      _cts?.Cancel();
      _waiteEventRead?.Wait(TimeSpan.FromSeconds(1)); // Ограниченное ожидание
      EventMemoryDataControl?.Dispose();
      _mmDataControl?.Dispose();
      _cts?.Dispose();
    }
    _disposed = true;
  }

  public void Dispose()
  {
    Dispose(true);
    GC.SuppressFinalize(this);
  }
}


