namespace DMemory.Core.Copy;
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

public class MemoryNomeNew:IDisposable
{
  public string NameMemory { get; }
  private readonly string _serverName="";
  public readonly string NameModule;
  public ServerClient ServerClient { get; }
  private  Action<MapCommands> _setCommandControl;
  private readonly Action<byte[]> _actionWriteByteData;
  private  Action<byte[], MapCommands> _actionWriteByteDataM;
  private readonly Func<int, byte[]> _funcReadByteData;
  private readonly MemoryBaseNew _memoryRead;
  private readonly MemoryBaseNew _memoryWrite;
  private MapCommands _dMD = new();

  //  public MemoryNome(string nameMemory, ServerClient serverClient, Action<MapControl> callBackCommandControl)
  public MemoryNomeNew(string nameMemory, ServerClient serverClient)
  {
    NameModule = nameMemory;
    NameMemory = nameMemory;
    ServerClient = serverClient;

    if (ServerClient == ServerClient.Server)
    {
//      memoryRead = new MemoryBaseNew(nameMemory + "Read", TypeBlockMemory.Read, callBackCommandControl);
      _memoryRead = new MemoryBaseNew(nameMemory + "Read", TypeBlockMemory.Read);
      _memoryRead.InitializationCallBack(CallbackCommandDatAction);
      _memoryWrite = new MemoryBaseNew(nameMemory + "Write", TypeBlockMemory.Write);
      _serverName = "server" + NameModule;
      if (!_dMD.TryAdd("state", _serverName))
        _dMD.Add("state", _serverName);
    }
    else
    {
      _memoryRead = new MemoryBaseNew(nameMemory + "Write", TypeBlockMemory.Read);
      _memoryRead.InitializationCallBack(CallbackCommandDatAction);
      _memoryWrite = new MemoryBaseNew(nameMemory + "Read", TypeBlockMemory.Write);
    }

    _setCommandControl = _memoryWrite.SetCommandControl;
    _actionWriteByteData = _memoryWrite.WriteByteData;
    _actionWriteByteDataM = _memoryWrite.WriteByteData;
    _funcReadByteData = _memoryRead.ReadMemoryData;


  }

  private void Initilization_MD()
  {

  }
  public void CommandControlWrite(MapCommands command) => _setCommandControl(command);
  public byte[] ReadMemoryData(int count) => _funcReadByteData(count);
  public void WriteDataToMemory(byte[] bytes) => _actionWriteByteData(bytes);
  public void WriteDataToMemory(byte[] bytes, MapCommands map) => _actionWriteByteDataM(bytes, map);
  public DateTime ParseCudaDate(string dateString)
  {
    // "format" должен быть определен в вашем классе
    const string format = "yyyy.MM.dd HH:mm:ss.fff";
    return DateTime.ParseExact(dateString, format, CultureInfo.InvariantCulture);
  }

  public virtual MapCommands ReadCommandControlWrite() => new MapCommands();

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

  public void Dispose()
  {
    Dispose(true);
    GC.SuppressFinalize(this);
  }

  ~MemoryNomeNew()
  {
    Dispose(false);
  }
}

