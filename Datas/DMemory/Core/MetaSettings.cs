namespace DMemory.Core;

public class MetaSettings
{
  public string MemoryName { get; }
  public int MetaSize { get; }

  public string MetaEventServer => $"Global\\EventServer{MemoryName}";
  public string MetaEventClient => $"Global\\EventClient{MemoryName}";
  public string ControlName => $"{MemoryName}Control";

  public MetaSettings(string name, int metaSize = 8192)
  {
    MemoryName = name ?? throw new ArgumentNullException(nameof(name));
    MetaSize = metaSize > 0 ? metaSize : throw new ArgumentOutOfRangeException(nameof(metaSize));
  }
}

