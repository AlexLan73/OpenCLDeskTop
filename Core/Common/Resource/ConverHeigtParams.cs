using System.Globalization;
using System.Windows.Data;

namespace Common.Resource;

[ValueConversion(typeof(string), typeof(string))]
public class HeightParams : IValueConverter     // Conver
{
  public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
  {
    if (value != null)
    {
      var count = (int)value;
      return count == 0 ? "0" : "auto";
    }

    return "0";
  }

  public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
  {
    throw new NotImplementedException();
  }
}

[ValueConversion(typeof(string), typeof(string))]
public class HeightParams0 : IValueConverter     // Conver
{
  public object Convert(object value, Type targetType, object parameter, CultureInfo culture)=>
    value == null ? "0" : "*";
  

  public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
  {
    throw new NotImplementedException();
  }
}

[ValueConversion(typeof(bool), typeof(string))]
public class HeightExpander : IValueConverter     // Conver
{
  public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
  {
    return value != null && (ExpandDirection)value == ExpandDirection.Down ? "20" : "200";
//    return @is ? "x*0.2" : "120";
  }


  public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
  {
    throw new NotImplementedException();
  }
}


[ValueConversion(typeof(string), typeof(string))]
public class HeightOpenClose: IValueConverter    
{
  public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => (string)value == (string)parameter ? "auto" : "0";

  public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
  {
    throw new NotImplementedException();
  }
}

[ValueConversion(typeof(string), typeof(string))]
public class HeightIsOpenClose : IValueConverter
{
  public object Convert(object value, Type targetType, object parameter, CultureInfo culture)=>
    (string)value == (string)parameter ? "auto" : "0";

  public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
  {
    throw new NotImplementedException();
  }
}

