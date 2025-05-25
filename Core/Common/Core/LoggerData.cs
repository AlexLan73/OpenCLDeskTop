namespace Common.Core;
public class LoggerData(string module, string send, LoggerSendEnum loggerSendEnum = LoggerSendEnum.Info):ReactiveObject
{
  public string Module { get; set; } = module;
  public string Send { get; set; } = send;
  public LoggerSendEnum LoggerSendEnum { get; set; } = loggerSendEnum;
  public DateTime DateTime = DateTime.Now;
  
  [Reactive] public bool Is { get; set; } = false;
}




