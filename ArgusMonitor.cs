using Microsoft.Extensions.Logging;
using MoBro.Plugin.SDK.Builders;
using MoBro.Plugin.SDK.Enums;
using MoBro.Plugin.SDK.Exceptions;
using MoBro.Plugin.SDK.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Zeanon.Plugin.ArgusMonitor.CustomItem;
using Zeanon.Plugin.ArgusMonitor.Enums;
using Zeanon.Plugin.ArgusMonitor.Utility;

namespace Zeanon.Plugin.ArgusMonitor;


public class ArgusMonitor : IDisposable
{
    private readonly IntPtr argus_monitor = ArgusMonitorWrapper.Instantiate();

    private readonly IMoBroService _service;
    private readonly ILogger _logger;
    private static readonly StringBuilder CPU = new StringBuilder("CPU");

    public static readonly Dictionary<string, string> Settings = new()
    {
        ["CPU"] = "true",
        ["GPU"] = "true",
        ["RAM"] = "true",
        ["Mainboard"] = "true",
        ["Drive"] = "true",
        ["Network"] = "true",
        ["Battery"] = "true",
        ["ArgusMonitor"] = "true",
    };


    public ArgusMonitor(IMoBroService service, ILogger logger)
    {
        _service = service;
        _logger = logger;
    }

    public void UpdateSettings(IMoBroSettings settings)
    {
        foreach (KeyValuePair<string, string> setting in Settings)
        {
            argus_monitor.SetSensorEnabled(new StringBuilder(setting.Key), settings.GetValue(setting.Key, bool.Parse(setting.Value)));
        }
    }

    public void Start()
    {
        int errno = argus_monitor.Start();
        switch (errno)
        {
            case 0:
                return;
            case 10:
                _logger.LogError("ArgusMonitorLink instance already running, aborting...");
                throw new PluginException("ArgusMonitorLink instance already running, aborting...")
                    .AddDetail("timestamp", DateTime.UtcNow.ToString("o"))
                    .AddDetail("pluginVersion", "0.1.5");
            default:
                _logger.LogError("Plugin failed to start ArgusMonitorLink, error code '" + errno + "' , aborting...");
                throw new PluginException("Plugin failed to start ArgusMonitorLink, error code '" + errno + "' , aborting...")
                    .AddDetail("timestamp", DateTime.UtcNow.ToString("o"))
                    .AddDetail("pluginVersion", "0.1.5");
        }
    }

    public void WaitForConnection()
    {
        while (!argus_monitor.CheckConnection())
        {
            Thread.Sleep(50);
        }
    }

    public void WaitForData()
    {
        while (!argus_monitor.CheckData())
        {
            Thread.Sleep(50);
        }
    }

    public void Dispose()
    {
        int errno = argus_monitor.Stop();
        switch (errno)
        {
            case 0:
                return;
            case 10:
                _logger.LogError("ArgusMonitorInstance not running, aborting...");
                throw new PluginException("ArgusMonitorInstance not running, aborting...")
                    .AddDetail("timestamp", DateTime.UtcNow.ToString("o"))
                    .AddDetail("pluginVersion", "0.1.5");
            default:
                _logger.LogError("Plugin failed to stop ArgusMonitorLink, error code '" + errno + "'");
                throw new PluginException("Plugin failed to stop ArgusMonitorLink, error code '" + errno + "'")
                    .AddDetail("timestamp", DateTime.UtcNow.ToString("o"))
                    .AddDetail("pluginVersion", "0.1.5");
        }
    }

    public void RegisterCategories()
    {
        _service.Register(ArgusMonitorCategory.Synthetic);
    }

    public void RegisterItems()
    {
        bool cpu_enabled = argus_monitor.GetSensorEnabled(CPU);
        string[] sensor_data = SensorData();

        // to ensure we are only registering groups once
        List<string> groups = new();

        if (cpu_enabled)
        {
            groups.AddRange(new string[] { CommonGroup.CPU_Temperature.ToString(), CommonGroup.CPU_Multiplier.ToString(), CommonGroup.CPU_Core_Clock.ToString() });
            // basic groups
            _service.Register(MoBroItem.CreateGroup().WithId(CommonGroup.CPU_Temperature.ToString()).WithLabel("Temperature").Build());
            _service.Register(MoBroItem.CreateGroup().WithId(CommonGroup.CPU_Multiplier.ToString()).WithLabel("Multiplier").Build());
            _service.Register(MoBroItem.CreateGroup().WithId(CommonGroup.CPU_Core_Clock.ToString()).WithLabel("Core Clock").Build());


            // register some artificial metrics
            _service.Register(MoBroItem
                .CreateMetric()
                .WithId(ArgusMonitorUtilities.SanitizeId(CommonID.CPU_Temperature_CPU_Max))
                .WithLabel("CPU Temperature Max")
                .OfType(CoreMetricType.Temperature)
                .OfCategory(CoreCategory.Cpu)
                .OfGroup(ArgusMonitorUtilities.SanitizeId(CommonGroup.CPU_Temperature))
                .Build());

            _service.Register(MoBroItem
                .CreateMetric()
                .WithId(ArgusMonitorUtilities.SanitizeId(CommonID.CPU_Multiplier_CPU_Max))
                .WithLabel("CPU Multiplier Max")
                .OfType(CoreMetricType.Multiplier)
                .OfCategory(CoreCategory.Cpu)
                .OfGroup(ArgusMonitorUtilities.SanitizeId(CommonGroup.CPU_Multiplier))
                .Build());

            _service.Register(MoBroItem
                .CreateMetric()
                .WithId(ArgusMonitorUtilities.SanitizeId(CommonID.CPU_Core_Clock_CPU_Max))
                .WithLabel("CPU Clock Max")
                .OfType(CoreMetricType.Frequency)
                .OfCategory(CoreCategory.Cpu)
                .OfGroup(ArgusMonitorUtilities.SanitizeId(CommonGroup.CPU_Core_Clock))
                .Build());

            _service.Register(MoBroItem
                .CreateMetric()
                .WithId(ArgusMonitorUtilities.SanitizeId(CommonID.CPU_Temperature_CPU_Average))
                .WithLabel("CPU Temperature Average")
                .OfType(CoreMetricType.Temperature)
                .OfCategory(CoreCategory.Cpu)
                .OfGroup(ArgusMonitorUtilities.SanitizeId(CommonGroup.CPU_Temperature))
                .Build());

            _service.Register(MoBroItem
                .CreateMetric()
                .WithId(ArgusMonitorUtilities.SanitizeId(CommonID.CPU_Multiplier_CPU_Average))
                .WithLabel("CPU Multiplier Average")
                .OfType(CoreMetricType.Multiplier)
                .OfCategory(CoreCategory.Cpu)
                .OfGroup(ArgusMonitorUtilities.SanitizeId(CommonGroup.CPU_Multiplier))
                .Build());

            _service.Register(MoBroItem
                .CreateMetric()
                .WithId(ArgusMonitorUtilities.SanitizeId(CommonID.CPU_Core_Clock_CPU_Average))
                .WithLabel("CPU Clock Average")
                .OfType(CoreMetricType.Frequency)
                .OfCategory(CoreCategory.Cpu)
                .OfGroup(ArgusMonitorUtilities.SanitizeId(CommonGroup.CPU_Core_Clock))
                .Build());

            _service.Register(MoBroItem
                .CreateMetric()
                .WithId(ArgusMonitorUtilities.SanitizeId(CommonID.CPU_Temperature_CPU_Min))
                .WithLabel("CPU Temperature Min")
                .OfType(CoreMetricType.Temperature)
                .OfCategory(CoreCategory.Cpu)
                .OfGroup(ArgusMonitorUtilities.SanitizeId(CommonGroup.CPU_Temperature))
                .Build());

            _service.Register(MoBroItem
                .CreateMetric()
                .WithId(ArgusMonitorUtilities.SanitizeId(CommonID.CPU_Multiplier_CPU_Min))
                .WithLabel("CPU Multiplier Min")
                .OfType(CoreMetricType.Multiplier)
                .OfCategory(CoreCategory.Cpu)
                .OfGroup(ArgusMonitorUtilities.SanitizeId(CommonGroup.CPU_Multiplier))
                .Build());

            _service.Register(MoBroItem
                .CreateMetric()
                .WithId(ArgusMonitorUtilities.SanitizeId(CommonID.CPU_Core_Clock_CPU_Min))
                .WithLabel("CPU Clock Min")
                .OfType(CoreMetricType.Frequency)
                .OfCategory(CoreCategory.Cpu)
                .OfGroup(ArgusMonitorUtilities.SanitizeId(CommonGroup.CPU_Core_Clock))
                .Build());
        }


        // parse the data and then iterate over it to register all metrics and groups
        for (int i = 0; i < sensor_data.Length; ++i)
        {
            string sensor_string = sensor_data[i];
            if (sensor_string.Length == 0)
            {
                continue;
            }

            string[] sensor_values = sensor_string.Split(new string[] { "[<|>]" }, StringSplitOptions.None);

            if (sensor_values.Length < 5 || sensor_values[2] == "Invalid")
            {
                continue;
            }

            string group_id = ArgusMonitorUtilities.GroupID(sensor_values);

            if (!groups.Contains(group_id))
            {
                groups.Add(group_id);
                _service.Register(MoBroItem.CreateGroup().WithId(group_id).WithLabel(sensor_values[4]).Build());
            }

            CoreMetricType type = ArgusMonitorUtilities.GetMetricType(sensor_values[2]);
            CoreCategory category = ArgusMonitorUtilities.GetCategory(sensor_values[3]);

            MetricBuilder.ICategoryStage type_stage = MoBroItem
                .CreateMetric()
                .WithId(ArgusMonitorUtilities.SensorID(sensor_values))
                .WithLabel(sensor_values[0])
                .OfType(type);

            MetricBuilder.IGroupStage category_stage = sensor_values[3] == "ArgusMonitor"
                    ? type_stage.OfCategory(ArgusMonitorCategory.Synthetic)
                    : type_stage.OfCategory(category);

            MetricBuilder.IBuildStage group_stage = category_stage.OfGroup(group_id);

            if (sensor_values[2] == "Text")
            {
                _service.Register(group_stage.AsStaticValue().Build());
            }
            else
            {
                _service.Register(group_stage.Build());
            }


            // register artificial core clock metrics
            if (cpu_enabled
                && type == CoreMetricType.Multiplier
                && category == CoreCategory.Cpu
                && sensor_values[4] == "Multiplier")
            {
                MoBro.Plugin.SDK.Models.Metrics.Metric freq = MoBroItem
                    .CreateMetric()
                    .WithId(ArgusMonitorUtilities.CoreClockID(sensor_values))
                    .WithLabel(sensor_values[0] + " Clock")
                    .OfType(CoreMetricType.Frequency)
                    .OfCategory(category)
                    .OfGroup(ArgusMonitorUtilities.SanitizeId(CommonGroup.CPU_Core_Clock))
                    .Build();
                _service.Register(freq);
            }
        }
    }

    public void UpdateMetricValues()
    {
        double? fsb = null;
        List<double> cpu_temps = new();
        Dictionary<string, double> multipliers = new Dictionary<string, double>();

        string[] sensor_data = SensorData();

        for (int i = 0; i < sensor_data.Length; ++i)
        {
            string sensor_string = sensor_data[i];
            if (sensor_string.Length == 0)
            {
                continue;
            }

            string[] sensor_values = sensor_string.Split(new string[] { "[<|>]" }, StringSplitOptions.None);

            if (sensor_values.Length < 5 || sensor_values[2] == "Invalid")
            {
                continue;
            }

            object? value = ArgusMonitorUtilities.GetMetricValue(sensor_values[1], sensor_values[2]);

            if (value != null
                && argus_monitor.GetSensorEnabled(CPU)
                && ArgusMonitorUtilities.GetCategory(sensor_values[3]) == CoreCategory.Cpu)
            {
                CoreMetricType type = ArgusMonitorUtilities.GetMetricType(sensor_values[2]);

                if (type == CoreMetricType.Temperature && sensor_values[4] == "Temperature")
                {
                    cpu_temps.Add((double)value);
                }

                if (type == CoreMetricType.Multiplier && sensor_values[4] == "Multiplier")
                {
                    multipliers.Add(ArgusMonitorUtilities.CoreClockID(sensor_values), (double)value);
                }

                if (type == CoreMetricType.Frequency && sensor_values[4] == "FSB")
                {
                    fsb = (double)value;
                }
            }

            _service.UpdateMetricValue(ArgusMonitorUtilities.SensorID(sensor_values), value);
        }

        // if we have a fsb value, update the core clocks
        if (fsb != null)
        {
            foreach (KeyValuePair<string, double> multiplier in multipliers)
            {
                _service.UpdateMetricValue(multiplier.Key, multiplier.Value * fsb);
            }

            _service.UpdateMetricValue(ArgusMonitorUtilities.SanitizeId(CommonID.CPU_Multiplier_CPU_Max), multipliers.Values.Max());
            _service.UpdateMetricValue(ArgusMonitorUtilities.SanitizeId(CommonID.CPU_Core_Clock_CPU_Max), multipliers.Values.Max() * fsb);
            _service.UpdateMetricValue(ArgusMonitorUtilities.SanitizeId(CommonID.CPU_Multiplier_CPU_Average), multipliers.Values.Average());
            _service.UpdateMetricValue(ArgusMonitorUtilities.SanitizeId(CommonID.CPU_Core_Clock_CPU_Average), multipliers.Values.Average() * fsb);
            _service.UpdateMetricValue(ArgusMonitorUtilities.SanitizeId(CommonID.CPU_Multiplier_CPU_Min), multipliers.Values.Min());
            _service.UpdateMetricValue(ArgusMonitorUtilities.SanitizeId(CommonID.CPU_Core_Clock_CPU_Min), multipliers.Values.Min() * fsb);
        }

        // if we have cpu temp values, update the max
        if (cpu_temps.Count > 0)
        {
            _service.UpdateMetricValue(ArgusMonitorUtilities.SanitizeId(CommonID.CPU_Temperature_CPU_Max), cpu_temps.Max());
            _service.UpdateMetricValue(ArgusMonitorUtilities.SanitizeId(CommonID.CPU_Temperature_CPU_Average), cpu_temps.Average());
            _service.UpdateMetricValue(ArgusMonitorUtilities.SanitizeId(CommonID.CPU_Temperature_CPU_Min), cpu_temps.Min());
        }
    }

    private string[] SensorData()
    {
        argus_monitor.ParseSensorData();
        int data_length = argus_monitor.GetDataLength();
        StringBuilder buffer = new StringBuilder(data_length);
        argus_monitor.GetSensorData(buffer, data_length);
        string sensor_data = buffer.ToString();

        return sensor_data.Split(new string[] { "]<|>[" }, StringSplitOptions.None);
    }
}