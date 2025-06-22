using Common.Core;
using DMemory.Constants;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MahApps.Metro.Controls;
using YamlDotNet.Serialization;

namespace DMemory.Core;
using MapCommands = Dictionary<string, string>;
public class MemoryNome
{
  private readonly DuplexMemory _memory;

  private readonly string _cudaTemperature = nameof(CudaTemperature).ToLower();
  private readonly string _arrCudaTemperature = nameof(CudaTemperature).ToLower() + "[]";

  public MemoryNome(string nameMemory, ServerClient serverClient)
  {
    _memory = new DuplexMemory(nameMemory, serverClient, CallbackCommandDatAction);
  }

  public virtual void CommandControlWrite(MapCommands map) => _memory.CommandControl(map);
  public virtual MapCommands ReadCommandControlWrite() => new MapCommands();

  protected virtual void CallbackCommandDatAction(RecDataMetaData dMetaData)
  {

  }
}


