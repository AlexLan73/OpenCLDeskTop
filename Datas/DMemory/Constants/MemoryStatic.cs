namespace DMemory.Constants;

public static class MemStatic
{
  public static string StCudaTemperature = nameof(CudaTemperature).ToLower();
  public static string StArrCudaTemperature = nameof(CudaTemperature).ToLower() + "[]";
  public const int SizeDataControl = 1024 * 8;
  public const int SizeDataSegment = 64 * 1024; // 64 KB
  public static readonly byte[] EmptyBuffer = new byte[SizeDataControl];
}

