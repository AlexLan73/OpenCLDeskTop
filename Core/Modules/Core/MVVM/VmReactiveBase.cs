using Common.Enum;

namespace Modules.Core.MVVM;
public abstract class VmReactiveBase : VmReactive
{
  // ReSharper disable four InconsistentNaming
  protected readonly IRegionManager IRegMan;
  protected readonly IContainerProvider Container;
  protected readonly IEventAggregator Ea;
  protected readonly IDataContext IdataContext;
  protected readonly IManagerLogger iLogger;
//  protected DbContext DbContext;

  protected VmReactiveBase(IContainerProvider container, IEventAggregator ea = null, IRegionManager region = null)
  {
    IRegMan = region;
    Container = container;
    Ea = ea;

    try
    {
      if (container != null)
        IdataContext = container.Resolve<IDataContext>();
      if (container != null) iLogger = container.Resolve<IManagerLogger>();
    }
    catch (Exception)
    {
      Container.Resolve<IManagerLogger>()
        .Send("VmReactiveBase", "Initialization container.Resolve<IDataContext>()", LoggerSendEnum.Warning);
    }

    try
    {
//      DbContext = Container.Resolve<DbContext>();
    }
    catch (Exception)
    {
//      throw;
    }
  }
}

