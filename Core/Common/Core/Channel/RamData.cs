using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MapCommands = System.Collections.Generic.Dictionary<string, string>;
namespace Common.Core.Channel;

public record RamData(object Data, Type DataType, MapCommands MetaData);