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

public record CudaDateTimeVariable(
  [property: Key(0)] int Id,
  [property: Key(1)] DateTime DateTime, // формат 2025.06.24 20:23:15.3423..
  [property: Key(2)] float Variable
  );

