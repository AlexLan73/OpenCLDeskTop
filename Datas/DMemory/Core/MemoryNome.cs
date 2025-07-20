namespace DMemory.Core;
using MapCommands = Dictionary<string, string>;

/// <summary>
/// Все рассматривается с позиции сервера
/// Server
///  блок  TypeBlockMemory.Read, callBackCommandControl     память CUDARead чтение  ReadData
///    блок сервера на чтение реагирует на Event, считывает строку и выбрасывает callBackCommandControl
///  блок  TypeBlockMemory.Write, память CUDAWrite пишем в WriteData и Записываем в SetControlData команду
///
///  Client
///   ....   все на оборот 
/// </summary>

/*
 
public class MemoryNome : IDisposable
   {
   
       /// Записать команду (словарь) в MD
       public void CommandControlWrite(Dictionary<string, string> command)
           => _memoryWrite.WriteInMemoryMd(command);
   
       /// Прочитать текущую запись из MD (control)
       public Dictionary<string, string> ReadCommandControlWrite()
           => _memoryWrite.ReadMemoryMd();
   
       /// Прочитать текущую запись из MD (read-канал)
       public Dictionary<string, string> ReadCommandControlRead()
           => _memoryRead.ReadMemoryMd();
   
       /// Очистить MD ("write"-канал)
       public void ClearCommandControlWrite()
           => _memoryWrite.ClearMemoryMd();
   
       /// Очистить MD ("read"-канал)
       public void ClearCommandControlRead()
           => _memoryRead.ClearMemoryMd();
   
       /// Проверить наличие конкретной команды
       public bool TryGetCommand(string key, out string? value)
       {
           var map = ReadCommandControlWrite();
           return map.TryGetValue(key, out value);
       }
   
       public void Dispose()
       {
           _memoryRead?.Dispose();
           _memoryWrite?.Dispose();
       }
   }
   
 
 */

public class MemoryNome:IDisposable
{
  public string NameMemory { get; }
  public ServerClient ServerClient { get; }
  private  Action<MapCommands> _setCommandControl;
  private readonly Action<byte[]> _actionWriteByteData;
  private  Action<byte[], MapCommands> _actionWriteByteDataM;
  private readonly Func<int, byte[]> _funcReadByteData;
  private readonly MemoryBase _memoryRead;
  private readonly MemoryBase _memoryWrite;
  private readonly List<string> commandHistory = new List<string>();
  public MemoryNome(string nameMemory, ServerClient serverClient)
  {
    NameMemory = nameMemory;
    ServerClient = serverClient;
    if (ServerClient == ServerClient.Server)
    {
      _memoryRead = new MemoryBase(nameMemory + "Read", TypeBlockMemory.Read);
      _memoryWrite = new MemoryBase(nameMemory + "Write", TypeBlockMemory.Write);
    }
    else
    {
      _memoryRead = new MemoryBase(nameMemory + "Write", TypeBlockMemory.Read);
      _memoryWrite = new MemoryBase(nameMemory + "Read", TypeBlockMemory.Write);
    }
  }
  public void WriteCommand(string command)
  {
    commandHistory.Add(command);
    // Дополнительно: реально записывать в память/буфер/файл
  }
  public string ReadLastCommand()
  {
    return commandHistory.Count > 0 ? commandHistory.Last() : null;
  }
  public IReadOnlyList<string> GetAllCommands()
  {
    return commandHistory.AsReadOnly();
  }

  /// Записать команду (словарь) в MD
  public void CommandControlWrite(Dictionary<string, string> command)
    => _memoryWrite.SetCommandControl(command);

  /// Прочитать текущую запись из MD (control)
  public Dictionary<string, string> ReadCommandControlWrite()
    => _memoryWrite.GetCommandControl();

  /// Прочитать текущую запись из MD (read-канал)
  public Dictionary<string, string> ReadCommandControlRead()
    => _memoryRead.GetCommandControl();

  /// Очистить MD ("write"-канал)
  public void ClearCommandControlWrite()
    => _memoryWrite.ClearCommandControl();

  /// Очистить MD ("read"-канал)
  public void ClearCommandControlRead()
    => _memoryRead.ClearCommandControl();

  /// Проверить наличие конкретной команды
  public bool TryGetCommand(string key, out string? value)
  {
    var map = ReadCommandControlWrite();
    return map.TryGetValue(key, out value);
  }

  public void Dispose()
  {
    _memoryRead?.Dispose();
    _memoryWrite?.Dispose();
  }


//  public void CommandControlWrite(MapCommands command) => _setCommandControl(command);
  public byte[] ReadMemoryData(int count) => _funcReadByteData(count);
  public void WriteDataToMemory(byte[] bytes) => _actionWriteByteData(bytes);
  public void WriteDataToMemory(byte[] bytes, MapCommands map) => _actionWriteByteDataM(bytes, map);
  public DateTime ParseCudaDate(string dateString)
  {
    // "format" должен быть определен в вашем классе
    const string format = "yyyy.MM.dd HH:mm:ss.fff";
    return DateTime.ParseExact(dateString, format, CultureInfo.InvariantCulture);
  }

//  public virtual MapCommands ReadCommandControlWrite() => new MapCommands();

  public virtual void CallbackCommandDatAction(RecDataMetaData dMetaData)
  {

  }

  private void ReleaseUnmanagedResources()
  {
    // TODO release unmanaged resources here
  }
  protected virtual void Dispose(bool disposing)
  {
    ReleaseUnmanagedResources();
    if (!disposing) return;

    // TODO release managed resources here
    _memoryRead.Dispose();
    _memoryWrite.Dispose();
  }

}

//public MemoryNome(string nameMemory, ServerClient serverClient)
//{
//  NameMemory = nameMemory;
//  ServerClient = serverClient;

//  if (ServerClient == ServerClient.Server)
//  {
//    //      memoryRead = new MemoryBase(nameMemory + "Read", TypeBlockMemory.Read, callBackCommandControl);
//    _memoryRead = new MemoryBase(nameMemory + "Read", TypeBlockMemory.Read);
//    _memoryRead.InitializationCallBack(CallbackCommandDatAction);
//    _memoryWrite = new MemoryBase(nameMemory + "Write", TypeBlockMemory.Write);
//    //_setCommandControl = _memoryWrite.WriteInMemoryMd;
//    //_actionWriteByteData = _memoryWrite.WriteByteData;
//    //_actionWriteByteDataM = _memoryWrite.WriteByteData;
//    //_funcReadByteData = _memoryRead.ReadMemoryData;
//  }
//  else
//  {
//    _memoryRead = new MemoryBase(nameMemory + "Write", TypeBlockMemory.Read);
//    _memoryRead.InitializationCallBack(CallbackCommandDatAction);
//    _memoryWrite = new MemoryBase(nameMemory + "Read", TypeBlockMemory.Write);
//    //_setCommandControl = _memoryWrite.WriteInMemoryMd;
//    //_actionWriteByteData = _memoryWrite.WriteByteData;
//    //_actionWriteByteDataM = _memoryWrite.WriteByteData;
//    //_funcReadByteData = _memoryRead.ReadMemoryData;
//  }

//  _setCommandControl = _memoryWrite.WriteInMemoryMd;
//  _actionWriteByteData = _memoryWrite.WriteByteData;
//  _actionWriteByteDataM = _memoryWrite.WriteByteData;
//  _funcReadByteData = _memoryRead.ReadMemoryData;

//}

