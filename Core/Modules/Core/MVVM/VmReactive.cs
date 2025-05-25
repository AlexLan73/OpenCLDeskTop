using ReactiveUI.Fody.Helpers;

namespace Modules.Core.MVVM;
public abstract class VmReactive : ReactiveObject, IDisposable
{
  #region __ Name __
  private string _name = "";
  [Reactive]
  public string Name
  {
    get => _name;
    set => this.RaiseAndSetIfChanged(ref _name, value);
  }
  #endregion
  #region ----  Title  ----
  private string _title = "";
  public string Title
  {
    get => _title;
    set => this.RaiseAndSetIfChanged(ref _title, value);
  }
  #endregion
  protected virtual void Dispose(bool disposing)
  {
    if (disposing) { }
  }
  public virtual void Dispose()
  {
    Dispose(true);
    GC.SuppressFinalize(this);
  }
}

