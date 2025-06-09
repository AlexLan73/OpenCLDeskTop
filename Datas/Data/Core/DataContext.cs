
//using CanDrv;
using Common.Core;
using Common.Enum;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Common.Interfaces;
using System.Runtime.ConstrainedExecution;

namespace Data.Core;

public class Element
{
  public uint Id { get; set; }
}
public interface IDataContext
{
  SourceCache<Element, uint> DElement { get; set; }
  SourceCache<Element, uint> DElementWrite { get; set; }
  SourceCache<Element, uint> DElementRead { get; set; }
  void Dispose();
  ConcurrentDictionary<string, uint> DNameIdCanMatrixElement { get; set; }
  ConcurrentDictionary<string, Guid> DNameIdMatrixElement { get; set; }
  ConcurrentDictionary<string, dynamic> DGlobalData { get; set; }
  ConcurrentDictionary<int, IpAddressOne> DIdIpAddress { get; set; }
  ConcurrentDictionary<string, IpAddressOne> DNameIpAddress { get; set; }
}
public class DataContext: IDataContext, IDisposable
{
  private AutoResetEvent waitHandler = new AutoResetEvent(true);  // объект-событие
  private bool _isReadCan=true;

  public ConcurrentDictionary<int, IpAddressOne> DIdIpAddress { get; set; }
  public ConcurrentDictionary<string, IpAddressOne> DNameIpAddress { get; set; }


  #region ___ CAN ___

  public ConcurrentDictionary<string, uint> DNameIdCanMatrixElement { get; set; } = new();
  public ConcurrentDictionary<string, Guid> DNameIdMatrixElement { get; set; } = new();
  public ConcurrentDictionary<string, dynamic> DGlobalData { get; set; } = new();


  #endregion
  // DElement -> Элемент - вывод на панель данные 
  // UC - WInfoTable
  public SourceCache<Element, uint> DElement { get; set; } = new(t => t.Id);
  public SourceCache<Element, uint> DElementWrite { get; set; } = new(t => t.Id);
  public SourceCache<Element, uint> DElementRead { get; set; } = new(t => t.Id);

  // Сохранение полученных данных из CAN
//  public SourceList<ElementCanData> LsElementCanData { get; set; } = new();

  // Сохранение Листа сообщений из CAN
  //public SourceList<CanCanalView> NLsCanCanal { get; set; } = new SourceList<CanCanalView>();

  private bool _iiAddData = false;
  private readonly IContainerProvider _container;
  private readonly IManagerLogger _logger;
  public DataContext(IContainerProvider containerProvider, IEventAggregator ea)
  {
//    _nameId.Add("")
    _container = containerProvider;
    _logger = _container.Resolve<IManagerLogger>();
//    DIdIpAddress = { get; set; }
//    DNameIpAddress { get; set; }


//!!!!  Вернуть когда будет CAN
//    _container.Resolve<JobCAN>().InicialAdd(AddCAN);
//!!!!  Вернуть когда будет CAN

//    TestCanReserveWords();
//    Task.Run(TAddElementCanData);
//    if (!_iiAddData)
//      return;

//    Task.Run(SetDataCan);
  }

  public void AddIpAddress(Dictionary<int, IpAddressOne> data)
  {
    DIdIpAddress = new ConcurrentDictionary<int, IpAddressOne>(data);
    var data1 = new Dictionary<string, IpAddressOne>();
    foreach (var (key, val) in data)
      data1.TryAdd(val.Name, val);

    DNameIpAddress = new ConcurrentDictionary<string, IpAddressOne>(data1);
  }

  /*
    public DataContext()
    {
      TestCanReserveWords();
  //    TestStartCanRead();
    }
  */
  private void AddCanElementRead(Element elementCanData, string comment = "", bool? _is = null)
  {
    if (elementCanData == null) return;
//    var v0 = CanDataReserveWords.Get(elementCanData.Id);
//    if (v0 == null) return;

    //var comments = _is == null
    //  ? v0.CommentFalse : _is.Value ? v0.CommentTrue : v0.CommentFalse;

    //_logger.Send(new LoggerData(v0.FullName, comments));


    //var element = DElementRead.KeyValues.FirstOrDefault(x => x.Key == v0.Id).Value;
    //if (element == null)
    //  DElementRead.AddOrUpdate(new Element(v0));

    //DElementRead.KeyValues.FirstOrDefault(x => x.Key == v0.Id).Value
    //  .Update(elementCanData, null, comment, _is);
  }
  public void AddCAN(Element d)
  {
//    _elementQueue.Enqueue(d);
    waitHandler.Set();
  }
  private void SetDataCan()
  {
    var rand = new Random();
    while (true)
    {
      var idCanWords = (uint) rand.Next(1, 13); // creates a number between 1 and 12
      var b = rand.Next(0, 2) == 1;
      var bytes8 = new byte[8];
      rand.NextBytes(bytes8);

      AddCAN(new Element {Id = idCanWords});

      Thread.Sleep(100);
    }
  }

  public void Dispose()
  {
    _isReadCan = false;
  }
}

