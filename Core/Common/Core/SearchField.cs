namespace Common.Core;
public record SearchField(string Name, string NameClass)
{
  public string NameFull => Name + "." + NameClass;
}