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
public record DTVariableBase(uint Id, ulong Tic, double Variable);
public record VectorIdBase(uint Id, double[] Values );
public record DateTimeVariableBase(uint Id, ulong Tic, float Variable);
public record VectorDoubleBase(uint Id, double[] Values);
public record MatrixBase(uint Id, uint I, uint J, double[,] Values);
public record RecResultBase(uint Id,uint NFft, uint MChannel, double TimeFft, double TimeLoadData, double TimeWaiteData);
public record DtRecordId(int Id, ulong Tic, RecResultBase[] DtRecord);

public record TemperatureBase(
  ulong Tic,
  float Temp
);
