namespace Common.Core;

public interface IIpAddressOne
{
  int Id { get; set; }
  string Name { get; set; }
  string IpAddress { get; set; }
  int Port1 { get; set; }
  int Port2 { get; set; }
}
public class IpAddressOne: IIpAddressOne
{

  public int Id { get; set; }
  public string Name { get; set; }
  public string IpAddress { get; set; }
  public int Port1 { get; set; }
  public int Port2 { get; set; }
}


//public IpAddressOne(IIpAddressOne source) 
//  : this(source.Id, source.Name, source.IpAddress, source.Port1)
//{
//}

//public IpAddressOne(int id, string name, string ipAddress, int port)
//{
//  Id = id;
//  Name  = name;
//  IpAddress  = ipAddress;
//  Port1  = port;

//}

//public IpAddressOne()
//{
//}
