using Common.Core;
using Common.Core.Converter;
using DMemory.Core.Channel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DMemory.Core.Converter;

//VIdDataTimeVal
public class VDtValuesChannelConverter : IChannelConverter
{
  public Type SourceType => typeof(VDtValuesChannel);
  public Type TargetType => typeof(VIdDataTimeVal);

  public object Convert(object channelObject)
  {
    var src = channelObject as VDtValuesChannel;
    if (src == null)
      throw new ArgumentNullException(nameof(channelObject), "Expected VDtValuesChannel");

    // Конвертация массива DtValues[] -> DataTimeValRec[]
    DataTimeValRec[] variables = src.Values?.Select(v => new DataTimeValRec(v.Tik, v.Values)).ToArray() ?? [];

    return new VIdDataTimeVal(src.Id, variables);
  }
}

public class VDtValuesToChannelConverter : IBaseToChannelConverter
{
  public Type SourceType => typeof(VIdDataTimeVal);
  public Type TargetType => typeof(VDtValuesChannel);

  public object Convert(object baseObj)
  {
    var src = baseObj as VIdDataTimeVal;
    var arr = src.Variables.Select(v => new DtValues(v.Tik, v.Values)).ToArray();
    return new VDtValuesChannel(src.Id, arr);
  }
}




/*
  public record VDtValuesChannel(
     [property: Key(0)] uint Id,
     [property: Key(1)] DtValues[] Values
   );
 */

/*
VectorIdBase

public class DtVariableChannelConverter : IChannelConverter
   {
     public Type SourceType => typeof(DtVariableChannel);
     public Type TargetType => typeof(DTVariable);
   
     public object Convert(object channelObject)
     {
       var src = channelObject as DtVariableChannel;
       return new DTVariable(src.Id, src.Values.Tik, src.Values.Values);
     }
   }
 */