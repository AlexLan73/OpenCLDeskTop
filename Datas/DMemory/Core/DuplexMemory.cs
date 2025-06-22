using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DMemory.Core;
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

public class DuplexMemory
{

  public string NameMemory { get; }
  public ServerClient ServerClient { get; }
  private readonly MemoryBase _memoryWrite;
  private readonly MemoryBase _memoryRead;
  private Action<string> setCommandControl=null;
  private Action<byte[]> actionWriteByteData = null;
  private Func<int, byte[]> funcReadByteData = null;


  public DuplexMemory(string nameMemory, ServerClient serverClient, Action<string> callBackCommandControl)
  {
    NameMemory = nameMemory;
    ServerClient = serverClient;
//    var callBackCommandControl1 = callBackCommandControl;

    if (ServerClient == ServerClient.Server)
    {
      _memoryRead = new MemoryBase(nameMemory + "Read", TypeBlockMemory.Read, callBackCommandControl);
      _memoryWrite = new MemoryBase(nameMemory + "Write", TypeBlockMemory.Write);
      setCommandControl = _memoryWrite.SetCommandControl;
      actionWriteByteData = _memoryWrite.WriteByteData;
      funcReadByteData = _memoryRead.ReadMemoryData;

    }
    else
    {
      _memoryRead = new MemoryBase(nameMemory + "Write", TypeBlockMemory.Read, callBackCommandControl);
      _memoryWrite = new MemoryBase(nameMemory + "Read", TypeBlockMemory.Write);
      setCommandControl = _memoryWrite.SetCommandControl;
      actionWriteByteData = _memoryWrite.WriteByteData;
      funcReadByteData = _memoryRead.ReadMemoryData;
    }

  }

  public void CommandControl(string command) => setCommandControl(command);
  public byte[] ReadMemoryData(int count) => funcReadByteData(count);
  public void WriteDataToMwmory(byte[] bytes) => actionWriteByteData(bytes);


}

