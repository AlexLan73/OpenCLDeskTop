using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Core.Channel;
public class ChannelBase
{
}

public enum LoggerSendEnumMemory
{
  Error = -1,
  Info = 0,
  Warning = 1
}


public record LoggerBase(uint Id, string Module, string Log, LoggerSendEnumMemory Code);
//public record IdDataTimeVal(uint Id, ulong Tic, double Variable);
public record IdDataTimeVal(uint Id, DataTimeValRec Variable);
public record VIdDataTimeVal(uint Id, DataTimeValRec[] Variables);

/////////////////////////////////
//public record VectorIdBase(uint Id, double[] Values );
//public record DateTimeVariableBase(uint Id, ulong Tic, float Variable);
//public record VectorDoubleBase(uint Id, double[] Values);
//public record MatrixBase(uint Id, uint I, uint J, double[,] Values);
//public record RecResultBase(uint Id,uint NFft, uint MChannel, double TimeFft, double TimeLoadData, double TimeWaiteData);
//public record DtRecordId(int Id, ulong Tic, RecResultBase[] DtRecord);

//public record TemperatureBase(
//  ulong Tic,
//  float Temp
//);

/*
 
[MessagePackObject]
   public record DtValues(
     [property: Key(0)] ulong Tik,
     [property: Key(1)] double Values
   );
   
   [MessagePackObject]
   public record LoggerChannel(
     [property: Key(0)] uint Id,
     [property: Key(1)] string Module,
     [property: Key(2)] string Log,
     [property: Key(3)] LoggerSendEnumMemory Code
   );
     [MessagePackObject]
     public record DtVariableChannel(
       [property: Key(0)] uint Id,
       [property: Key(1)] DtValues Values
     );
   
   [MessagePackObject]
   public record VDtValuesChannel(
     [property: Key(0)] uint Id,
     [property: Key(1)] DtValues[] Values
   );
    

 */