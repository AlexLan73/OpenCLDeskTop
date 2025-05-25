namespace Common.Core;
public class IsControlParameter : ReactiveObject, IIdName, IComparable<IsControlParameter>
{
  public Guid Id { get; set; }
  public string Name { get; set; }
  [Reactive] public bool IsAction { get; set; } = false;
  [Reactive] public int NumParameter { get; set; } = 0;
  [Reactive] public bool? IsSelectSection { get; set; } = false;
  [Reactive] public bool? IsOpenCloseSelect { get; set; } = false;
  public string SingColor => NumParameter switch {
      -3 => "Red",
      -2 => "Violet",
      -1 => "Coral",
      0 => "White",
      1 => "Yellow",
      2 => "Green",
      3 => "Blue",
      4 => "Gray",
      _ => "White"
    };

public int CompareTo(IsControlParameter other) => string.CompareOrdinal(Name, other.Name);
}