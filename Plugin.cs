using System;
using System.Threading;
using MoBro.Plugin.SDK;
using MoBro.Plugin.SDK.Services;

namespace Zeanon.Plugin.ArgusMonitor;

public class Plugin : IMoBroPlugin, IDisposable
{
  private static readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(2);
  private const int DefaultUpdateFrequencyMs = 1000;
  private const int DefaultInitDelay = 0;

  private readonly IMoBroSettings _settings;
  private readonly IMoBroService _service;
  private readonly IMoBroScheduler _scheduler;

  private readonly ArgusMonitor _argus;

  public Plugin(IMoBroSettings settings, IMoBroService service, IMoBroScheduler scheduler)
  {
    _settings = settings;
    _service = service;
    _scheduler = scheduler;
    _argus = new ArgusMonitor();
  }

  public void Init()
  {
    _argus.Start();

    while(_argus.CheckConnection() == 0) {
      Thread.Sleep(5);
    }

    var initDelay = _settings.GetValue("init_delay", DefaultInitDelay);
    _scheduler.OneOff(InitArgus, TimeSpan.FromSeconds(initDelay));
  }

  private void InitArgus()
  {
    while (!_argus.CheckData())
    {
      Thread.Sleep(5);
    }

    _argus.Update(_settings);

    // register groups and metrics
    _service.Register(_argus.GetMetricItems());

    // start polling metric values
    var updateFrequency = _settings.GetValue("update_frequency", DefaultUpdateFrequencyMs);
    _scheduler.Interval(UpdateMetricValues, TimeSpan.FromMilliseconds(updateFrequency), InitialDelay);
  }

  private void UpdateMetricValues()
  {
    _service.UpdateMetricValues(_argus.GetMetricValues());
  }

  public void Dispose()
  {
    _argus.Dispose();
  }
}