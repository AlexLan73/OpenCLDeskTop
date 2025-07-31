namespace DMemory.Core;

public class ServerMetaDataTimer
{
  public int _oneSecCounter = 0;
  public int _fiveSecCounter = 0;
  public int _missedWorkAcks = 0;
  public int _workOkExpecting = 0;
  public int _initTimer = 0;
  public int _timeGeneralWork = 0;
  public int _timeWork = 0;
  public int _timeInitialization = 0;
  public int _workSendCount = 0;

  public int _CompeGeneralWork { get; private set; } = 4; // 60 сек  => 1 * 12 * 5
  public int _CompelWork { get; private set; } = 6;
  public int _CompeInitialization { get; private set; } = 6;
  public int _CompelWorkSendCount { get; private set; } = 5;

  // --------------- Методы для доступа, сброса и инкремента ----------------

  public int IncOneSec() => Interlocked.Increment(ref _oneSecCounter);
  public int IncFiveSec() => Interlocked.Increment(ref _fiveSecCounter);
  public int IncGeneralWork() => Interlocked.Increment(ref _timeGeneralWork);
  public int IncWork() => Interlocked.Increment(ref _timeWork);
  public int IncWorkSendCount() => Interlocked.Increment(ref _workSendCount);
  public int IncInitialization() => Interlocked.Increment(ref _timeInitialization);
  public int IncWorkAckMissed() => Interlocked.Increment(ref _missedWorkAcks);
  public int IncInit() => Interlocked.Increment(ref _initTimer);

  public void ResetOneSec() => Interlocked.Exchange(ref _oneSecCounter, 0);
  public void ResetTenSec() => Interlocked.Exchange(ref _fiveSecCounter, 0);
  public void ResetGeneralWork() => Interlocked.Exchange(ref _timeGeneralWork, 0);
  public void ResetWork() => Interlocked.Exchange(ref _timeWork, 0);
  public void ResetWorkSendCount() => Interlocked.Exchange(ref _workSendCount, 0);
  public void ResetInitialization() => Interlocked.Exchange(ref _timeInitialization, 0);
  public void ResetWorkAckMissed() => Interlocked.Exchange(ref _missedWorkAcks, 0);
  public void ResetInit() => Interlocked.Exchange(ref _initTimer, 0);

  public int GetOneSec() => Interlocked.CompareExchange(ref _oneSecCounter, 0, 0);
  public int GetTenSec() => Interlocked.CompareExchange(ref _fiveSecCounter, 0, 0);
  public int GetGeneralWork() => Interlocked.CompareExchange(ref _timeGeneralWork, 0, 0);
  public int GetWork() => Interlocked.CompareExchange(ref _timeWork, 0, 0);
  public int GetWorkSendCount() => Interlocked.CompareExchange(ref _workSendCount, 0, 0);
  public int GetInitialization() => Interlocked.CompareExchange(ref _timeInitialization, 0, 0);
  public int GetWorkAckMissed() => Interlocked.CompareExchange(ref _missedWorkAcks, 0, 0);
  public int GetInit() => Interlocked.CompareExchange(ref _initTimer, 0, 0);

  public void ResetAll()
  {
    ResetOneSec();
    ResetTenSec();
    ResetWorkAckMissed();
    ResetInit();
    ResetGeneralWork();
    ResetWork();
    ResetInitialization();
    ResetWorkSendCount();
  }

  public void ResetWorkProtocol()
  {
    ResetGeneralWork();
    ResetWork();
    ResetInitialization();

  }



  // Доступ к константам
  public int CompeGeneralWork => _CompeGeneralWork;
  public int CompelWork => _CompelWork;
  public int CompeInitialization => _CompeInitialization;
  public int CompelWorkSendCount => _CompelWorkSendCount;
}
