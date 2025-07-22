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
        if (map.TryGetValue(MdCommand.Command.AsKey(), out var cmdVal))
        {
          if (cmdVal == MdCommand.Ok.AsKey())
          {
            _mode = SateMode.Work;
            Console.WriteLine(">>> Handshake подтверждён, переход в Work");
            _mode = SateMode.Work;
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

      case SateMode.Work:
        // Здесь будет основная логика работы: приём данных, реакции, управление
        Console.WriteLine(">>> Работаем: получили данные в режиме Work");
        // 👇 Пока ничего не шлём, ждём команды подтверждения
        break;

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
}

