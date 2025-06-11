using Common.Core;
using System.Windows.Forms;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Modules.Core;

public class ReadWriteYaml(string pathFileName)
{
  private string _pathFileName = pathFileName;
  public Dictionary<int, IpAddressOne> ReadYaml(string path = null)
  {
    if(!string.IsNullOrEmpty(path))
      _pathFileName = path;

    if (!File.Exists(_pathFileName))
      throw new MyException($"Error not file {_pathFileName}: ", -100);

    // Создаём десериализатор с той же конвенцией
    var deserializer = new DeserializerBuilder()
      .WithNamingConvention(CamelCaseNamingConvention.Instance)
      .Build();

    // Читаем YAML из файла и десериализуем обратно в словарь
    using var reader = new StreamReader(_pathFileName);
    var loadedDict = deserializer.Deserialize<Dictionary<int, IpAddressOne>>(reader);

    return loadedDict;
  }

  public void WriteYaml(Dictionary<int, IpAddressOne> data, string path = null)
  {
    if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(_pathFileName))
      throw new MyException($"Error not name file save  {_pathFileName}: ", -101);

    if (data == null)
      throw new MyException($"Error data in save Yaml: ", -102);

    // Создаём сериализатор с CamelCase конвенцией
    var serializer = new SerializerBuilder()
      .WithNamingConvention(CamelCaseNamingConvention.Instance)
      .Build();

    // Сохраняем словарь в YAML-файл
    using var writer = new StreamWriter(_pathFileName);
    serializer.Serialize(writer, data);
  }


}






/*
    
 
 string filePath = "ipAddresses.yaml";
   
   // Создаём сериализатор с CamelCase конвенцией
   var serializer = new SerializerBuilder()
     .WithNamingConvention(CamelCaseNamingConvention.Instance)
     .Build();
   
   // Сохраняем словарь в YAML-файл
   using (var writer = new StreamWriter(filePath))
   {
     serializer.Serialize(writer, ipDict);
   }
   
   Console.WriteLine($"Данные сохранены в файл {filePath}");
   
   // Создаём десериализатор с той же конвенцией
   var deserializer = new DeserializerBuilder()
     .WithNamingConvention(CamelCaseNamingConvention.Instance)
     .Build();
   
   // Читаем YAML из файла и десериализуем обратно в словарь
   Dictionary<int, IpAddressDict> loadedDict;
   using (var reader = new StreamReader(filePath))
   {
     loadedDict = deserializer.Deserialize<Dictionary<int, IpAddressDict>>(reader);
   }
   
   Console.WriteLine("Данные, считанные из файла:");
   foreach (var kvp in loadedDict)
   {
     Console.WriteLine($"Key: {kvp.Key}, Name: {kvp.Value.Name}, IP: {kvp.Value.IpAddress}, Port1: {kvp.Value.Port1}");
   }
   
 */