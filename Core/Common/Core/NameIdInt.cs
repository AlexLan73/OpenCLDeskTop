namespace Common.Core;
public class NameIdInt : INameIdInt
{
  public NameIdInt()
  {
  }
  public NameIdInt(int id, string name)
  {
    Id = id;
    Name = name;
  }
  public int Id { get; set; }
  public string Name { get; set; }
}
