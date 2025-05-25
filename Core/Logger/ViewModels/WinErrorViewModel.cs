using Logger.Models;
using System.Reactive;
using System.Windows.Forms;

namespace Logger.ViewModels;

public class SLoggerData(LoggerData ld)
{
  public string Module { get; set; } = ld.Module;
  public string Send { get; set; } = ld.Send;
  public LoggerSendEnum LoggerSendEnum { get; set; } = ld.LoggerSendEnum;
  public string SDateTime { get; set; } = ld.DateTime.ToString("yyyy-MM-dd HH:mm:ss");
}

public class WinErrorViewModel : ReactiveObject , IDisposable //, INavigationAware, IConfirmNavigationRequest
{
  private bool _disposed = false;

  #region __ Name __
  private string _name = "";
//  [Reactive]
  public string Name
  {
    get => _name;
    set => this.RaiseAndSetIfChanged(ref _name, value);
  }
  #endregion

  #region __ StatusStr __
  private string _statusStr = "";
  public string StatusStr
  {
    get => _statusStr;
    set => this.RaiseAndSetIfChanged(ref _statusStr, value);
  }
  #endregion

  private WinError _winError;
  private readonly ReadOnlyObservableCollection<SLoggerData> _lsError;
  public ReadOnlyObservableCollection<SLoggerData> LsError => _lsError;
  
  // ReSharper disable one NotAccessedField.Local
  private readonly IContainerProvider _container;
  private SourceCache<LoggerData, DateTime> LoggerData { get; set; } = new(t => t.DateTime);
  // ReSharper disable once NotAccessedField.Local
  private readonly IEventAggregator _ea;
  // ReSharper disable two PrivateFieldCanBeConvertedToLocalVariable
  private readonly IRegionManager _region;
  private readonly IManagerLogger _iLogger;
  public ReactiveCommand<string, Unit> CommandNavigate { get; }
  

  public WinErrorViewModel(IRegionManager regionManager, IContainerProvider container, IEventAggregator ea)
  {
    CommandNavigate = ReactiveCommand.Create<string>(execute: Navigate);
    _container = container;
    _region = regionManager;
    _ea = ea;
    _iLogger = container.Resolve<IManagerLogger>();
    //    var context = _container.Resolve<IDataContext>();
    StatusStr = "";
    LoggerData.Connect()
      .Transform(transformFactory: x=> new SLoggerData(ld: x))
      .Bind(readOnlyObservableCollection: out _lsError)
      .ObserveOnDispatcher(priority: DispatcherPriority.Normal)
      .ObserveOn(scheduler: RxApp.MainThreadScheduler)
      .Subscribe(onNext: _ => { });

    LoggerData.AddOrUpdate(items: _iLogger.DLoggerData.KeyValues.Select(selector: x=>x.Value));

    _ = _iLogger.DLoggerData.Connect()
      .ObserveOnDispatcher(priority: DispatcherPriority.Normal)
      .ObserveOn(scheduler: RxApp.MainThreadScheduler)
      .Subscribe(onNext: t =>
      {
        
        foreach (var it in t.ToArray())
        {
          switch (it.Reason)
          {
            case ChangeReason.Add:
            case ChangeReason.Update:
            case ChangeReason.Refresh:
            case ChangeReason.Moved:
              LoggerData.AddOrUpdate(item: it.Current);
              break;

            case ChangeReason.Remove:
              var itCurrent = it.Current;
              var firstOrDefault = LoggerData.Items.FirstOrDefault(predicate: x=>(x.Module + x.Send + x.LoggerSendEnum)==
                                                                                 (itCurrent.Module + itCurrent.Send + itCurrent.LoggerSendEnum));
              LoggerData.Remove(item: firstOrDefault);
              break;
            default:
              throw new ArgumentException($"Значение не входит в нужный деапазоп в WinErrorViewModel");
          }
          StatusStr = $" Кол-во записей: {LsError.Count};  Всего записей {_iLogger.DLoggerData.Count}";
        }
      });


    try
    {
      // ReSharper disable two UnusedVariable
      var count = _region.Regions.Count();
      var region = _region.Regions[regionName: NameRegions.WinError];
      
      
    }
    catch (Exception)
    {
      // ignored
    }


//    var guidLoad = Container.Resolve<Func<Guid, WBaseA2L>>();
//    _wBaseA2L = guidLoad(Id);
//    region.Add(_wBaseA2L, SWBaseA2L);
//    var parameters = new NavigationParameters();
//    parameters.Add("id", Id);
//    parameters.Add("NameRegion", SId);
//    IRegMan.RequestNavigate(SId, SWBaseA2L, parameters);

    Name = "WinErrorViewModel";
     
  }


  private void Navigate(string command)
  {
    switch (command)
    {
      case "ScrollIntoViewBehavior":
        break;
    }
  }

/*
  public void Send(string module, string send, LoggerSendEnum lte = LoggerSendEnum.Info)
  {
    
  }
*/
  public void LoadWim(WinError winError)
  {
    this._winError= winError;
    this._winError.DgError.LoadingRow += (_, e) =>
    {
      this._winError.DgError.ScrollIntoView(e.Row.Item);
    };
  }


/*
  public void Dispose()
  {
    try
    {
      // Dispose managed resources if needed
      // _wAuthor?.Dispose();
      // _loadYamlData?.Dispose();

      // Remove region
      _region.Regions.Remove(nameof(WinError));
    }
    catch (Exception)
    {
      // ignored
    }

    // Suppress finalization to optimize GC if finalizer exists
    GC.SuppressFinalize(this);
  }
  */


  ~WinErrorViewModel()
  {
    Dispose(false);
  }

  public void Dispose()
  {
    Dispose(true);
    GC.SuppressFinalize(this);
  }

  protected virtual void Dispose(bool disposing)
  {
    if (_disposed) return;

    if (disposing)
    {
      // Dispose managed resources
      try
      {
        _region.Regions.Remove(nameof(WinError));
      }
      catch
      {
        // ignored
      }
    }

    // Free unmanaged resources here if any

    _disposed = true;
  }




  /*
    public void Dispose()
    {

      try
      {
  //      _wAuthor.Dispose();
  //      _loadYamlData.Dispose();
  // Уточнить регион
        _region.Regions.Remove(nameof(WinError));
      }
      catch (Exception)
      { // ignored
      }

    }
  */
}


