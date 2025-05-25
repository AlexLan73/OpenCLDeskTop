using System.Globalization;
using System.Windows.Data;

namespace Common.Resource;

[ValueConversion(typeof(int), typeof(string))]
public class ConResColor: IValueConverter     // Conver
{
  public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
  {
    if (value == null) return 0;
    var count = (int)value;
    var color = count switch
    {
      -3 => "Red",
      -2 => "MediumVioletRed",
      -1 => "IndianRed",
      0 => "White",
      1 => "Gray",
      2 => "LightGreen",
      3 => "AliceBlue",
      4 => "YellowGreen",
      _ => ""
    };
    return color;

  }

  public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
  {
    throw new NotImplementedException();
  }
}

[ValueConversion(typeof(object), typeof(string))]
public class ConResColorBool : IValueConverter     // Conver
{
  public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
  {
    if (value != null)
      return (bool)value? "Green": "Red";

    return "Red"; 
  }

  public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
  {
    throw new NotImplementedException();
  }
}
