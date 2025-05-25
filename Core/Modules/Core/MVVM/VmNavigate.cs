namespace Modules.Core.MVVM;
public class VmNavigate(IContainerProvider container,
                        IEventAggregator ea = null,
                        IRegionManager region = null)
  // ReSharper disable once RedundantExtendsListEntry
  : VmBase(container, ea, region), INavigationAware, IConfirmNavigationRequest
{
  public virtual void OnNavigatedTo(NavigationContext navigationContext) { }
  public virtual bool IsNavigationTarget(NavigationContext navigationContext) => true;
  public virtual void OnNavigatedFrom(NavigationContext navigationContext) { }
  public virtual void ConfirmNavigationRequest(NavigationContext navigationContext, Action<bool> continuationCallback) =>
    continuationCallback(true);

}

