
namespace Common.Core;
public class MyException : Exception
{
  public MyException(string message, int num) : base(message)
  {
    switch (num)
    {
      case 0:
        F0(message, num);
        break;
      case -1:
        F0(message, -1);
        //                Environment.Exit(-1);
        break;
      case -2:
        F0(message, num);
        Environment.Exit(num);
        break;
    }

    return;

    static void F0(string s, int i) => Console.WriteLine($"  -Error № {i}  -  {s}");
  }

}