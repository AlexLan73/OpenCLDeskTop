

namespace Common.Interfaces;
public interface IIdName
{
  [Reactive] public Guid Id { get; set; }
  public string Name { get; set; }
}
