using MahApps.Metro.Controls;
using System.Windows;
using DeskTop.ViewModels;

namespace DeskTop.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class Shell : MetroWindow
{
  public Shell()
  {
    InitializeComponent();
    this.Loaded += (_, _) => { ((ShellViewModel)this.DataContext).LoadWin(this); };

  }
}

