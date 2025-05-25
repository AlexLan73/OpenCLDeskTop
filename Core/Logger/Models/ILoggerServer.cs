namespace Logger.Models;

public interface ILoggerServer : IDisposable
{
  void Send(string module, string send, LoggerSendEnum lte = LoggerSendEnum.Info);
  void Send(LoggerData ld);
}