using DeskTop.Views;
using Prism.Mvvm;
using System;

namespace DeskTop.ViewModels;

public class ShellViewModel : BindableBase
{
    private Shell _shell;
    private string _title = "__ TEST OpenCL __";
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public ShellViewModel()
    {

    }

    internal void LoadWin(Shell shell)
    {
        _shell = shell;
    }
}

