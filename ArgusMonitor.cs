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
    private readonly IntPtr _argus_monitor;

    private readonly IMoBroService _service;
    private readonly ILogger _logger;
    private readonly ArgusMonitorWrapper.UpdateFloat _updateFloat;
    private readonly ArgusMonitorWrapper.UpdateText _updateText;


    private static readonly string CPU = "CPU";

    public static readonly Dictionary<string, string> Settings = new()
    {
        ["CPU"] = "true",
        ["GPU"] = "true",
        ["RAM"] = "true",
        ["Fan"] = "true",
        ["Drive"] = "true",
        ["Network"] = "true",
        ["Battery"] = "true",
        ["Temperature"] = "true",
        ["ArgusMonitor"] = "true",
    };


    public ArgusMonitor(IMoBroService service, ILogger logger)
    {
        _service = service;
        _logger = logger;
        _updateFloat = (string sensorId, float sensorValue) =>
        {
            _service.UpdateMetricValue(ArgusMonitorUtilities.SanitizeId(sensorId), sensorValue);
        };
        _updateText = (string sensorId, string sensorValue) =>
        {
            _service.UpdateMetricValue(ArgusMonitorUtilities.SanitizeId(sensorId), sensorValue);
        };
        _argus_monitor = ArgusMonitorWrapper.Instantiate();
    }

    public void UpdateSettings(IMoBroSettings settings)
    {
        foreach (KeyValuePair<string, string> setting in Settings)
        {
            _argus_monitor.SetHardwareEnabled(setting.Key, settings.GetValue(setting.Key, bool.Parse(setting.Value)));
        }
    }

    public void Open(int pollingInterval)
    {
        while (_argus_monitor.Open() != 0)
        {
            Thread.Sleep(pollingInterval);
        }
    }

    public void WaitForArgus(int pollingInterval)
    {
        while (!_argus_monitor.CheckArgusSignature())
        {
            Thread.Sleep(pollingInterval);
        }
    }

    public void WaitForSensors(int pollingInterval)
    {
        while (_argus_monitor.GetTotalSensorCount() == 0)
        {
            Thread.Sleep(pollingInterval);
        }
    }

    public void Dispose()
    {
        if (_argus_monitor != IntPtr.Zero)
        {
            int errno = _argus_monitor.Close();

            switch (errno)
            {
                case 0:
                    return;
                case 1:
                    _logger.LogError("Could not unmap view of file, error code '{errno}'", errno);
                    throw new PluginException("Could not unmap view of file, error code '" + errno + "'")
                        .AddDetail("timestamp", DateTime.UtcNow.ToString("o"))
                        .AddDetail("pluginVersion", Plugin.VERSION);
                case 10:
                    _logger.LogError("Could not close handle on file, error code '{errno}'", errno);
                    throw new PluginException("Could not close handle on file, error code '" + errno + "'")
                        .AddDetail("timestamp", DateTime.UtcNow.ToString("o"))
                        .AddDetail("pluginVersion", Plugin.VERSION);
                case 11:
                    _logger.LogError("Could not unmap view of file and close handle on file, error code '{errno}'", errno);
                    throw new PluginException("Could not unmap view of file and close handle on file, error code '" + errno + "'")
                        .AddDetail("timestamp", DateTime.UtcNow.ToString("o"))
                        .AddDetail("pluginVersion", Plugin.VERSION);
                default:
                    _logger.LogError("Plugin failed to stop ArgusMonitorLink, error code '{errno}'", errno);
                    throw new PluginException("Plugin failed to stop ArgusMonitorLink, error code '" + errno + "'")
                        .AddDetail("timestamp", DateTime.UtcNow.ToString("o"))
                        .AddDetail("pluginVersion", Plugin.VERSION);
            }
        }

        _argus_monitor.Destroy();
    }

    public void RegisterCategories()
    {
        if (_argus_monitor.IsHardwareEnabled("ArgusMonitor"))
        {
            _service.Register(ArgusMonitorCategory.ArgusMonitor);
        }

        if (_argus_monitor.IsHardwareEnabled("Temperature"))
        {
            _service.Register(ArgusMonitorCategory.Temperature);
        }
    }

    public List<Action> RegisterItems()
    {
        bool cpuEnabled = _argus_monitor.IsHardwareEnabled(CPU);

        List<Action> toUpdate = new();

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

        void register(string sensorName,
                      string sensorValue,
                      string sensorType,
                      string hardwareType,
                      string sensorGroup,
                      string sensorIndex,
                      string dataIndex)
        {
            CoreCategory category = ArgusMonitorUtilities.GetCategory(hardwareType);
            CoreMetricType type = ArgusMonitorUtilities.GetMetricType(sensorType);

            if (CoreCategory.Gpu == category)
            {
                if (!gpus.ContainsKey(sensorIndex))
                {
                    gpus[sensorIndex] = new();
                }
                gpus[sensorIndex].Add(new string[] { sensorName,
                                                     sensorValue,
                                                     sensorType,
                                                     hardwareType,
                                                     sensorGroup,
                                                     sensorIndex,
                                                     dataIndex });
                return;
            }

            string groupId = CoreCategory.Cpu == category
                ? ArgusMonitorUtilities.GroupID(hardwareType, sensorGroup, sensorIndex)
                : ArgusMonitorUtilities.GroupID(hardwareType, sensorGroup);

            string? cpuId = null;

            if (CoreCategory.Cpu == category)
            {
                cpuId = sensorIndex.ToString();
                if (!cpuIds.Contains(cpuId))
                {
                    _service.Register(MoBroItem
                        .CreateCategory()
                        .WithId(ArgusMonitorUtilities.SanitizeId("CPU_" + sensorIndex))
                        .WithLabel("CPU [" + sensorIndex + "]")
                        .Build());
                    cpuIds.Add(cpuId);
                }
            }

            addGroup(ArgusMonitorUtilities.SanitizeId(groupId), sensorGroup);

            MetricBuilder.ICategoryStage typeStage = MoBroItem
                .CreateMetric()
                .WithId(ArgusMonitorUtilities.SensorID(hardwareType, sensorType, sensorGroup, sensorIndex, dataIndex))
                .WithLabel(CoreCategory.Network == category
                           ? dataIndex == "0"
                           ? sensorName + " (Up)"
                           : sensorName + " (Down)"
                           : sensorName)
                .OfType(type);

            MetricBuilder.IGroupStage categoryStage =
                      "ArgusMonitor" == hardwareType
                        ? typeStage.OfCategory(ArgusMonitorCategory.ArgusMonitor)
                    : "Temperature" == hardwareType
                        ? typeStage.OfCategory(ArgusMonitorCategory.Temperature)
                    : null != cpuId
                        ? typeStage.OfCategory("CPU_" + cpuId)
                        : typeStage.OfCategory(category);

            MetricBuilder.IBuildStage groupStage = categoryStage.OfGroup(groupId);

            if (sensorType == "Text")
            {
                _service.Register(groupStage.AsStaticValue().Build());
                toUpdate.Add(() =>
                {
                    _service.UpdateMetricValue(ArgusMonitorUtilities.SensorID(hardwareType,
                                                                              sensorType,
                                                                              sensorGroup,
                                                                              sensorIndex,
                                                                              dataIndex),
                                               sensorValue);
                });
            }
            else
            {
                _service.Register(groupStage.Build());
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
                    .WithId(ArgusMonitorUtilities.CoreClockID(hardwareType, sensorIndex, dataIndex))
                    .WithLabel(sensorName + " Clock")
                    .OfType(CoreMetricType.Frequency)
                    .OfCategory("CPU_" + cpuId)
                    .OfGroup(ArgusMonitorUtilities.SanitizeId(CPU + "_" + CommonGroup.Core_Clock.ToString() + "_" + cpuId))
                    .Build());
            }
        }

        // Register all metrics
        _argus_monitor.GetSensorData(register);

        foreach (KeyValuePair<string, List<string[]>> gpu in gpus)
        {
            Category? gpuCategory = null;
            foreach (string[] sensor in gpu.Value)
            {
                // sensor: sensorName, sensorValue, sensorType, hardwareType, sensorGroup, sensorIndex, dataIndex
                if ("Name" == sensor[0])
                {
                    gpuCategory = MoBroItem
                        .CreateCategory()
                        .WithId(ArgusMonitorUtilities.SanitizeId("GPU_" + sensor[5]))
                        .WithLabel(sensor[1])
                        .Build();
                    _service.Register(gpuCategory);
                }
            }
            if (null != gpuCategory)
            {
                foreach (string[] sensor in gpu.Value)
                {
                    string groupId = ArgusMonitorUtilities.GroupID(sensor[3], sensor[4], sensor[5]);

                    addGroup(ArgusMonitorUtilities.SanitizeId(groupId), sensor[4]);

                    MetricBuilder.IBuildStage groupStage = MoBroItem
                        .CreateMetric()
                        .WithId(ArgusMonitorUtilities.SensorID(sensor[3], sensor[2], sensor[4], sensor[5], sensor[6]))
                        .WithLabel(sensor[0])
                        .OfType(ArgusMonitorUtilities.GetMetricType(sensor[2]))
                        .OfCategory(gpuCategory)
                        .OfGroup(groupId);

                    if ("Name" == sensor[0])
                    {
                        _service.Register(groupStage.AsStaticValue().Build());
                        toUpdate.Add(() =>
                        {
                            _service.UpdateMetricValue(ArgusMonitorUtilities.SensorID(sensor[3],
                                                                                      sensor[2],
                                                                                      sensor[4],
                                                                                      sensor[5],
                                                                                      sensor[6]),
                                                       sensor[1]);
                        });
                    }
                    else
                    {
                        _service.Register(groupStage.Build());
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

        return toUpdate;
    }

    public void UpdateMetricValues()
    {
        _argus_monitor.UpdateSensorData(_updateFloat, _updateText);
    }
}