namespace DMemory.Enum;

public enum MdCommand
{
  None, State, Command, Work, Ok, No, Error, Crc
}

public static class MdCommandExtensions
{
  public static string AsKey(this MdCommand cmd) => cmd switch
  {
    MdCommand.State => "state",
    MdCommand.Command => "command",
    MdCommand.Work => "work",
    MdCommand.Ok => "ok",
    MdCommand.No => "no",
    MdCommand.Error => "error",
    MdCommand.Crc => "crc",
    _ => ""
  };
}

//// Пример использования:
//var reserved = new Dictionary<string, string>
//{
//  [MdCommand.State.AsKey()] = "serverCUDA",
//  [MdCommand.Command.AsKey()] = "ok"
//};
