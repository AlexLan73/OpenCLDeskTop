using Common.Core.Converter;
using DMemory.Core.Channel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DMemory.Core.Converter;

public class DtValuesChannelConverter : IChannelConverter
{
  public Type SourceType => typeof(DtValuesChannel);
  public Type TargetType => typeof(DataTimeVariable);

  public object Convert(object channelObject)
  {
    var src = channelObject as DtValuesChannel;
    return new DataTimeVariable(src.Values.Tik, src.Values.Values);
  }
}

/*
public class DtValuesChannelConverter : IChannelConverter
{
  public Type SourceType => typeof(DtValuesChannel);
  public Type TargetType => typeof(Common.Core.Channel.IdDataTimeVal);

  public object Convert(object channelObject)
  {
    var src = channelObject as DtValuesChannel;
    return new Common.Core.Channel.IdDataTimeVal(src.Id, new DataTimeValRec(src.Values.Tik, src.Values.Values));
  }
}
*/

public class DtVariableToChannelConverter : IBaseToChannelConverter
{
  public Type SourceType => typeof(IdDataTimeVal);
  public Type TargetType => typeof(DtValuesChannel);

  public object Convert(object baseObj)
  {
    var src = baseObj as IdDataTimeVal;
    if (src == null) return null;

    // Преобразуем массив Variables: DataTimeValRec[] -> DtValues[]
        var arr =  src.Variable;
        return new DtValuesChannel(src.Id,  new DtValues(arr.Tik, arr.Values) );
  }
}


