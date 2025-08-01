namespace DMemory.Core;

public class ServerMetaData : BaseMetaData, IDisposable
{

  private readonly string _nameModule;
  private readonly string _clientName;

  public ServerMetaData(MetaSettings meta, MemoryDataProcessor processor)
    : base(meta, processor, "server", "client", meta.MetaEventClient, meta.MetaEventServer)
  {
    _clientName = "client" + meta.MemoryName;
  }
}

