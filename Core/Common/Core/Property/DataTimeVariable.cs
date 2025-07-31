namespace Common.Core.Property;

public record DataTimeValRec(long Tik, double Values);
public interface IDataTimeVariable
{
  long Tik { get; set; }
  double Variable { get; set; }
}

public class DataTimeVariable //: IDataTimeTik
{
  public long Tik { get; set; } = 0;
  public double Variable { get; set; } = 0d;

  public DataTimeVariable(long tik, double variable)
  {
    Tik = tik;
    Variable = variable;
  }

  public DataTimeVariable(IDataTimeVariable source)
  {
    Tik = source.Tik;
    Variable = source.Variable;
  }

  public string GetDateTime() => DateTimeOffset.FromUnixTimeMilliseconds((long)Tik).ToString("yyyy.MM.dd HH:mm:ss,fffff");
  //{
  //DateTimeOffset dateTime = DateTimeOffset.FromUnixTimeMilliseconds((long)Tik);
  //// Форматирование: "yyyy.MM.dd HH:mm:ss,fffff"
  //return dateTime.ToString("yyyy.MM.dd HH:mm:ss,fffff");
  // }


}

public record DataTimeVariableV(DataTimeVariable[] DataTimeVariable);
