using System.Globalization;
using System.Windows.Data;

namespace Common.Resource;
// "Left"  "Right" 

[ValueConversion(typeof(object), typeof(string))]
public class ConvertHorizont : IValueConverter     // Conver
{
  public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
  {
    if (value == null) return "Left" ;

    var _type = (TypeOperation)value;
    return _type == TypeOperation.Write ? "Right": "Left" ;
  }

  public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
  {
    throw new NotImplementedException();
  }
}

[ValueConversion(typeof(object), typeof(string))]
public class ConvertHorizontColor : IValueConverter     // Conver
{
  public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
  {
    if (value == null) return "LightGreen";

    var _type = (TypeOperation)value;
    return _type == TypeOperation.Write ? "LightBlue" : "LightGreen";
  }

  public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
  {
    throw new NotImplementedException();
  }
}
