namespace DMemory.Core;

// Наследник для ClientMetaData
public class ClientMetaData : BaseMetaData
{
  private readonly string _clientName;

  public ClientMetaData(MetaSettings meta, MemoryDataProcessor processor)
    : base(meta, processor, "client", "server",
      meta.MetaEventServer, meta.MetaEventClient)
  {
    _clientName = "server" + meta.MemoryName;
  }
}

