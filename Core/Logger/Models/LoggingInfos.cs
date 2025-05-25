namespace Logger.Models;
public interface ILoggingInfo
{
  LoggerSendEnum LoginTypeError { get; set; }
  string SLoginTypeError { get; }
  string WhereError { get; set; }
  string Message { get; set; }
  string Print();
}

public class LoggingInfo : ILoggingInfo
{
  public LoggingInfo(ILoggingInfo source)
  {
    LoginTypeError = source.LoginTypeError;
    WhereError = source.WhereError;
    Message = source.Message;
  }
  public LoggingInfo()
  {
  }

  public LoggerSendEnum LoginTypeError { get; set; }
  public string WhereError { get; set; }
  public string Message { get; set; }
  public string SLoginTypeError => LoginTypeError.ToString();
  public string Print() => "" + LoginTypeError + "  " + WhereError + "  " + Message;
}
public interface ILoggingInfos
{
  SourceList<LoggingInfo> InfoLog { get; set; }
  void Add(LoggingInfo dan);
  void Delete(int id);
}
public class LoggingInfos : ILoggingInfos
{
  public SourceList<LoggingInfo> InfoLog { get; set; } = new();
  public void Add(LoggingInfo dan) => InfoLog.Add(new LoggingInfo(dan)
  {
    LoginTypeError = LoggerSendEnum.Info,
    WhereError = null,
    Message = null
  });
  public void Delete(int id) => InfoLog.RemoveAt(id);

}
