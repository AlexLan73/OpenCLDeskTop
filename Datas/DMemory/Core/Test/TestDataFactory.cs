using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DMemory.Core.Test;

public  class TestDataFactory
{
  private static readonly Random _rnd = new();

  /// <summary>
  /// Генерация тестового объекта IdDataTimeVal
  /// </summary>
  public IdDataTimeVal CreateDtVariable(int id)
  {
    var convertedId = unchecked((uint)id);
    var tik = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    var value = _rnd.NextDouble() * (100 - 30) + 30; // от 30 до 100
    return new IdDataTimeVal(convertedId, new DataTimeValRec(tik, value));
  }

  /// <summary>
  /// Генерация тестового объекта LoggerBase
  /// </summary>
  public LoggerBase CreateLoggerBase(int id)
  {
    var convertedId = unchecked((uint)id);
    string[] modules = { "ModuleA", "ModuleB", "ModuleC" };
    string[] logs = { "Initialization complete", "Warning: Low memory", "Error: Timeout occurred", "Info: Process started" };

    var module = modules[_rnd.Next(modules.Length)];
    var log = logs[_rnd.Next(logs.Length)];
    var codeValues = Enum.GetValues(typeof(LoggerSendEnumMemory));
    var code = (LoggerSendEnumMemory)codeValues.GetValue(_rnd.Next(codeValues.Length));

    return new LoggerBase(convertedId, module, log, code);
  }

  /// <summary>
  /// Генерация тестового объекта VIdDataTimeVal с указанным числом элементов Variables
  /// </summary>
  public VIdDataTimeVal CreateVDtValues(int id, int variableCount)
  {
    var convertedId = unchecked((uint)id);
    var variables = new DataTimeValRec[variableCount];

    for (int i = 0; i < variableCount; i++)
    {
      var tik = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + (ulong)i * 10;
      var value = _rnd.NextDouble() * (200 - 50) + 50; // от 50 до 200
      variables[i] = new DataTimeValRec(tik, value);
    }

    return new VIdDataTimeVal(convertedId, variables);
  }
}

