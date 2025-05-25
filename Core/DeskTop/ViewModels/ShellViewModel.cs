using DeskTop.Views;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using System;
using DryIoc;
using Modules.Core.MVVM;

namespace DeskTop.ViewModels;

public class ShellViewModel : VmReactiveNavigate
{
    #region _  __DATA__  _
    private Shell _shell;
    private WinError _winError;
//    private WInfoTable _wInfoTable;
//    private WCan _wCan;
    private readonly Dispatcher _dispatcher = Application.Current != null ? Application.Current.Dispatcher : Dispatcher.CurrentDispatcher;
    public ReactiveCommand<string, Unit> CommandNavigate { get; }
    #region   ____ Правый флэш ____
    #region ----  IsSetups  ----
    private bool _isSetups;
    public bool IsSetups
    {
        get => _isSetups;
        set => this.RaiseAndSetIfChanged(ref _isSetups, value);
    }
    #endregion
    #endregion
    #region   ____ Статус бар ____
    #region ----  DockStatusBar  ----
    private string _dockStatusBar;
    public string DockStatusBar
    {
        get => _dockStatusBar;
        set => this.RaiseAndSetIfChanged(ref _dockStatusBar, value);
    }
    #endregion
    #endregion
    #region _____ Левая флэш
    #region ----  IsGoto0  ----
    private bool _isGoto0;
    public bool IsGoto0
    {
        get => _isGoto0;
        set => this.RaiseAndSetIfChanged(ref _isGoto0, value);
    }
    #endregion

    #region ----  IsGoto1  ----
    private bool _isGoto1;
    public bool IsGoto1
    {
        get => _isGoto1;
        set => this.RaiseAndSetIfChanged(ref _isGoto1, value);
    }
    #endregion
    #region ----  IsGoto2  ----
    private bool _isGoto2;
    public bool IsGoto2
    {
        get => _isGoto2;
        set => this.RaiseAndSetIfChanged(ref _isGoto2, value);
    }
    #endregion

    #endregion
    #endregion
    public ShellViewModel(IRegionManager regionManager, IContainerProvider containerProvider, IEventAggregator ea)
      : base(containerProvider, ea, regionManager)
    {
        CommandNavigate = ReactiveCommand.Create<string>(Navigate);
        Title = "== НАМИ == ";
    }

    internal void LoadWin(Shell shell)
    {
        _shell = shell;
        _shell.Closing += (_, _) =>
        {
            _dispatcher.Invoke(DispatcherPriority.Normal, () =>
            {
                //if (_wCan != null)
                //    try { _wCan.Close(); }
                //    catch (Exception)
                //    {
                //        // ignored
                //    }
                //if (_winError != null)
                //    try { _winError.Close(); }
                //    catch (Exception)
                //    {
                //        // ignored
                //    }

                //if (_wInfoTable == null) return;
                //try { _wInfoTable.Close(); }
                //catch (Exception)
                //{
                //    // ignored
                //}
            });
        };
    }
    public void Navigate(string command)
    {
        switch (command)
        {
            case "DelAll":
                break;

            case "setup":
                IsSetups = true;
                break;
            case "goto0":
                IsGoto0 = true;
                break;

            case "goto1":
                IsGoto1 = true;
                break;

            case "goto2":
                IsGoto2 = true;
                break;

            case "Journal":
                //if (_winError == null)
                //{
                //    _winError = Container.Resolve<WinError>();
                //    RegionManager.SetRegionManager(_winError, IRegMan);
                //    _winError.Closing += (_, _) => { _winError = null; };
                //    _winError.Show();
                //}
                break;

            //case nameof(WInfoTable):
            //    if (_wInfoTable == null)
            //    {
            //        _wInfoTable = Container.Resolve<WInfoTable>();
            //        RegionManager.SetRegionManager(_wInfoTable, IRegMan);
            //        _wInfoTable.Closing += (_, _) => { _wInfoTable = null; };
            //        _wInfoTable.Show();
            //        IsGoto0 = false;
            //    }
            //    break;

            //case nameof(WCan):
            //    if (_wCan == null)
            //    {
            //        _wCan = Container.Resolve<WCan>();
            //        RegionManager.SetRegionManager(_wCan, IRegMan);
            //        _wCan.Closing += (_, _) => { _wCan = null; };
            //        _wCan.Show();
            //        IsGoto0 = false;
            //    }
            //    break;
        }
    }
}

