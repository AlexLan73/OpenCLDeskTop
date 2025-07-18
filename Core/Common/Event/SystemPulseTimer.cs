using System;
using System.Timers;

namespace Common.Event;


public static class SystemPulseTimer
{
  private static readonly Timer _timer;
  private static int _halfSecondTicks = 0;

  // События подписки
  public static event Action OnHalfSecond;
  public static event Action OnOneSecond;
  public static event Action OnFiveSeconds;

  static SystemPulseTimer()
  {
    _timer = new Timer(500); // 0.5 секунды
    _timer.Elapsed += (s, e) =>
    {
      _halfSecondTicks++;

      OnHalfSecond?.Invoke();
      if (_halfSecondTicks % 2 == 0) OnOneSecond?.Invoke();
      if (_halfSecondTicks % 20 == 0) OnFiveSeconds?.Invoke();
    };
    _timer.AutoReset = true;
  }

  public static void Start() => _timer.Start();
  public static void Stop() => _timer.Stop();
}
