using Microsoft.Extensions.Logging;
using MoBro.Plugin.SDK;
using MoBro.Plugin.SDK.Services;
using System;

namespace Zeanon.Plugin.ArgusMonitor;

public class Plugin : IMoBroPlugin, IDisposable
{
    private static readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(2);
    private const int DefaultUpdateFrequencyMs = 1000;
    private const int DefaultInitDelay = 0;

    private readonly IMoBroSettings _settings;
    private readonly IMoBroScheduler _scheduler;

    private readonly ArgusMonitor _argus;

    public Plugin(IMoBroSettings settings, IMoBroService service, IMoBroScheduler scheduler, ILogger logger)
    {
        _settings = settings;
        _scheduler = scheduler;
        _argus = new ArgusMonitor(service, logger);
    }

    public void Init()
    {
        _argus.Start();

        _argus.WaitForConnection();

        int initDelay = _settings.GetValue("init_delay", DefaultInitDelay);
        _scheduler.OneOff(InitArgus, TimeSpan.FromSeconds(initDelay));
    }

    private void InitArgus()
    {
        _argus.WaitForData();

        // set the settings
        _argus.UpdateSettings(_settings);

        // register custom hardware category
        _argus.RegisterCategories();

        // register groups and metrics
        _argus.RegisterItems();

        // start polling metric values
        int updateFrequency = _settings.GetValue("update_frequency", DefaultUpdateFrequencyMs);
        _scheduler.Interval(UpdateMetricValues, TimeSpan.FromMilliseconds(updateFrequency), InitialDelay);
    }

    private void UpdateMetricValues()
    {
        _argus.UpdateMetricValues();
    }

    public void Dispose()
    {
        _argus.Dispose();
    }
}