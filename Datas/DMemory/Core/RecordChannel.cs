namespace DMemory.Core;
// <summary>
/// Статический класс для хранения уникальных идентификаторов типов данных.
/// </summary>
public static class DataTypeIds
{
  public const uint Logger = 0;
  public const uint CudaDateTimeVariable = 1;
  public const uint CudaVector = 2;
  public const uint CudaMatrix = 3;
  public const uint RecResult = 4; // Этот тип используется внутри CudaDtRecord
  public const uint CudaDtRecord = 5;
}

public enum LoggerSendEnumMemory
{
  Error = -1,
  Info = 0,
  Warning = 1
}

// == 0 ==
[MessagePackObject]
public record Logger(
  [property: Key(0)] uint Id,
  [property: Key(1)] string Module,
  [property: Key(2)] string Log,
  [property: Key(3)] LoggerSendEnumMemory Code

);

// == 1 ==
[MessagePackObject]
public record CudaDateTimeVariable(
  [property: Key(0)] uint Id,
  [property: Key(1)] DateTime DateTime, // формат 2025.06.24 20:23:15.3423..
  [property: Key(2)] float Variable
  );

// == 2 ==
[MessagePackObject]
public record CudaVector(
  [property: Key(0)] uint Id,
  [property: Key(1)] double[] Values
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
