using System;
using System.Timers;

namespace Common.Event;


public static class SystemPulseTimer
{
  private static readonly Timer _timer;
  private static int _halfSecondTicks = 0;

  // События подписки
  public static event Action On250MilSec;
  public static event Action On1Second;
  public static event Action On5Seconds;

  static SystemPulseTimer()
  {
    _timer = new Timer(250); // 0.25 секунды
    _timer.Elapsed += (s, e) =>
    {
      _halfSecondTicks++;

      On250MilSec?.Invoke();
      if (_halfSecondTicks % 4 == 0) On1Second?.Invoke();
      if (_halfSecondTicks % 100 == 0) On5Seconds?.Invoke();
    };
    _timer.AutoReset = true;
  }

  public static void Start() => _timer.Start();
  public static void Stop() => _timer.Stop();
}
