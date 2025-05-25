


namespace Modules.Core;

public class ReadWriteFile
{
//  private readonly Regex _regex = new Regex(@"^(\r|\n|\t|\v|\s)*");
  private readonly Regex _regex = new Regex(@"\r|\n|\t|\v|\s");
  public IList<string> Read(string str) 
    => !File.Exists(str) 
        ? null 
        : File.ReadAllLines(str)
            .Select(x => _regex.Replace(x, ""))
            .ToList();
  
}

