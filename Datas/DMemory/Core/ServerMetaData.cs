using DMemory.Enum;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DMemory.Core;
using MapCommands = System.Collections.Generic.Dictionary<string, string>;

public class ServerMetaData
{
  private readonly MetaSettings _meta;
  public BasicMemoryMd Md;
  public EventWaitHandle SendToClient;
  private readonly CancellationTokenSource _cts;                 // завершение потока
  public readonly Task WaiteEvent;
  private Action<MapCommands> _callBack;            // Инициализация пустым делегатом передать данные на верх
  private readonly string _nameModule;
  private readonly string _clientName;

  private readonly ConcurrentQueue<MapCommands> _queue = new();
  private readonly ManualResetEventSlim _signal = new(false);
  private SateMode _mode;
  public ServerMetaData(MetaSettings meta)
  {
    _meta = meta;
    _nameModule = "server" + _meta.MemoryName;
    _clientName = "client" + _meta.MemoryName;
    _cts = new CancellationTokenSource();
    var token = _cts.Token;
    SendToClient = new EventWaitHandle(false, EventResetMode.AutoReset, _meta.MetaEventClient);
    Md = new BasicMemoryMd(_meta.MetaEventServer, _meta.MetaSize, _meta.ControlName, CallBackMetaData, SendToClient);
    //    WaiteEvent = ReadDataCallBack(token);
    _mode = SateMode.Initialization;

    WaiteEvent = ReadDataCallBack(_cts.Token);

  }
  private async Task ReadDataCallBack(CancellationToken cancellationToken = default)
  {
    int i = 0;
    try
    {
      while (!cancellationToken.IsCancellationRequested)
      {
        // Ждём новых данных или таймаута 1 сек
        if (_signal.Wait(1000, cancellationToken))
        {
          _signal.Reset();

          while (_queue.TryDequeue(out var map))
          {
            // Проверка данные пришли от client
            if (map.TryGetValue(MdCommand.State.AsKey(), out var stateValue))
            {
              if(_clientName != stateValue) continue;

              switch (_mode)
              {
                case SateMode.Initialization:
                {
                  // Проверка в наличии command если "" посылаем запрос "ok" подтверждение
                  if (map.TryGetValue(MdCommand.Command.AsKey(), out string commandValue))
                  {
                    if (commandValue == MdCommand.Ok.AsKey())
                    {
                      _mode = SateMode.Work;
                      continue;
                    }
                    else
                    {
                      var mapCommand = new MapCommands()
                      {
                        [MdCommand.State.AsKey()] = _nameModule,
                        [MdCommand.Command.AsKey()] = MdCommand.Ok.AsKey(),
                      };
                      Md.WriteMetaMap(mapCommand);
                      continue;
                    }
                  }
                  else
                  {// если нет Command а связь установлена посылаем command = ""
                      var mapCommand = new MapCommands()
                      {
                        [MdCommand.State.AsKey()] = _nameModule,
                        [MdCommand.Command.AsKey()] = "",
                      };
                      Md.WriteMetaMap(mapCommand);
                      continue;
                  }
                  break;
                }
                case SateMode.Work:
                { // если SateMode.Work разбираем map на управляющие значения для обработки данных

                    break;
                }
                case SateMode.Dispose:
                {
                  break;
                }

                case SateMode.None:
                default:
                  throw new ArgumentOutOfRangeException();
              }

            }
            
          }
        }

        // Ваши действия по циклу каждую секунду
        var mapToWrite = new MapCommands()
        {
          [MdCommand.State.AsKey()] = _nameModule,
          ["id_server"] = i.ToString(),
        };
        Md.WriteMetaMap(mapToWrite); // пишем pong
        i++;
      }
    }
    catch (OperationCanceledException)
    {
      // Обычное завершение
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Ошибка в ReadDataCallBack: {ex}");
    }
  }

  //private void ParserComman
  private void CallBackMetaData(MapCommands map)
  {
    if(map == null || map.Count()==0)
      return;
    // Помещаем данные в очередь, сигналим цикл
    _queue.Enqueue(map);
    _signal.Set();

    foreach (var kv in map)
      Console.WriteLine($" - внешний уровень server == >  {kv.Key} = {kv.Value}");
  }

  public void Dispose()
  {
    Console.WriteLine($"ServerPong  -- Dispose() ");
    _cts.Cancel();
    Task.WaitAll(WaiteEvent);
    Md?.Dispose();
    _signal.Dispose();
  }
}

