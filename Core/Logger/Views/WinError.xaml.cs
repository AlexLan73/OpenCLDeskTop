using MahApps.Metro.Controls;

namespace Logger.Views;
// ReSharper disable once RedundantExtendsListEntry
public partial class WinError : MetroWindow
{
  public WinError()
  {
    InitializeComponent();
    this.Loaded += (_, _) =>  {((WinErrorViewModel)(this.DataContext)).LoadWim(this);};
    this.Closing += (_, _) => { ((WinErrorViewModel)(this.DataContext)).Dispose(); };
  }
}

