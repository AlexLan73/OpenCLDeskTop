using Common.Core.Converter;
using DMemory.Core.Channel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DMemory.Core.Converter;

public class LoggerChannelConverter : IChannelConverter
{
  public Type SourceType => typeof(LoggerChannel);
  public Type TargetType => typeof(LoggerBase);

  public object Convert(object channelObject)
  {
    var src = channelObject as LoggerChannel;
    if (src == null)
      throw new ArgumentNullException(nameof(channelObject), "Expected LoggerChannel");

    return new LoggerBase(src.Tik, src.Id, src.Module, src.Log, src.Code);
  }
}
// LoggerBase -> LoggerChannel
public class LoggerBaseToChannelConverter : IBaseToChannelConverter
{
  public Type SourceType => typeof(LoggerBase);
  public Type TargetType => typeof(LoggerChannel);

  /*
  public object Convert(object baseObj)
  {
    if (baseObj is LoggerBase src) return new LoggerChannel(src.Tik, src.Id, src.Module, src.Log, src.Code);
    else return null;
  }
*/
  public object Convert(object baseObj)=> (baseObj is LoggerBase src) 
                                               ? new LoggerChannel(src.Tik, src.Id, src.Module, src.Log, src.Code)
                                               :null;

}