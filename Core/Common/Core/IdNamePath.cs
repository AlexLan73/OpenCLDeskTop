namespace Common.Core;
public class IdNamePath: IdName 
{
  public IdNamePath(){}

  public IdNamePath(string path, Guid id)
  {
    Path = path;
    Id = id;
    Name = Directory.Exists(path) ? path.Split(@"\")[^1] : "";
  }
  public string Path { get; set; }

}
