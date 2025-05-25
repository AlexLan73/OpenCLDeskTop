
using Common.Enum;

namespace Modules.Core.MVVM;
public abstract class VmBase : Vm
{
  // ReSharper disable four InconsistentNaming
  protected readonly IRegionManager IRegMan;
  protected readonly IContainerProvider Container;
  protected readonly IEventAggregator Ea;
  protected readonly IDataContext IdataContext;
  protected readonly IManagerLogger iLogger;
//  protected Data.Core..DbContext DbContext;
  
  protected VmBase(IContainerProvider container, IEventAggregator ea = null, IRegionManager region = null)
  {
    IRegMan = region;
    Container = container;
    Ea = ea;

    try
    {
      IdataContext = container.Resolve<IDataContext>();
      iLogger = container.Resolve<IManagerLogger>();
    }
    catch
    {
      Container.Resolve<IManagerLogger>().Send("VmBase", "Initialization container.Resolve<IDataContext>()", LoggerSendEnum.Warning);
    }

    try
    {
//      DbContext = Container.Resolve<DbContext>();
    }
    catch (Exception)
    {
    }
  }
}

