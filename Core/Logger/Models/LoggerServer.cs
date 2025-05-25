namespace Logger.Models;

public sealed class LoggerServer : ILoggerServer
{
  private readonly ILogger<string> _makeLoginInfo;
  private bool _disposedValue;
  public LoggerServer(ILoggingInfos logging)
  {
    logging.InfoLog = new SourceList<LoggingInfo>();

    var loggerFactory = LoggerFactory.Create(builder =>
    {
      _ = builder.ClearProviders();
      _ = builder.AddDebug();                                     //_ = builder.SetMinimumLevel(LogLevel.Error);
      var dt = DateTime.Now.ToString("yyyy-MM-dd_H-mm-ss");
      var pathLog = AllPathFileName.SetPathLog(dt);         // var pathLog = AllPathFileName.SetPathLog(dt) StaticConfig.GetPathLog() + "\\" + $"Log_{dt}.log";
      var loggerConfiguration = new LoggerConfiguration()
          .WriteTo.File(pathLog);  // .MinimumLevel.Error();
                                   // .WriteTo.File(pathLog, rollingInterval: RollingInterval.Minute);  //.MinimumLevel.Error();
      _ = builder.AddSerilog(loggerConfiguration.CreateLogger());
    });
    _makeLoginInfo = loggerFactory.CreateLogger<string>();
  }

  public void Send(LoggerData ld) => Send(ld.Module, ld.Send, ld.LoggerSendEnum);
  public void Send(string module, string send = "", LoggerSendEnum lte = LoggerSendEnum.Info)
  {
    switch (lte)
    {
      case LoggerSendEnum.Info:
        _makeLoginInfo.LogInformation("{Module} | {Send}", module, send);
        break;

      case LoggerSendEnum.Warning:
        _makeLoginInfo.LogWarning("{Module} | {Send}", module, send);
        break;

      case LoggerSendEnum.Error:
        _makeLoginInfo.LogError("{Module} | {Send}", module, send);
        break;

      default:
        throw new ArgumentOutOfRangeException(nameof(lte), lte, null);
    }
  }


  /*
    public void Send(string module, string send = "", LoggerSendEnum lte = LoggerSendEnum.Info)
     {
       var info = module + " | " + send;
       switch (lte)
       {
         case LoggerSendEnum.Info:
           _makeLoginInfo.LogInformation(info);
           break;

         case LoggerSendEnum.Warning:
           _makeLoginInfo.LogWarning(info);
           break;

         case LoggerSendEnum.Error:
           _makeLoginInfo.LogError(info);
           break;

         default:
           throw new ArgumentOutOfRangeException(nameof(lte), lte, null);
       }
     }

  */
  private void Dispose(bool disposing)
  {
    if (!_disposedValue)
    {
      if (disposing)
      {
        // TODO: освободить управляемое состояние (управляемые объекты)
      }

      // TODO: освободить неуправляемые ресурсы (неуправляемые объекты) и переопределить метод завершения
      // TODO: установить значение NULL для больших полей
      _disposedValue = true;
    }
  }

  // // TODO: переопределить метод завершения, только если "Dispose(bool disposing)" содержит код для освобождения неуправляемых ресурсов
  // ~LoggerServer()
  // {
  //     // Не изменяйте этот код. Разместите код очистки в методе "Dispose(bool disposing)".
  //     Dispose(disposing: false);
  // }

  public void Dispose()
  {
    // Не изменяйте этот код. Разместите код очистки в методе "Dispose(bool disposing)".
    Dispose(disposing: true);
    // ReSharper disable once GCSuppressFinalizeForTypeWithoutDestructor
    GC.SuppressFinalize(this);
  }

  //public void Send(string module, string send, LoggerSendEnum lte = LoggerSendEnum.Info)
  //{
  //  throw new NotImplementedException();
  //}

  //public void Send(LoggerData ld)
  //{
  //  throw new NotImplementedException();
  //}
}
 

