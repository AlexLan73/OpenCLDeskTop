using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MessagePack;

namespace Common.Core.Property;

[MessagePackObject]

public record CudaTemperature(
  [property: Key(0)] string Dt,
  [property: Key(1)] float Temp
);

public record CudaDtTemperature(DateTime Dt, float Temp);
