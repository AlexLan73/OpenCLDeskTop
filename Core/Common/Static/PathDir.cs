
namespace Common.Static;

public static class PathDir
{
  private static readonly string DirUser = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
  private static string DirBase => DirUser+"\\"+PathConstant.Stand0;
  public static string Config => DirBase+"\\" + "Config";
  private static string Tmp => DirBase + "\\" + "TMP";
  public static string Logger => DirBase + "\\" + "Logger";
  private static readonly string  PathReadConfig = PathDir.Config + "\\Config.yaml";

  public static void SetPath()
  {

    if (!Directory.Exists(DirBase))
      Directory.CreateDirectory(DirBase);

    if (!Directory.Exists(Config))
      Directory.CreateDirectory(Config);

    if (!Directory.Exists(Tmp))
      Directory.CreateDirectory(Tmp);

    if (!Directory.Exists(Logger))
      Directory.CreateDirectory(Logger);
  }
}

public static class AllPathFileName
{
  public static string ActiveFileA2LHex => PathDir.Config + "\\"+ FileNameConstant.ActiveA2LHex;
  public static string SetPathLog(string path) => PathLoggerFile = PathDir.Logger+ $"\\{path}.log";
  public static string PathLoggerFile { get; set; }
}


