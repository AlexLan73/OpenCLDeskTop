using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Modules.Core;


public class YamlConverter
{
  private readonly IDeserializer _deserializer;
  private readonly ISerializer _serializer;

  public YamlConverter()
  {
    _deserializer = new DeserializerBuilder()
      .WithNamingConvention(CamelCaseNamingConvention.Instance)
      .Build();

    _serializer = new SerializerBuilder()
      .WithNamingConvention(CamelCaseNamingConvention.Instance)
      .Build();
  }

  // Десериализация из YAML-строки в объект типа T
  public T Deserialize<T>(string yaml)
  {
    if (string.IsNullOrEmpty(yaml))
      throw new ArgumentException("YAML string is null or empty", nameof(yaml));

    return _deserializer.Deserialize<T>(yaml);
  }

  // Сериализация объекта типа T в YAML-строку
  public string Serialize<T>(T obj)
  {
    if (obj == null)
      throw new ArgumentNullException(nameof(obj));

    return _serializer.Serialize(obj);
  }
}

/*
 
 public class IpAddressOne
   {
       public int Id { get; set; }
       public string Name { get; set; }
       public string IpAddress { get; set; }
       public int Port1 { get; set; }
       public int Port2 { get; set; }
   }
   
   class Program
   {
       static void Main()
       {
           var converter = new YamlConverter();
   
           var obj = new IpAddressOne
           {
               Id = 1,
               Name = "Test",
               IpAddress = "192.168.0.1",
               Port1 = 8080,
               Port2 = 8081
           };
   
           // Сериализация объекта в YAML-строку
           string yaml = converter.Serialize(obj);
           Console.WriteLine("Serialized YAML:\n" + yaml);
   
           // Десериализация обратно из YAML-строки
           var deserializedObj = converter.Deserialize<IpAddressOne>(yaml);
           Console.WriteLine($"Deserialized object: Id={deserializedObj.Id}, Name={deserializedObj.Name}");
       }
   }
   
 */