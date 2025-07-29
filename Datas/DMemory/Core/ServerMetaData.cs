namespace DMemory.Core;

public class ServerMetaData : BaseMetaData, IDisposable
{

  private readonly string _nameModule;
  private readonly string _clientName;

  public ServerMetaData(MetaSettings meta, MemoryDataProcessor processor)
    : base(meta, processor, "server", "client", meta.MetaEventClient, meta.MetaEventServer)
  {
    _clientName = "client" + meta.MemoryName;
  }
}

//////////////////////////
/*
 
using Common.Event;
   using DMemory.Enum;
   
   namespace DMemory.Core;
   
   
   using MapCommands = Dictionary<string, string>;
   
   
   public class ServerMetaData : BaseMetaData, IDisposable
   {
     public BasicMemoryMd Md;
     public EventWaitHandle SendToClient;
   
     private readonly CancellationTokenSource _cts;
     public readonly Task WaiteEvent;
   
     private readonly string _nameModule;
     private readonly string _clientName;
   
     public SateMode _mode;
     public TransferWaiting _transferWaiting;// Используется только для подтверждения ответа в режиме Work
   
     #region ===_ Time _===
     private readonly ServerMetaDataTimer _timer = new ServerMetaDataTimer();
     #endregion
   
     //public class ServerMetaData : BaseMetaData
     //{
     //  public ServerMetaData(MetaSettings meta, MemoryDataProcessor processor)
     //    : base(meta, processor, "server", "client", meta.MetaEventClient, meta.MetaEventServer)
   
   
     public ServerMetaData(MetaSettings meta, MemoryDataProcessor processor)
           : base(meta, processor, "server", "client", meta.MetaEventClient, meta.MetaEventServer)
     {
       _nameModule = "server" + meta.MemoryName;
       _clientName = "client" + meta.MemoryName;
   
       _cts = new CancellationTokenSource();
   
       SendToClient = new EventWaitHandle(false, EventResetMode.AutoReset, meta.MetaEventClient);
   
       Md = new BasicMemoryMd(
           meta.MetaEventServer,
           meta.MetaSize,
           meta.ControlName,
           CallBackMetaData,
           SendToClient
       );
       
       _mode = SateMode.Initialization;
       _transferWaiting = TransferWaiting.None;
       var initAck = new MapCommands
       {
         [MdCommand.State.AsKey()] = _nameModule,
       };
   
       Md.WriteMetaMap(initAck);
   
   
       _timer.ResetAll();
   
       SystemPulseTimer.On250MilSec += () =>
       { /* действия каждые 0.25 сек * /
         if (_mode == SateMode.Work)
         {
           _timer._timeWork = _timer.IncWork();
         }
         else
           _timer.ResetWork();
       };
   
       SystemPulseTimer.On250MilSec += Comparison250MilSec; 
   
       SystemPulseTimer.On1Second += () =>
       {
         if(_mode == SateMode.Initialization)
           _timer._timeInitialization = _timer.IncInitialization();
         else
           _timer.ResetInitialization();
       };
       SystemPulseTimer.On1Second += Comparison1SecTimer;
   
       SystemPulseTimer.On5Seconds += () =>
       {
         _timer._timeGeneralWork = _timer.IncGeneralWork();
       };
   
       SystemPulseTimer.Start();
       // Старт фона (будет использоваться при добавлении таймов)
       WaiteEvent = Task.CompletedTask;
     }
   
     private void CallBackMetaData(MapCommands map)
     {
       if (map == null || map.Count == 0)
         return;
   
       if (!map.TryGetValue(MdCommand.State.AsKey(), out var stateValue))
         return;
   
       if (stateValue == _nameModule)
         return;
       _timer.ResetGeneralWork();
   
       Console.WriteLine($"[Server] Получено от {stateValue}:");
   
       foreach (var kv in map)
         Console.WriteLine($" - {kv.Key} = {kv.Value}");
   
       switch (_mode)
       {
         case SateMode.Initialization:
         {
           if (map.TryGetValue(MdCommand.Command.AsKey(), out var cmdVal))
           {
   //          _timer.ResetWorkProtocol();
             if (cmdVal == MdCommand.Ok.AsKey())
             {
               _mode = SateMode.Work;
               _transferWaiting = TransferWaiting.Transfer;
               _timer.ResetInitialization();
               Console.WriteLine(">>> Handshake подтверждён, переход в [SERVER] Work");
               return;
             }
             else if (cmdVal == "_")
             {
               // Отвечаем ok
               var reply = new MapCommands
               {
                 [MdCommand.State.AsKey()] = _nameModule,
                 [MdCommand.Command.AsKey()] = MdCommand.Ok.AsKey()
               };
               Console.WriteLine(">>> Server Отправили ok для завершения [SERVER]  handhsake");
               _mode = SateMode.Work;
               _transferWaiting = TransferWaiting.Transfer;
               _timer.ResetInitialization();
               Md.WriteMetaMap(reply);
               return;
             }
           }
   
           // Если команд нет — шлём пустое подтверждение
           var initAck = new MapCommands
           {
             [MdCommand.State.AsKey()] = _nameModule,
             [MdCommand.Command.AsKey()] = "_"
           };
           Md.WriteMetaMap(initAck);
           Console.WriteLine(">>> Отправили пустой command server → client");
           break;
         }
   
         case SateMode.Work:
         {
           //  Когда будут посылаться данные ставится TransferWaiting.Waiting !!
           // Здесь будет основная логика работы: приём данных, реакции, управление
           Console.WriteLine(">>> Работаем: получили данные в режиме SERVER Work");
           // 👇 Пока ничего не шлём, ждём команды подтверждения
   
           if (map.Count < 2) return;
           _timer.ResetWork();
           _timer.ResetWorkSendCount();
           if (map.TryGetValue(MdCommand.Command.AsKey(), out var cmdVal))
           {
             if (cmdVal == MdCommand.Ok.AsKey())
             {
               _transferWaiting = TransferWaiting.Transfer; // подтверждение, что данные были приняты
               Console.WriteLine(">>> Handshake подтверждён, переход в SERVER Work");
               return;
             }
           }
           else
           {
             var searchTerms = new List<string> { MdCommand.State.AsKey(), "id" };
             var matchedKeys = map.Keys.ToList()
               .Where(key => searchTerms.Any(term => key.Contains(term, StringComparison.OrdinalIgnoreCase)))
               .ToList();
             if (matchedKeys.Count == 0)
               return;
   
             //if()
             foreach (string kv in matchedKeys)
               Console.WriteLine($" - внешний уровень [client] !!!!  в SERVER  == >  {kv} = {map[kv]}");
             //  обработка данных
             map.Clear();
             map.Add(MdCommand.State.AsKey(), _nameModule);
             map.Add(MdCommand.Command.AsKey(), MdCommand.Ok.AsKey());
             _transferWaiting = TransferWaiting.Transfer;
             Md.WriteMetaMap(map);
           }
           break;
         }
         case SateMode.Dispose:
             Console.WriteLine(">>> Завершаем работу");
             break;
   
         case SateMode.None:
         default:
           throw new ArgumentOutOfRangeException(nameof(_mode), _mode, null);
       }
     }
   
     public void Dispose()
     {
       Console.WriteLine($"ServerMetaData -- Dispose()");
       _cts.Cancel();
       Md?.Dispose();
       SendToClient?.Dispose();
     }
   
     #region ===-- Comparison1SecTimer ---
     private void Comparison250MilSec()
     {
       if (_mode == SateMode.Work && _timer.GetWork()> _timer._CompelWork)
       {
         _timer.ResetWork();
         var initAck = new MapCommands
         {
           [MdCommand.State.AsKey()] = _nameModule,
           [MdCommand.Command.AsKey()] = "_"
         };
         Md.WriteMetaMap(initAck);
         _timer._workSendCount = _timer.IncWorkSendCount();
       }
     }
     private void Comparison1SecTimer()
     {
   
       if (_mode == SateMode.Work && _timer.GetInitialization() > _timer._CompeGeneralWork)
       { // время вышло связи нет переходим на начальный уровень
         _mode = SateMode.Initialization;
         _timer.ResetWork();
         _timer.ResetInitialization();
         _transferWaiting = TransferWaiting.None;
   
         var initAck = new MapCommands
         {
           [MdCommand.State.AsKey()] = _nameModule,
           [MdCommand.Command.AsKey()] = "_"
         };
         Md.WriteMetaMap(initAck);
         return;
       }
   
       if (_mode == SateMode.Initialization && (_timer.GetInitialization()% 5  == 1))
       { // время вышло связи нет переходим на начальный уровень
   //      _mode = SateMode.Initialization;
         _transferWaiting = TransferWaiting.None;
   
         _timer.ResetWork();
   //      ResetInitialization();
         var initAck = new MapCommands
         {
           [MdCommand.State.AsKey()] = _nameModule,
           [MdCommand.Command.AsKey()] = "_"
         };
         Md.WriteMetaMap(initAck);
         return;
       }
   
       if (_mode == SateMode.Work && _timer.GetWorkSendCount() > _timer._CompelWorkSendCount)
       {
         _mode = SateMode.Initialization;
         _timer.ResetWork();
         _timer.ResetInitialization();
         _timer.ResetWorkSendCount();
         var initAck = new MapCommands
         {
           [MdCommand.State.AsKey()] = _nameModule,
           [MdCommand.Command.AsKey()] = "_"
         };
         Md.WriteMetaMap(initAck);
         _timer._workSendCount = _timer.IncWorkSendCount();
       }
     }
     #endregion
   
   
     public void WriteMetaMap(MapCommands map)
     {
       Md.WriteMetaMap(map);
     }
   }
   
   
 
 */