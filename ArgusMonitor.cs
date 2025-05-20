using Microsoft.Extensions.Logging;
using MoBro.Plugin.SDK.Builders;
using MoBro.Plugin.SDK.Enums;
using MoBro.Plugin.SDK.Exceptions;
using MoBro.Plugin.SDK.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Zeanon.Plugin.ArgusMonitor.CustomItem;
using Zeanon.Plugin.ArgusMonitor.Enums;
using Zeanon.Plugin.ArgusMonitor.Utility;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Zeanon.Plugin.ArgusMonitor;


public class ArgusMonitor : IDisposable
{
    private readonly IntPtr argus_monitor;

    private int total_sensor_count = 0;

    private readonly IMoBroService _service;
    private readonly ILogger _logger;
    private static readonly string CPU = "CPU";

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
        argus_monitor = ArgusMonitorWrapper.Instantiate();
    }

    public void UpdateSettings(IMoBroSettings settings)
    {
        foreach (KeyValuePair<string, string> setting in Settings)
        {
            argus_monitor.SetHardwareEnabled(setting.Key, settings.GetValue(setting.Key, bool.Parse(setting.Value)));
        }
    }

    public void Open(int polling_interval)
    {
        if (argus_monitor != IntPtr.Zero)
        {
            int errno = argus_monitor.Open();

            switch (errno)
            {
                case 0:
                    return;
                case 1:
                    _logger.LogError("Could not open file mapping, error code '{errno}'", errno);
                    throw new PluginException("Could not open file mapping, error code '" + errno + "'")
                        .AddDetail("timestamp", DateTime.UtcNow.ToString("o"))
                        .AddDetail("pluginVersion", "0.1.8");
                case 10:
                    _logger.LogError("Could not obtain fileview, error code '{errno}'", errno);
                    throw new PluginException("Could not obtain fileview, error code '" + errno + "'")
                        .AddDetail("timestamp", DateTime.UtcNow.ToString("o"))
                        .AddDetail("pluginVersion", "0.1.8");
                default:
                    _logger.LogError("Plugin failed to open ArgusMonitorLink, error code '{errno}'", errno);
                    throw new PluginException("Plugin failed to open ArgusMonitorLink, error code '" + errno + "'")
                        .AddDetail("timestamp", DateTime.UtcNow.ToString("o"))
                        .AddDetail("pluginVersion", "0.1.8");
            }
        }
    }

    public void WaitForArgus(int polling_interval)
    {
        while (!argus_monitor.CheckArgusSignature())
        {
            Thread.Sleep(polling_interval);
        }
    }

    public void InitTotalSensorCount(int polling_interval)
    {
        while (argus_monitor.GetTotalSensorCount() == 0)
        {
            Thread.Sleep(polling_interval);
        }
        total_sensor_count = argus_monitor.GetTotalSensorCount();
    }

    public void Dispose()
    {
        if (argus_monitor != IntPtr.Zero)
        {
            int errno = argus_monitor.Close();

            switch (errno)
            {
                case 0:
                    return;
                case 1:
                    _logger.LogError("Could not unmap view of file, error code '{errno}'", errno);
                    throw new PluginException("Could not unmap view of file, error code '" + errno + "'")
                        .AddDetail("timestamp", DateTime.UtcNow.ToString("o"))
                        .AddDetail("pluginVersion", "0.1.8");
                case 10:
                    _logger.LogError("Could not close handle on file, error code '{errno}'", errno);
                    throw new PluginException("Could not close handle on file, error code '" + errno + "'")
                        .AddDetail("timestamp", DateTime.UtcNow.ToString("o"))
                        .AddDetail("pluginVersion", "0.1.8");
                case 11:
                    _logger.LogError("Could not unmap view of file and close handle on file, error code '{errno}'", errno);
                    throw new PluginException("Could not unmap view of file and close handle on file, error code '" + errno + "'")
                        .AddDetail("timestamp", DateTime.UtcNow.ToString("o"))
                        .AddDetail("pluginVersion", "0.1.8");
                default:
                    _logger.LogError("Plugin failed to stop ArgusMonitorLink, error code '{errno}'", errno);
                    throw new PluginException("Plugin failed to stop ArgusMonitorLink, error code '" + errno + "'")
                        .AddDetail("timestamp", DateTime.UtcNow.ToString("o"))
                        .AddDetail("pluginVersion", "0.1.8");
            }
        }
    }

    public void RegisterCategories()
    {
        if (argus_monitor.GetHardwareEnabled("ArgusMonitor"))
        {
            _service.Register(ArgusMonitorCategory.Synthetic);
        }
    }

    public void RegisterItems(int polling_interval)
    {
        bool cpu_enabled = argus_monitor.GetHardwareEnabled(CPU);

        // to ensure we are only registering groups once
        List<string> groups = new();

        void addGroup(string group_id, string group_name)
        {
            if (!groups.Contains(group_id))
            {
                groups.Add(group_id);
                _service.Register(MoBroItem.CreateGroup().WithId(ArgusMonitorUtilities.SanitizeId(group_id)).WithLabel(group_name).Build());
            }
        }

        if (cpu_enabled)
        {
            addGroup(CPU + "_" + CommonGroup.Temperature.ToString(), "Temperature");
            addGroup(CPU + "_" + CommonGroup.Multiplier.ToString(), "Multiplier");
            addGroup(CPU + "_" + CommonGroup.Core_Clock.ToString(), "Core Clock");

            // register some artificial metrics
            _service.Register(MoBroItem
                .CreateMetric()
                .WithId(ArgusMonitorUtilities.SanitizeId(CommonID.CPU_Temperature_Temperature_Max))
                .WithLabel("CPU Temperature Max")
                .OfType(CoreMetricType.Temperature)
                .OfCategory(CoreCategory.Cpu)
                .OfGroup(ArgusMonitorUtilities.SanitizeId(CPU + "_" + CommonGroup.Temperature))
                .Build());

            _service.Register(MoBroItem
                .CreateMetric()
                .WithId(ArgusMonitorUtilities.SanitizeId(CommonID.CPU_Multiplier_Multiplier_Max))
                .WithLabel("CPU Multiplier Max")
                .OfType(CoreMetricType.Multiplier)
                .OfCategory(CoreCategory.Cpu)
                .OfGroup(ArgusMonitorUtilities.SanitizeId(CPU + "_" + CommonGroup.Multiplier))
                .Build());

            _service.Register(MoBroItem
                .CreateMetric()
                .WithId(ArgusMonitorUtilities.SanitizeId(CommonID.CPU_Frequency_Core_Clock_Max))
                .WithLabel("CPU Clock Max")
                .OfType(CoreMetricType.Frequency)
                .OfCategory(CoreCategory.Cpu)
                .OfGroup(ArgusMonitorUtilities.SanitizeId(CPU + "_" + CommonGroup.Core_Clock))
                .Build());

            _service.Register(MoBroItem
                .CreateMetric()
                .WithId(ArgusMonitorUtilities.SanitizeId(CommonID.CPU_Temperature_Temperature_Average))
                .WithLabel("CPU Temperature Average")
                .OfType(CoreMetricType.Temperature)
                .OfCategory(CoreCategory.Cpu)
                .OfGroup(ArgusMonitorUtilities.SanitizeId(CPU + "_" + CommonGroup.Temperature))
                .Build());

            _service.Register(MoBroItem
                .CreateMetric()
                .WithId(ArgusMonitorUtilities.SanitizeId(CommonID.CPU_Multiplier_Multiplier_Average))
                .WithLabel("CPU Multiplier Average")
                .OfType(CoreMetricType.Multiplier)
                .OfCategory(CoreCategory.Cpu)
                .OfGroup(ArgusMonitorUtilities.SanitizeId(CPU + "_" + CommonGroup.Multiplier))
                .Build());

            _service.Register(MoBroItem
                .CreateMetric()
                .WithId(ArgusMonitorUtilities.SanitizeId(CommonID.CPU_Frequency_Core_Clock_Average))
                .WithLabel("CPU Clock Average")
                .OfType(CoreMetricType.Frequency)
                .OfCategory(CoreCategory.Cpu)
                .OfGroup(ArgusMonitorUtilities.SanitizeId(CPU + "_" + CommonGroup.Core_Clock))
                .Build());

            _service.Register(MoBroItem
                .CreateMetric()
                .WithId(ArgusMonitorUtilities.SanitizeId(CommonID.CPU_Temperature_Temperature_Min))
                .WithLabel("CPU Temperature Min")
                .OfType(CoreMetricType.Temperature)
                .OfCategory(CoreCategory.Cpu)
                .OfGroup(ArgusMonitorUtilities.SanitizeId(CPU + "_" + CommonGroup.Temperature))
                .Build());

            _service.Register(MoBroItem
                .CreateMetric()
                .WithId(ArgusMonitorUtilities.SanitizeId(CommonID.CPU_Multiplier_Multiplier_Min))
                .WithLabel("CPU Multiplier Min")
                .OfType(CoreMetricType.Multiplier)
                .OfCategory(CoreCategory.Cpu)
                .OfGroup(ArgusMonitorUtilities.SanitizeId(CPU + "_" + CommonGroup.Multiplier))
                .Build());

            _service.Register(MoBroItem
                .CreateMetric()
                .WithId(ArgusMonitorUtilities.SanitizeId(CommonID.CPU_Frequency_Core_Clock_Min))
                .WithLabel("CPU Clock Min")
                .OfType(CoreMetricType.Frequency)
                .OfCategory(CoreCategory.Cpu)
                .OfGroup(ArgusMonitorUtilities.SanitizeId(CPU + "_" + CommonGroup.Core_Clock))
                .Build());
        }

        // get the data and then iterate over it to register all metrics and groups
        var sensor_data = SensorData();
        foreach (string[] sensor_values in sensor_data)
        {
            if (sensor_values.Length < 5 || sensor_values[2] == "Invalid")
            {
                continue;
            }

            string group_id = ArgusMonitorUtilities.GroupID(sensor_values);

            addGroup(group_id, sensor_values[4]);

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
                _service.Register(MoBroItem
                    .CreateMetric()
                    .WithId(ArgusMonitorUtilities.CoreClockID(sensor_values))
                    .WithLabel(sensor_values[0] + " Clock")
                    .OfType(CoreMetricType.Frequency)
                    .OfCategory(category)
                    .OfGroup(ArgusMonitorUtilities.SanitizeId(CPU + "_" + CommonGroup.Core_Clock))
                    .Build());
            }
        }
    }

    public void UpdateMetricValues()
    {
        double? fsb = null;
        List<double> cpu_temps = new();
        Dictionary<string, double> multipliers = new();

        foreach (string[] sensor_values in SensorData())
        {
            if (sensor_values.Length < 5 || sensor_values[2] == "Invalid")
            {
                continue;
            }

            object? value = ArgusMonitorUtilities.GetMetricValue(sensor_values[1], sensor_values[2]);

            if (value != null
                && ArgusMonitorUtilities.GetCategory(sensor_values[3]) == CoreCategory.Cpu
                && argus_monitor.GetHardwareEnabled(CPU))
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

        // if we have a fsb value and multipliers, update the core clocks
        if (fsb != null && multipliers.Count > 0)
        {
            foreach (KeyValuePair<string, double> multiplier in multipliers)
            {
                _service.UpdateMetricValue(multiplier.Key, multiplier.Value * fsb);
            }

            _service.UpdateMetricValue(ArgusMonitorUtilities.SanitizeId(CommonID.CPU_Multiplier_Multiplier_Max), multipliers.Values.Max());
            _service.UpdateMetricValue(ArgusMonitorUtilities.SanitizeId(CommonID.CPU_Frequency_Core_Clock_Max), multipliers.Values.Max() * fsb);
            _service.UpdateMetricValue(ArgusMonitorUtilities.SanitizeId(CommonID.CPU_Multiplier_Multiplier_Average), multipliers.Values.Average());
            _service.UpdateMetricValue(ArgusMonitorUtilities.SanitizeId(CommonID.CPU_Frequency_Core_Clock_Average), multipliers.Values.Average() * fsb);
            _service.UpdateMetricValue(ArgusMonitorUtilities.SanitizeId(CommonID.CPU_Multiplier_Multiplier_Min), multipliers.Values.Min());
            _service.UpdateMetricValue(ArgusMonitorUtilities.SanitizeId(CommonID.CPU_Frequency_Core_Clock_Min), multipliers.Values.Min() * fsb);
        }

        // if we have cpu temp values, update the max
        if (cpu_temps.Count > 0)
        {
            _service.UpdateMetricValue(ArgusMonitorUtilities.SanitizeId(CommonID.CPU_Temperature_Temperature_Max), cpu_temps.Max());
            _service.UpdateMetricValue(ArgusMonitorUtilities.SanitizeId(CommonID.CPU_Temperature_Temperature_Average), cpu_temps.Average());
            _service.UpdateMetricValue(ArgusMonitorUtilities.SanitizeId(CommonID.CPU_Temperature_Temperature_Min), cpu_temps.Min());
        }
    }

    private List<string[]> SensorData()
    {
        List<string[]> argus_monitor_data = new(total_sensor_count);
        argus_monitor.GetSensorData(argus_monitor_data.Add);
        return argus_monitor_data;
    }
}