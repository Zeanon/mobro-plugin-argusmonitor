using Microsoft.Extensions.Logging;
using MoBro.Plugin.SDK;
using MoBro.Plugin.SDK.Services;
using System;

namespace Zeanon.Plugin.ArgusMonitor;

public class Plugin : IMoBroPlugin
{
    private const int DefaultInitialDelay = 1;
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
        _scheduler.OneOff(InitArgus, TimeSpan.FromSeconds(_settings.GetValue("init_delay", DefaultInitDelay)));
    }

    private void InitArgus()
    {
        int updateFrequency = _settings.GetValue("update_frequency", DefaultUpdateFrequencyMs);
        _argus.UpdateSettings(_settings);

        // wait for the argus api to connect to Argus Monitor
        _argus.Open(updateFrequency);

        // wait for a proper connection and data
        _argus.WaitForArgus(updateFrequency);

        // init the total sensor count so we know it for the list initialization
        _argus.InitTotalSensorCount(updateFrequency);

        // register custom hardware category
        _argus.RegisterCategories();

        // register groups and metrics
        _argus.RegisterItems(updateFrequency);

        // start polling metric values
        _scheduler.Interval(_argus.UpdateMetricValues,
            TimeSpan.FromMilliseconds(updateFrequency),
            TimeSpan.FromSeconds(_settings.GetValue("poll_delay", DefaultInitialDelay)));
    }
}