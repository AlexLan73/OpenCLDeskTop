using Common;
using Common.Core;
using Data;
using Data.Core;
using DeskTop.Views;
using Logger;
using Logger.Models;
using Prism.Ioc;
using Prism.Modularity;
using System.Windows;

namespace DeskTop;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App
{
  private IRegionManager _iRegion;
  private readonly Dispatcher _dispatcher = Application.Current != null ? Application.Current.Dispatcher : Dispatcher.CurrentDispatcher;

  protected override Window CreateShell()
  {
    return Container.Resolve<Shell>();
  }

  protected override void OnStartup(StartupEventArgs e)
  {
    base.OnStartup(e);

    _iRegion = Container.Resolve<IRegionManager>();
    Container.Resolve<ILoggingInfos>();
    Container.Resolve<ILoggerServer>();
    Container.Resolve<IManagerLogger>().Send(new LoggerData("App", "Start"));
    //    Container.Resolve<JobCAN>();
    //    _ = new InitializationDb(Container);
    //    _iRegion.RegisterViewWithRegion(NameRegions.SetupsWindows, typeof(Setups));
  }
  protected override void OnExit(ExitEventArgs e)
  {
    Container.Resolve<IManagerLogger>().Send("App", "Stop");
    Container.Resolve<IDataContext>().Dispose();
    base.OnExit(e);
  }

  protected override void RegisterTypes(IContainerRegistry containerRegistry)
  {
    //    _ = containerRegistry.RegisterSingleton<DbContext>();
    _ = containerRegistry.RegisterSingleton<IDataContext, DataContext>();
    _ = containerRegistry.RegisterSingleton<ILoggingInfos, LoggingInfos>();
    _ = containerRegistry.RegisterSingleton<ILoggerServer, LoggerServer>();
    _ = containerRegistry.RegisterSingleton<IManagerLogger, ManagerLogger>();
    //    _ = containerRegistry.RegisterSingleton<JobCan>();
    _ = containerRegistry.RegisterSingleton<WinErrorViewModel>();


  }

  protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
  {
    moduleCatalog.AddModule<CommonModule>();
    moduleCatalog.AddModule<DataModule>();
    moduleCatalog.AddModule<LoggerModule>();
    moduleCatalog.AddModule<Driver.DriverModule>();
    moduleCatalog.AddModule<Modules.ModulesModule>();
    moduleCatalog.AddModule<WOpenCL.WOpenCLModule>();

  }

}

