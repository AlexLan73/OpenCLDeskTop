

namespace Modules.Static;
public static class ManagerWindows
{
  public static ConcurrentDictionary<string, object> ActionWindows { get; private set; } = new ();
  public static void AddMyWindows(string name, object value)
  {
    if (ActionWindows.ContainsKey(name))
      ActionWindows[name] = value;
    else 
      ActionWindows.AddOrUpdate(name, value, (_, _) => value);
  }
  public static void RemoveMyWindows(string name) => ActionWindows.TryRemove(name, out _);
  
  public static void CloseAllWin()
  {
    foreach (var key in ActionWindows.Keys.ToArray())
      ((IClose)ActionWindows[key])?.Close();
  }
}
