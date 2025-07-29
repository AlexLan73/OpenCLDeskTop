using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Core.Converter;

public interface IChannelConverter
{
  object Convert(object channelObject);
  Type SourceType { get; }
  Type TargetType { get; }
}