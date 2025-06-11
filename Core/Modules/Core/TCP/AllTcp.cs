using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using Modules.Core.TCP; // пространство имен с Tcp00, Tcp01 ... Tcp09

namespace Modules.Core.TCP;

public class AllTcp:IDisposable
{
  private ConcurrentDictionary<int, TcpDuplex> _dAllTcp = new ();

  public AllTcp(string pathFileYaml="")
  {
    Dictionary<int, IpAddressOne> dIpAddress = new ();
    if (pathFileYaml == "")
    {
     // читаем из блока данных
    }
    else
    {
      dIpAddress = new ReadWriteYaml(pathFileYaml).ReadYaml();
    }
    //  Перенести в блок Data
    foreach (var (key, value) in dIpAddress.Where(x=>x.Key==0))
    {
      _dAllTcp.AddOrUpdate(
        key,
        new TcpDuplex(value), // значение для добавления, если ключ отсутствует
        (Key, Value) => new TcpDuplex(value) // функция обновления, если ключ есть
      );
    }

    foreach (var (key, value) in _dAllTcp)
    {
      _dAllTcp[key].RunRead();
    }

  }

  public void Dispose()
  {
    foreach (var (key, value) in _dAllTcp)
      _dAllTcp[key].Dispose();


  }
}



/*

    //    Получаем сборку, где находятся классы(например, текущая сборка)
   Assembly assembly = Assembly.GetExecutingAssembly();
   // Фильтруем типы по пространству имен, имени и наследованию от TcpDuplex
   var tcpTypes = assembly.GetTypes()
     .Where(t => t.IsClass
                 && !t.IsAbstract
                 && t.Namespace == "Modules.Core.TCP"
                 && t.Name.StartsWith("Tcp0")
                 && typeof(TcpDuplex).IsAssignableFrom(t))
     .OrderBy(t => t.Name) // для упорядочивания по имени Tcp00, Tcp01 и т.д.
     .ToList();

   int index = 0;
   foreach (var type in tcpTypes)
   {
     // Создаем экземпляр класса (предполагается, что есть конструктор без параметров)
     var instance = (TcpDuplex)Activator.CreateInstance(type);
     _dAllTcp.TryAdd(index++, instance);
   }



// 
Получаем сборку, где находятся классы (например, текущая сборка)
   Assembly assembly = Assembly.GetExecutingAssembly();
   
   // Фильтруем типы по пространству имен, имени и наследованию от TcpDuplex
   var tcpTypes = assembly.GetTypes()
       .Where(t => t.IsClass
                   && !t.IsAbstract
                   && t.Namespace == "Modules.Core.TCP"
                   && t.Name.StartsWith("Tcp0")
                   && typeof(TcpDuplex).IsAssignableFrom(t))
       .OrderBy(t => t.Name) // для упорядочивания по имени Tcp00, Tcp01 и т.д.
       .ToList();
   
   // Создаем экземпляры и добавляем в словарь
   int index = 0;
   foreach (var type in tcpTypes)
   {
       // Создаем экземпляр класса (предполагается, что есть конструктор без параметров)
       var instance = (TcpDuplex)Activator.CreateInstance(type);
       _dAllTcp.TryAdd(index++, instance);
   }
   

    foreach (var (key, value) in dIpAddress)
   {
     switch (key)
     {
       case 0:
         _dAllTcp.AddOrUpdate(key,new TcpDuplex(value), // значение для добавления, если ключ отсутствует
           (existingKey, existingValue) => new TcpDuplex(value) // функция обновления, если ключ есть
         );

         break;
       case 1:
         break;
       case 2:
         break;
       case 3:
         break;
       case 4:
         break;
     }
     //      
   }





    var dIpAddress = new ReadWriteYaml(pathFileYaml).ReadYaml();
  ConcurrentDictionary<int, TcpDuplex> _dAllTcp = new ();
   foreach (var (key, value) in dIpAddress)
         _dAllTcp.AddOrUpdate(key, ... )
 
  

   {
     switch (key)
     {
       case 0:
         _dAllTcp.AddOrUpdate(key, new Func<int,TArg,TcpDuplex>())
 
name: Server
     ipAddress: 127.0.0.1
     port1: 20000
     port2: 20001
   1:
     id: 1
     name: TclFFT
     ipAddress: 127.0.0.1
     port1: 20010
     port2: 20011
   2:
     id: 2
     name: TminskFFT
     ipAddress: 127.0.0.1
     port1: 20020
     port2: 20021
   3:
     id: 3
     name: OpenCL
     ipAddress: 127.0.0.1
     port1: 20030
     port2: 20031
   4:
     id: 4
     name: CUDA
     ipAddress: 127.0.0.1
     port1: 20040
     port2: 20041


 */