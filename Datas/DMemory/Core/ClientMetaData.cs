using DMemory.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DMemory.Core;
using MapCommands = Dictionary<string, string>;

public class ClientMetaData : IDisposable
{
  public BasicMemoryMd Md;
  public EventWaitHandle sendToServer;

  private readonly CancellationTokenSource _cts;
  public readonly Task WaiteEvent;

  private readonly string _nameModule;
  private readonly string _clientName;
  public TransferWaiting _transferWaiting;// Используется только для подтверждения ответа в режиме Work


  public SateMode _mode;

  public ClientMetaData(MetaSettings meta)
  {
    _nameModule = "client" + meta.MemoryName;
    _clientName = "server" + meta.MemoryName;

    _cts = new CancellationTokenSource();

    sendToServer = new EventWaitHandle(false, EventResetMode.AutoReset, meta.MetaEventServer);

    Md = new BasicMemoryMd(
        meta.MetaEventClient,
        meta.MetaSize,
        meta.ControlName,
        CallBackMetaData,
        sendToServer
    );

    _mode = SateMode.Initialization;
    _transferWaiting = TransferWaiting.None;
    // Старт фона (будет использоваться при добавлении таймов)
    Thread.Sleep(200);
    var reply = new MapCommands
    {
      [MdCommand.State.AsKey()] = _nameModule,
    };
    Md.WriteMetaMap(reply);
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

    Console.WriteLine($"[Client] Получено от {stateValue}:");

    foreach (var kv in map)
      Console.WriteLine($" - {kv.Key} = {kv.Value}");

    switch (_mode)
    {
      case SateMode.Initialization:
      {
        if (map.TryGetValue(MdCommand.Command.AsKey(), out var cmdVal))
        {
          if (cmdVal == MdCommand.Ok.AsKey())
          {
            _mode = SateMode.Work;
            Console.WriteLine(">>> Handshake подтверждён, переход в Work");
            _mode = SateMode.Work;
            _transferWaiting = TransferWaiting.Transfer;
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
            Md.WriteMetaMap(reply);
            Console.WriteLine(">>> Client Отправили ok для завершения handhsake");
            _mode = SateMode.Work;
            _transferWaiting = TransferWaiting.Transfer;
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
        Console.WriteLine(">>> Отправили пустой command  client -> server");
        break;
      }

      case SateMode.Work:
        {
          // Здесь будет основная логика работы: приём данных, реакции, управление
          // 👇 Пока ничего не шлём, ждём команды подтверждения
          //  Когда будут посылаться данные ставится TransferWaiting.Waiting !!
          // Здесь будет основная логика работы: приём данных, реакции, управление
          Console.WriteLine(">>> Работаем: получили данные в режиме CLIENT Work");
          // 👇 Пока ничего не шлём, ждём команды подтверждения

          if (map.Count == 1) return;

          if (map.TryGetValue(MdCommand.Command.AsKey(), out var cmdVal))
          {
            if (cmdVal == MdCommand.Ok.AsKey())
            {
              //              _mode = SateMode.Work;
              _transferWaiting = TransferWaiting.Transfer; // подтверждение, что данные были приняты
              Console.WriteLine(">>> [CLIENT]  Work подтверждение полученных данных ");
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
            foreach (var kv in matchedKeys)
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
    sendToServer?.Dispose();
  }

  public void WriteMetaMap(MapCommands map1)
  {
    Md.WriteMetaMap(map1);
    _transferWaiting = TransferWaiting.Transfer;
  }
}

