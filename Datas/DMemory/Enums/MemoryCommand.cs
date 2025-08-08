namespace DMemory.Enums;

public enum MdCommand
{
  None, State, Command, Work, Control, Ok, No, Error, Crc, DataOk, Data, Type, Size
}

public static class MdCommandExtensions
{
  public static string AsKey(this MdCommand cmd) => cmd switch
  {
    MdCommand.None => "",
    MdCommand.State => "state",
    MdCommand.Command => "command",
    MdCommand.Work => "work",
    MdCommand.Control => "control",
    MdCommand.Ok => "ok",
    MdCommand.No => "no",
    MdCommand.Error => "error",
    MdCommand.Crc => "crc",
    MdCommand.DataOk => "dataok",
    MdCommand.Data => "data",
    MdCommand.Type => "type",
    MdCommand.Size => "size",
    _ => ""
  };
}

//// Пример использования:
//var reserved = new Dictionary<string, string>
//{
//  [MdCommand.State.AsKey()] = "serverCUDA",
//  [MdCommand.Command.AsKey()] = "ok"
//};
