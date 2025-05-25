namespace Modules.Core.MVVM;
public abstract class Vm : BindableBase, IDestructible, IDisposable
{
  #region ----  Name  ----
  private string _name = "";
  public string Name
  {
    get => _name;
    set => SetProperty(ref _name, value);
  }
  #endregion
  #region ----  Title  ----
  private string _title = "";
  public string Title
  {
    get => _title;
    set => SetProperty(ref _title, value);
  }
  #endregion
  public void Destroy()  // для удаления объектов из MVVM 
  { }
  protected virtual void Dispose(bool disposing)
  {
    if (disposing)
    {
    }
  }
  public virtual void Dispose()
  {
    Dispose(true);
    GC.SuppressFinalize(this);
  }
}

