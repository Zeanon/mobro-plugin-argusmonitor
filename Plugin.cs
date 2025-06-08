using Microsoft.Extensions.Logging;
using MoBro.Plugin.SDK;
using MoBro.Plugin.SDK.Services;
using System;
using System.Collections.Generic;

namespace Zeanon.Plugin.ArgusMonitor;

public class Plugin : IMoBroPlugin, IDisposable
{
    public static readonly string VERSION;

    private const int DefaultInitialDelay = 1;
    private const int DefaultUpdateFrequencyMs = 1000;
    private const int DefaultInitDelay = 0;

    private readonly IMoBroSettings _settings;
    private readonly IMoBroScheduler _scheduler;

    private readonly ArgusMonitor _argus;

    static Plugin()
    {
        Version? _version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        VERSION = _version == null ? "unknown" : _version.ToString(3);
    }

    public Plugin(IMoBroSettings settings, IMoBroService service, IMoBroScheduler scheduler, ILogger logger)
    {
        logger.LogInformation("Plugin Version: {VERSION}", VERSION);

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
        List<Action> toUpdate = new();
        int updateFrequency = _settings.GetValue("update_frequency", DefaultUpdateFrequencyMs);

        // set the settings
        _argus.UpdateSettings(_settings);

        // wait for the argus api to connect to Argus Monitor
        _argus.Open(updateFrequency);

        // wait for a proper connection and data
        _argus.WaitForArgus(updateFrequency);

        // init the total sensor count so we know it for the list initialization
        _argus.WaitForSensors(updateFrequency);

        // register custom hardware category
        _argus.RegisterCategories();

        // register groups and metrics
        _argus.RegisterItems(toUpdate);

        // oneoff to update static values
        _scheduler.OneOff(() =>
        {
            foreach (Action update in toUpdate) update();

            // start polling metric values
            _scheduler.Interval(_argus.UpdateMetricValues,
                                TimeSpan.FromMilliseconds(updateFrequency),
                                TimeSpan.FromMilliseconds(0));
        }, TimeSpan.FromSeconds(_settings.GetValue("poll_delay", DefaultInitialDelay)));
    }

    public void Dispose()
    {
        _argus.Dispose();
    }
}
