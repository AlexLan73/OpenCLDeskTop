using DMemory.Enums;

namespace DMemory.Core;

public sealed class AppGlobals
{
  private static readonly AppGlobals _instance = new AppGlobals();
  public static AppGlobals Instance => _instance;

  public string ModuleName { get; set; }
  public int GlobalCounter { get; set; }
  public SateMode SateMode { get; set; }
  public System.Collections.Concurrent.ConcurrentDictionary<string, string> MdConfig { get; set; } = new();

  private AppGlobals() { }
}

