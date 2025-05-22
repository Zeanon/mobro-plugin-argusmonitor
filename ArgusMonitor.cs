using Microsoft.Extensions.Logging;
using MoBro.Plugin.SDK.Builders;
using MoBro.Plugin.SDK.Enums;
using MoBro.Plugin.SDK.Exceptions;
using MoBro.Plugin.SDK.Services;
using System;
using System.Collections.Generic;
using System.Threading;
using Zeanon.Plugin.ArgusMonitor.CustomItem;
using Zeanon.Plugin.ArgusMonitor.Enums;
using Zeanon.Plugin.ArgusMonitor.Utility;


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

    public void Open(int pollingInterval)
    {
        if (argus_monitor != IntPtr.Zero)
        {
            while (argus_monitor.Open() != 0)
            {
                Thread.Sleep(pollingInterval);
            }
        }
    }

    public void WaitForArgus(int pollingInterval)
    {
        while (!argus_monitor.CheckArgusSignature())
        {
            Thread.Sleep(pollingInterval);
        }
    }

    public void InitTotalSensorCount(int pollingInterval)
    {
        while (argus_monitor.GetTotalSensorCount() == 0)
        {
            Thread.Sleep(pollingInterval);
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
                        .AddDetail("pluginVersion", "0.1.9");
                case 10:
                    _logger.LogError("Could not close handle on file, error code '{errno}'", errno);
                    throw new PluginException("Could not close handle on file, error code '" + errno + "'")
                        .AddDetail("timestamp", DateTime.UtcNow.ToString("o"))
                        .AddDetail("pluginVersion", "0.1.9");
                case 11:
                    _logger.LogError("Could not unmap view of file and close handle on file, error code '{errno}'", errno);
                    throw new PluginException("Could not unmap view of file and close handle on file, error code '" + errno + "'")
                        .AddDetail("timestamp", DateTime.UtcNow.ToString("o"))
                        .AddDetail("pluginVersion", "0.1.9");
                default:
                    _logger.LogError("Plugin failed to stop ArgusMonitorLink, error code '{errno}'", errno);
                    throw new PluginException("Plugin failed to stop ArgusMonitorLink, error code '" + errno + "'")
                        .AddDetail("timestamp", DateTime.UtcNow.ToString("o"))
                        .AddDetail("pluginVersion", "0.1.9");
            }
        }
    }

    public void RegisterCategories()
    {
        if (argus_monitor.IsHardwareEnabled("ArgusMonitor"))
        {
            _service.Register(ArgusMonitorCategory.Synthetic);
        }
    }

    public void RegisterItems(int pollingInterval)
    {
        bool cpuValues = false;
        bool cpuEnabled = argus_monitor.IsHardwareEnabled(CPU);

        // to ensure we are only registering groups once
        List<string> groups = new();

        void addGroup(string groupId, string groupName)
        {
            if (!groups.Contains(groupId))
            {
                groups.Add(groupId);
                _service.Register(MoBroItem.CreateGroup().WithId(groupId).WithLabel(groupName).Build());
            }
        }

        void register(string sensorName, string sensor_value, string sensorType, string hardwareType, string sensorGroup)
        {
            string groupId = ArgusMonitorUtilities.GroupID(hardwareType, sensorGroup);

            addGroup(ArgusMonitorUtilities.SanitizeId(groupId), sensorGroup);

            CoreMetricType type = ArgusMonitorUtilities.GetMetricType(sensorType);
            CoreCategory category = ArgusMonitorUtilities.GetCategory(hardwareType);

            MetricBuilder.ICategoryStage type_stage = MoBroItem
                .CreateMetric()
                .WithId(ArgusMonitorUtilities.SensorID(hardwareType, sensorType, sensorGroup, sensorName))
                .WithLabel(sensorName)
                .OfType(type);

            MetricBuilder.IGroupStage category_stage = hardwareType == "ArgusMonitor"
                    ? type_stage.OfCategory(ArgusMonitorCategory.Synthetic)
                    : type_stage.OfCategory(category);

            MetricBuilder.IBuildStage group_stage = category_stage.OfGroup(groupId);

            if (sensorType == "Text" || sensorName == "Available Sensors")
            {
                _service.Register(group_stage.AsStaticValue().Build());
                _service.UpdateMetricValue(ArgusMonitorUtilities.SensorID(hardwareType, sensorType, sensorGroup, sensorName), sensor_value);
            }
            else
            {
                _service.Register(group_stage.Build());
            }


            // register artificial core clock metrics
            if (cpuEnabled
                && type == CoreMetricType.Multiplier
                && category == CoreCategory.Cpu
                && sensorGroup == "Multiplier")
            {
                cpuValues = true;

                addGroup(ArgusMonitorUtilities.SanitizeId(CPU + "_" + CommonGroup.Temperature.ToString()), "Temperature");
                addGroup(ArgusMonitorUtilities.SanitizeId(CPU + "_" + CommonGroup.Multiplier.ToString()), "Multiplier");
                addGroup(ArgusMonitorUtilities.SanitizeId(CPU + "_" + CommonGroup.Core_Clock.ToString()), "Core Clock");

                _service.Register(MoBroItem
                    .CreateMetric()
                    .WithId(ArgusMonitorUtilities.CoreClockID(hardwareType, sensorName))
                    .WithLabel(sensorName + " Clock")
                    .OfType(CoreMetricType.Frequency)
                    .OfCategory(category)
                    .OfGroup(ArgusMonitorUtilities.SanitizeId(CPU + "_" + CommonGroup.Core_Clock))
                    .Build());
            }
        }

        // Register all metrics
        argus_monitor.GetSensorData(register);

        if (cpuEnabled && cpuValues)
        {
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
    }

    public void UpdateMetricValues()
    {
        void update(string sensorId, string sensorValue)
        {
            _service.UpdateMetricValue(ArgusMonitorUtilities.SanitizeId(sensorId), sensorValue);
        }

        argus_monitor.UpdateSensorData(update);
    }
}