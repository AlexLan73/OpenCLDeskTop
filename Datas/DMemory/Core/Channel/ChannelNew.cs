using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Core.Channel;

namespace DMemory.Core.Channel;

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



/*
 
   
   // == 2 ==
   [MessagePackObject]
   public record VectorId(
     [property: Key(0)] uint Id,
     [property: Key(1)] double[] Values
   );
   
   
   
   // == 1 ==
   [MessagePackObject]
   public record DateTimeVariable(
     [property: Key(0)] uint Id,
     [property: Key(1)] DateTime DateTime, // формат 2025.06.24 20:23:15.3423..
     [property: Key(2)] float Variable
     );
   
   
   // Важно: MessagePack сериализует двумерный массив double[,] как вложенный массив.
   // На стороне C++ мы будем обрабатывать это как плоский вектор с размерами I и J.
   
   // == 3 ==
   [MessagePackObject]
   public record CudaMatrix(
     [property: Key(0)] uint Id,
     [property: Key(1)] uint I,
     [property: Key(2)] uint J,
     [property: Key(3)] double[,] Values
   );
   [MessagePackObject]
   
   // == 4 ==
   public record RecResult(
     [property: Key(0)] uint Id,
     [property: Key(1)] uint NFft,
     [property: Key(2)] uint MChannel,
     [property: Key(3)] double TimeFft,
     [property: Key(4)] double TimeLoadData,
     [property: Key(5)] double TimeWaiteData
   );
   
   // == 5 ==
   [MessagePackObject]
   public record CudaDtRecord(
     [property: Key(0)] int Id,
     [property: Key(1)] DateTime DateTime, // формат 2025.06.24 20:23:15.3423..
     [property: Key(2)] RecResult[] DtRecord
   );
   
   
   [MessagePackObject]
   public record CudaTemperature(
     [property: Key(0)] string Dt,
     [property: Key(1)] float Temp
   );
    

 */