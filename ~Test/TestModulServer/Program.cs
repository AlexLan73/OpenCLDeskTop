// See https://aka.ms/new-console-template for more information
using Common.Core;
using Modules.Core;
using System.Net;
using Windows.Services.Maps;
using Modules.Core;

Console.WriteLine("Test модули сервер ");

string _pathYaml = "E:\\C#\\OpenCLDeskTop\\Core\\DeskTop\\ipAddresses.yaml";

var _dIp = new ReadWriteYaml(_pathYaml).ReadYaml();

TcpDuplex _v0 = new TcpDuplex(_dIp[1]);
var _task = _v0.RunRead();
//System.Threading.Thread.Sleep(20000); // Даем серверу время запуститься

//_v0.StopReadServer();
Task.WaitAll(_task);


//var _v = new TestSocketPrimer();
//_v.Run();

var k =1;