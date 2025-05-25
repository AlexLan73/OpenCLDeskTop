using System.Threading.Tasks;
using ReactiveUI.Fody.Helpers;

namespace Logger.Models;

public interface IManagerLogger : ILoggerServer
{
  SourceCache<LoggerData, DateTime> DLoggerData { get; set; }
  new void Send(string module, string send, LoggerSendEnum lte = LoggerSendEnum.Info);
  new void Send(LoggerData ld);

}

/// <summary>
/// Class для маршрутизации сообщений
///  в текмтовый файл и в Windows
/// </summary>
public class ManagerLogger : ReactiveObject, IManagerLogger, IDisposable
{
  private bool _disposed = false;
  public SourceCache<LoggerData, DateTime> DLoggerData { get; set; } = new(t => t.DateTime);
  private bool _isRepeat = true;
  public ConcurrentQueue<LoggerData> LoggerData { get; set; } = new();
  [Reactive] public byte CountByte { get; set; }
  //  public byte CountByte { get; set; }

  public ManagerLogger(IContainerProvider container, IEventAggregator ea)
  {
    var server = container.Resolve<ILoggerServer>();
    CountByte = 0;
    ea.GetEvent<EventLogger>().Subscribe(FuncEventLogger, ThreadOption.UIThread);
    //    Application.Current.Dispatcher
    //    Task.Delay(1000).Wait();

    _ = this.WhenAnyValue(x => x.CountByte)
      .ObserveOnDispatcher(DispatcherPriority.Background)
      .ObserveOn(RxApp.TaskpoolScheduler)
      .Subscribe(_ =>
      {
        if (_isRepeat)
        {
          Task.Run(() =>
          {
            _isRepeat = false;
//            while (LoggerData.Count > 0)
            while (!LoggerData.IsEmpty)
            {
                var b = LoggerData.TryDequeue(out var ld);
              if (!b) continue;
              server.Send(ld);
              DLoggerData.AddOrUpdate(ld);
            }
            _isRepeat = true;
          });
        }
      });

  }

  private void FuncEventLogger(LoggerData data)
  {
    LoggerData.Enqueue(data);
    CountByte++;
  }

  public void Send(string module, string send, LoggerSendEnum lte = LoggerSendEnum.Info)
  {
    LoggerData.Enqueue(new LoggerData(module, send, lte));
    CountByte++;
  }

  public void Send(LoggerData ld)
  {
    LoggerData.Enqueue(ld);
    CountByte++;
  }

  ~ManagerLogger()
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
      // Dispose managed resources here
    }

    // Free unmanaged resources here (if any)

    _disposed = true;
  }
}


