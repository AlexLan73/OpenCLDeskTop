using System.Collections.Generic;
using System.Globalization;

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


  //  public MemoryNome(string nameMemory, ServerClient serverClient, Action<RecDataMetaData> callBackCommandControl)
  public MemoryNome(string nameMemory, ServerClient serverClient)
  {
    NameMemory = nameMemory;
    ServerClient = serverClient;

    if (ServerClient == ServerClient.Server)
    {
//      memoryRead = new MemoryBase(nameMemory + "Read", TypeBlockMemory.Read, callBackCommandControl);
      _memoryRead = new MemoryBase(nameMemory + "Read", TypeBlockMemory.Read);
      _memoryRead.InitializationCallBack(CallbackCommandDatAction);
      _memoryWrite = new MemoryBase(nameMemory + "Write", TypeBlockMemory.Write);
      //_setCommandControl = _memoryWrite.SetCommandControl;
      //_actionWriteByteData = _memoryWrite.WriteByteData;
      //_actionWriteByteDataM = _memoryWrite.WriteByteData;
      //_funcReadByteData = _memoryRead.ReadMemoryData;
    }
    else
    {
      _memoryRead = new MemoryBase(nameMemory + "Write", TypeBlockMemory.Read);
      _memoryRead.InitializationCallBack(CallbackCommandDatAction);
      _memoryWrite = new MemoryBase(nameMemory + "Read", TypeBlockMemory.Write);
      //_setCommandControl = _memoryWrite.SetCommandControl;
      //_actionWriteByteData = _memoryWrite.WriteByteData;
      //_actionWriteByteDataM = _memoryWrite.WriteByteData;
      //_funcReadByteData = _memoryRead.ReadMemoryData;
    }

    _setCommandControl = _memoryWrite.SetCommandControl;
    _actionWriteByteData = _memoryWrite.WriteByteData;
    _actionWriteByteDataM = _memoryWrite.WriteByteData;
    _funcReadByteData = _memoryRead.ReadMemoryData;

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

  ~MemoryNome()
  {
    Dispose(false);
  }
}

