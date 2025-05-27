using Microsoft.Extensions.Logging;
using MoBro.Plugin.SDK.Builders;
using MoBro.Plugin.SDK.Enums;
using MoBro.Plugin.SDK.Exceptions;
using MoBro.Plugin.SDK.Models.Categories;
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
                        .AddDetail("pluginVersion", "0.2.0");
                case 10:
                    _logger.LogError("Could not close handle on file, error code '{errno}'", errno);
                    throw new PluginException("Could not close handle on file, error code '" + errno + "'")
                        .AddDetail("timestamp", DateTime.UtcNow.ToString("o"))
                        .AddDetail("pluginVersion", "0.2.0");
                case 11:
                    _logger.LogError("Could not unmap view of file and close handle on file, error code '{errno}'", errno);
                    throw new PluginException("Could not unmap view of file and close handle on file, error code '" + errno + "'")
                        .AddDetail("timestamp", DateTime.UtcNow.ToString("o"))
                        .AddDetail("pluginVersion", "0.2.0");
                default:
                    _logger.LogError("Plugin failed to stop ArgusMonitorLink, error code '{errno}'", errno);
                    throw new PluginException("Plugin failed to stop ArgusMonitorLink, error code '" + errno + "'")
                        .AddDetail("timestamp", DateTime.UtcNow.ToString("o"))
                        .AddDetail("pluginVersion", "0.2.0");
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
        bool cpuEnabled = argus_monitor.IsHardwareEnabled(CPU);

        List<string> cpuIds = new();

        // to ensure we are only registering groups once
        List<string> groups = new();

        Dictionary<string, List<string[]>> gpus = new();

        void addGroup(string groupId, string groupName)
        {
            if (!groups.Contains(groupId))
            {
                groups.Add(groupId);
                _service.Register(MoBroItem.CreateGroup().WithId(groupId).WithLabel(groupName).Build());
            }
        }

        void register(string sensorName, string sensorValue, string sensorType, string hardwareType, string sensorGroup, string sensorIndex, string dataIndex)
        {
            CoreCategory category = ArgusMonitorUtilities.GetCategory(hardwareType);
            CoreMetricType type = ArgusMonitorUtilities.GetMetricType(sensorType);

            if (CoreCategory.Gpu == category)
            {
                if (!gpus.ContainsKey(sensorIndex))
                {
                    gpus[sensorIndex] = new();
                }
                gpus[sensorIndex].Add(new string[] { sensorName, sensorValue, sensorType, hardwareType, sensorGroup, sensorIndex, dataIndex });
                return;
            }

            string groupId = CoreCategory.Cpu == category ? ArgusMonitorUtilities.GroupID(hardwareType, sensorGroup, sensorIndex) : ArgusMonitorUtilities.GroupID(hardwareType, sensorGroup);

            string? cpuId = null;
            Category? cpu_category = null;

            if (CoreCategory.Cpu == category)
            {
                cpuId = sensorIndex.ToString();
                if (!cpuIds.Contains(cpuId))
                {
                    cpu_category = MoBroItem
                        .CreateCategory()
                        .WithId(ArgusMonitorUtilities.SanitizeId("CPU_" + sensorIndex))
                        .WithLabel("CPU [" + sensorIndex + "]")
                        .Build();
                    _service.Register(cpu_category);
                    cpuIds.Add(cpuId);
                }
            }

            addGroup(ArgusMonitorUtilities.SanitizeId(groupId), sensorGroup);

            MetricBuilder.ICategoryStage type_stage = MoBroItem
                .CreateMetric()
                .WithId(ArgusMonitorUtilities.SensorID(hardwareType, sensorType, sensorGroup, sensorName, sensorIndex, dataIndex))
                .WithLabel(CoreCategory.Network == category
                           ? dataIndex == "0"
                           ? sensorName + " (Up)"
                           : sensorName + " (Down)"
                           : sensorName)
                .OfType(type);

            MetricBuilder.IGroupStage category_stage = "ArgusMonitor" == hardwareType
                    ? type_stage.OfCategory(ArgusMonitorCategory.Synthetic)
                    : null != cpuId
                    ? type_stage.OfCategory("CPU_" + cpuId)
                    : type_stage.OfCategory(category);

            MetricBuilder.IBuildStage group_stage = category_stage.OfGroup(groupId);

            if (sensorType == "Text" || sensorName == "Available Sensors")
            {
                _service.Register(group_stage.AsStaticValue().Build());
                _service.UpdateMetricValue(ArgusMonitorUtilities.SensorID(hardwareType, sensorType, sensorGroup, sensorName, sensorIndex, dataIndex), sensorValue);
            }
            else
            {
                _service.Register(group_stage.Build());
            }


            // register artificial core clock metrics
            if (cpuEnabled
                && CoreMetricType.Multiplier == type
                && null != cpuId
                && sensorGroup == "Multiplier")
            {
                addGroup(ArgusMonitorUtilities.SanitizeId(CPU + "_" + CommonGroup.Temperature.ToString() + "_" + cpuId), "Temperature");
                addGroup(ArgusMonitorUtilities.SanitizeId(CPU + "_" + CommonGroup.Multiplier.ToString() + "_" + cpuId), "Multiplier");
                addGroup(ArgusMonitorUtilities.SanitizeId(CPU + "_" + CommonGroup.Core_Clock.ToString() + "_" + cpuId), "Core Clock");

                _service.Register(MoBroItem
                    .CreateMetric()
                    .WithId(ArgusMonitorUtilities.CoreClockID(hardwareType, sensorName, sensorIndex, dataIndex))
                    .WithLabel(sensorName + " Clock")
                    .OfType(CoreMetricType.Frequency)
                    .OfCategory("CPU_" + cpuId)
                    .OfGroup(ArgusMonitorUtilities.SanitizeId(CPU + "_" + CommonGroup.Core_Clock.ToString() + "_" + cpuId))
                    .Build());
            }
        }

        // Register all metrics
        argus_monitor.GetSensorData(register);

        foreach (KeyValuePair<string, List<string[]>> gpu in gpus)
        {
            Category? gpu_category = null;
            foreach (string[] sensor in gpu.Value)
            {
                // sensor: sensorName, sensorValue, sensorType, hardwareType, sensorGroup, sensorIndex, dataIndex
                if ("Name" == sensor[0])
                {
                    gpu_category = MoBroItem
                        .CreateCategory()
                        .WithId(ArgusMonitorUtilities.SanitizeId("GPU_" + sensor[5]))
                        .WithLabel(sensor[1])
                        .Build();
                    _service.Register(gpu_category);
                }
            }
            if (null != gpu_category)
            {
                foreach (string[] sensor in gpu.Value)
                {
                    string groupId = ArgusMonitorUtilities.GroupID(sensor[3], sensor[4], sensor[5]);

                    addGroup(ArgusMonitorUtilities.SanitizeId(groupId), sensor[4]);

                    MetricBuilder.IBuildStage group_stage = MoBroItem
                        .CreateMetric()
                        .WithId(ArgusMonitorUtilities.SensorID(sensor[3], sensor[2], sensor[4], sensor[0], sensor[5], sensor[6]))
                        .WithLabel(sensor[0])
                        .OfType(ArgusMonitorUtilities.GetMetricType(sensor[2]))
                        .OfCategory(gpu_category)
                        .OfGroup(groupId);

                    if ("Name" == sensor[0])
                    {
                        _service.Register(group_stage.AsStaticValue().Build());
                        _service.UpdateMetricValue(ArgusMonitorUtilities.SensorID(sensor[3], sensor[2], sensor[4], sensor[0], sensor[5], sensor[6]), sensor[1]);
                    }
                    else
                    {
                        _service.Register(group_stage.Build());
                    }
                }
            }
        }

        if (cpuEnabled)
        {
            foreach (string cpuId in cpuIds)
            {
                // register some artificial metrics
                _service.Register(MoBroItem
                    .CreateMetric()
                    .WithId(ArgusMonitorUtilities.SanitizeId(CommonID.CPU_Temperature_Temperature_Max.ToString() + "_" + cpuId))
                    .WithLabel("CPU Temperature Max")
                    .OfType(CoreMetricType.Temperature)
                    .OfCategory("CPU_" + cpuId)
                    .OfGroup(ArgusMonitorUtilities.SanitizeId(CPU + "_" + CommonGroup.Temperature.ToString() + "_" + cpuId))
                    .Build());

                _service.Register(MoBroItem
                    .CreateMetric()
                    .WithId(ArgusMonitorUtilities.SanitizeId(CommonID.CPU_Multiplier_Multiplier_Max.ToString() + "_" + cpuId))
                    .WithLabel("CPU Multiplier Max")
                    .OfType(CoreMetricType.Multiplier)
                    .OfCategory("CPU_" + cpuId)
                    .OfGroup(ArgusMonitorUtilities.SanitizeId(CPU + "_" + CommonGroup.Multiplier.ToString() + "_" + cpuId))
                    .Build());

                _service.Register(MoBroItem
                    .CreateMetric()
                    .WithId(ArgusMonitorUtilities.SanitizeId(CommonID.CPU_Frequency_Core_Clock_Max.ToString() + "_" + cpuId))
                    .WithLabel("CPU Clock Max")
                    .OfType(CoreMetricType.Frequency)
                    .OfCategory("CPU_" + cpuId)
                    .OfGroup(ArgusMonitorUtilities.SanitizeId(CPU + "_" + CommonGroup.Core_Clock.ToString() + "_" + cpuId))
                    .Build());

                _service.Register(MoBroItem
                    .CreateMetric()
                    .WithId(ArgusMonitorUtilities.SanitizeId(CommonID.CPU_Temperature_Temperature_Average.ToString() + "_" + cpuId))
                    .WithLabel("CPU Temperature Average")
                    .OfType(CoreMetricType.Temperature)
                    .OfCategory("CPU_" + cpuId)
                    .OfGroup(ArgusMonitorUtilities.SanitizeId(CPU + "_" + CommonGroup.Temperature.ToString() + "_" + cpuId))
                    .Build());

                _service.Register(MoBroItem
                    .CreateMetric()
                    .WithId(ArgusMonitorUtilities.SanitizeId(CommonID.CPU_Multiplier_Multiplier_Average.ToString() + "_" + cpuId))
                    .WithLabel("CPU Multiplier Average")
                    .OfType(CoreMetricType.Multiplier)
                    .OfCategory("CPU_" + cpuId)
                    .OfGroup(ArgusMonitorUtilities.SanitizeId(CPU + "_" + CommonGroup.Multiplier.ToString() + "_" + cpuId))
                    .Build());

                _service.Register(MoBroItem
                    .CreateMetric()
                    .WithId(ArgusMonitorUtilities.SanitizeId(CommonID.CPU_Frequency_Core_Clock_Average.ToString() + "_" + cpuId))
                    .WithLabel("CPU Clock Average")
                    .OfType(CoreMetricType.Frequency)
                    .OfCategory("CPU_" + cpuId)
                    .OfGroup(ArgusMonitorUtilities.SanitizeId(CPU + "_" + CommonGroup.Core_Clock.ToString() + "_" + cpuId))
                    .Build());

                _service.Register(MoBroItem
                    .CreateMetric()
                    .WithId(ArgusMonitorUtilities.SanitizeId(CommonID.CPU_Temperature_Temperature_Min.ToString() + "_" + cpuId))
                    .WithLabel("CPU Temperature Min")
                    .OfType(CoreMetricType.Temperature)
                    .OfCategory("CPU_" + cpuId)
                    .OfGroup(ArgusMonitorUtilities.SanitizeId(CPU + "_" + CommonGroup.Temperature.ToString() + "_" + cpuId))
                    .Build());

                _service.Register(MoBroItem
                    .CreateMetric()
                    .WithId(ArgusMonitorUtilities.SanitizeId(CommonID.CPU_Multiplier_Multiplier_Min.ToString() + "_" + cpuId))
                    .WithLabel("CPU Multiplier Min")
                    .OfType(CoreMetricType.Multiplier)
                    .OfCategory("CPU_" + cpuId)
                    .OfGroup(ArgusMonitorUtilities.SanitizeId(CPU + "_" + CommonGroup.Multiplier.ToString() + "_" + cpuId))
                    .Build());

                _service.Register(MoBroItem
                    .CreateMetric()
                    .WithId(ArgusMonitorUtilities.SanitizeId(CommonID.CPU_Frequency_Core_Clock_Min.ToString() + "_" + cpuId))
                    .WithLabel("CPU Clock Min")
                    .OfType(CoreMetricType.Frequency)
                    .OfCategory("CPU_" + cpuId)
                    .OfGroup(ArgusMonitorUtilities.SanitizeId(CPU + "_" + CommonGroup.Core_Clock.ToString() + "_" + cpuId))
                    .Build());
            }
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