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
    private readonly IntPtr _argusMonitorLink;

    private readonly IMoBroService _service;
    private readonly ILogger _logger;
    private readonly ArgusMonitorWrapper.UpdateFloat _updateFloat;

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
        _argusMonitorLink = ArgusMonitorWrapper.Create();
    }

    public void UpdateSettings(IMoBroSettings settings)
    {
        foreach (KeyValuePair<string, string> setting in Settings)
        {
            _argusMonitorLink.SetHardwareEnabled(setting.Key, settings.GetValue(setting.Key, bool.Parse(setting.Value)));
        }
    }

    public void Open(int pollingInterval)
    {
        while (0 != _argusMonitorLink.Open())
        {
            Thread.Sleep(pollingInterval);
        }
    }

    public void WaitForArgus(int pollingInterval)
    {
        while (!_argusMonitorLink.CheckArgusSignature())
        {
            Thread.Sleep(pollingInterval);
        }
    }

    public void WaitForSensors(int pollingInterval)
    {
        while (0 == _argusMonitorLink.GetTotalSensorCount())
        {
            Thread.Sleep(pollingInterval);
        }
    }

    public void Dispose()
    {
        if (IntPtr.Zero != _argusMonitorLink)
        {
            int errno = _argusMonitorLink.Close();

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

        _argusMonitorLink.Destroy();
    }

    public void RegisterCategories()
    {
        if (_argusMonitorLink.IsHardwareEnabled("ArgusMonitor"))
        {
            _service.Register(ArgusMonitorCategory.ArgusMonitor);
        }

        if (_argusMonitorLink.IsHardwareEnabled("Temperature"))
        {
            _service.Register(ArgusMonitorCategory.Temperature);
        }
    }

    public void RegisterItems(ICollection<Action> toUpdate)
    {
        bool cpuEnabled = _argusMonitorLink.IsHardwareEnabled(CPU);

        List<string> cpuIds = [];

        // to ensure we are only registering groups once
        List<string> groups = [];

        Dictionary<int, List<string[]>> gpus = new();

        void addGroup(string groupId, string groupName)
        {
            if (!groups.Contains(groupId))
            {
                groups.Add(groupId);
                _service.Register(MoBroItem.CreateGroup().WithId(groupId).WithLabel(groupName).Build());
            }
        }

        void register(string sensorId,
                      string sensorName,
                      string sensorValue,
                      string sensorType,
                      string hardwareType,
                      string sensorGroup,
                      int sensorIndex,
                      int dataIndex,
                      bool staticValue)
        {
            CoreCategory category = ArgusMonitorUtilities.GetCategory(hardwareType);
            CoreMetricType type = ArgusMonitorUtilities.GetMetricType(sensorType);

            if (CoreCategory.Gpu == category)
            {
                if (!gpus.ContainsKey(sensorIndex))
                {
                    gpus[sensorIndex] = [];
                }
                gpus[sensorIndex].Add([ sensorId,
                                        sensorName,
                                        sensorValue,
                                        sensorType,
                                        hardwareType,
                                        sensorGroup,
                                        sensorIndex.ToString() ]);
                return;
            }

            string groupId =
                CoreCategory.Cpu == category
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
                .WithId(ArgusMonitorUtilities.SanitizeId(sensorId))
                .WithLabel(CoreCategory.Network == category
                           ? dataIndex == 0
                           ? sensorName + " (Upload)"
                           : sensorName + " (Download)"
                           : ArgusMonitorUtilities.SanitizeName(sensorName, type, category))
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

            if (staticValue)
            {
                _service.Register(groupStage.AsStaticValue().Build());
                toUpdate.Add(() =>
                {
                    _service.UpdateMetricValue(ArgusMonitorUtilities.SanitizeId(sensorId),
                                               sensorValue);
                });
            }
            else
            {
                _service.Register(groupStage.Build());
                try
                {
                    if (Convert.ToDouble(sensorValue) == 0)
                    {
                        toUpdate.Add(() =>
                        {
                            _service.UpdateMetricValue(ArgusMonitorUtilities.SanitizeId(sensorId),
                                                       sensorValue);
                        });
                    }
                }
                catch (System.FormatException) { }
            }


            // register artificial core clock metrics
            if (cpuEnabled
                && CoreMetricType.Multiplier == type
                && null != cpuId
                && "Multiplier" == sensorGroup)
            {
                addGroup(ArgusMonitorUtilities.SanitizeId(CPU + "_" + CommonGroup.Temperature.ToString() + "_" + cpuId), "Temperature");
                addGroup(ArgusMonitorUtilities.SanitizeId(CPU + "_" + CommonGroup.Multiplier.ToString() + "_" + cpuId), "Multiplier");
                addGroup(ArgusMonitorUtilities.SanitizeId(CPU + "_" + CommonGroup.Core_Clock.ToString() + "_" + cpuId), "Core Clock");

                _service.Register(MoBroItem
                    .CreateMetric()
                    .WithId(ArgusMonitorUtilities.SanitizeId("CPUFreqClock_" + sensorIndex + "_" + dataIndex))
                    .WithLabel(sensorName + " Clock")
                    .OfType(CoreMetricType.Frequency)
                    .OfCategory("CPU_" + cpuId)
                    .OfGroup(ArgusMonitorUtilities.SanitizeId(CPU + "_" + CommonGroup.Core_Clock.ToString() + "_" + cpuId))
                    .Build());
            }
        }

        // Register all metrics
        _argusMonitorLink.GetSensorData(register);

        foreach (KeyValuePair<int, List<string[]>> gpu in gpus)
        {
            Category? gpuCategory = null;
            foreach (string[] sensor in gpu.Value)
            {
                // sensor: sensorId, sensorName, sensorValue, sensorType, hardwareType, sensorGroup, sensorIndex
                if ("Name" == sensor[1])
                {
                    gpuCategory = MoBroItem
                        .CreateCategory()
                        .WithId(ArgusMonitorUtilities.SanitizeId("GPU_" + sensor[6]))
                        .WithLabel(sensor[2])
                        .Build();
                    _service.Register(gpuCategory);
                }
            }
            if (null != gpuCategory)
            {
                foreach (string[] sensor in gpu.Value)
                {
                    string groupId = ArgusMonitorUtilities.GroupID(sensor[4], sensor[5], Convert.ToInt32(sensor[6]));

                    addGroup(ArgusMonitorUtilities.SanitizeId(groupId), sensor[5]);

                    MetricBuilder.IBuildStage groupStage = MoBroItem
                        .CreateMetric()
                        .WithId(ArgusMonitorUtilities.SanitizeId(sensor[0]))
                        .WithLabel(sensor[1])
                        .OfType(ArgusMonitorUtilities.GetMetricType(sensor[3]))
                        .OfCategory(gpuCategory)
                        .OfGroup(groupId);

                    if ("Name" == sensor[1])
                    {
                        _service.Register(groupStage.AsStaticValue().Build());
                        toUpdate.Add(() =>
                        {
                            _service.UpdateMetricValue(ArgusMonitorUtilities.SanitizeId(sensor[0]),
                                                       sensor[2]);
                        });
                    }
                    else
                    {
                        _service.Register(groupStage.Build());
                        try
                        {
                            if (Convert.ToDouble(sensor[2]) == 0)
                            {
                                toUpdate.Add(() =>
                                {
                                    _service.UpdateMetricValue(ArgusMonitorUtilities.SanitizeId(sensor[0]),
                                                               sensor[2]);
                                });
                            }
                        }
                        catch (System.FormatException) { }
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
                    .WithId(ArgusMonitorUtilities.SanitizeId(CommonID.CPUTempMax.ToString() + "_" + cpuId))
                    .WithLabel("CPU Temperature Max")
                    .OfType(CoreMetricType.Temperature)
                    .OfCategory("CPU_" + cpuId)
                    .OfGroup(ArgusMonitorUtilities.SanitizeId(CPU + "_" + CommonGroup.Temperature.ToString() + "_" + cpuId))
                    .Build());

                _service.Register(MoBroItem
                    .CreateMetric()
                    .WithId(ArgusMonitorUtilities.SanitizeId(CommonID.CPUMultMax.ToString() + "_" + cpuId))
                    .WithLabel("CPU Multiplier Max")
                    .OfType(CoreMetricType.Multiplier)
                    .OfCategory("CPU_" + cpuId)
                    .OfGroup(ArgusMonitorUtilities.SanitizeId(CPU + "_" + CommonGroup.Multiplier.ToString() + "_" + cpuId))
                    .Build());

                _service.Register(MoBroItem
                    .CreateMetric()
                    .WithId(ArgusMonitorUtilities.SanitizeId(CommonID.CPUFreqClockMax.ToString() + "_" + cpuId))
                    .WithLabel("CPU Clock Max")
                    .OfType(CoreMetricType.Frequency)
                    .OfCategory("CPU_" + cpuId)
                    .OfGroup(ArgusMonitorUtilities.SanitizeId(CPU + "_" + CommonGroup.Core_Clock.ToString() + "_" + cpuId))
                    .Build());

                _service.Register(MoBroItem
                    .CreateMetric()
                    .WithId(ArgusMonitorUtilities.SanitizeId(CommonID.CPUTempAvg.ToString() + "_" + cpuId))
                    .WithLabel("CPU Temperature Average")
                    .OfType(CoreMetricType.Temperature)
                    .OfCategory("CPU_" + cpuId)
                    .OfGroup(ArgusMonitorUtilities.SanitizeId(CPU + "_" + CommonGroup.Temperature.ToString() + "_" + cpuId))
                    .Build());

                _service.Register(MoBroItem
                    .CreateMetric()
                    .WithId(ArgusMonitorUtilities.SanitizeId(CommonID.CPUMultAvg.ToString() + "_" + cpuId))
                    .WithLabel("CPU Multiplier Average")
                    .OfType(CoreMetricType.Multiplier)
                    .OfCategory("CPU_" + cpuId)
                    .OfGroup(ArgusMonitorUtilities.SanitizeId(CPU + "_" + CommonGroup.Multiplier.ToString() + "_" + cpuId))
                    .Build());

                _service.Register(MoBroItem
                    .CreateMetric()
                    .WithId(ArgusMonitorUtilities.SanitizeId(CommonID.CPUFreqClockAvg.ToString() + "_" + cpuId))
                    .WithLabel("CPU Clock Average")
                    .OfType(CoreMetricType.Frequency)
                    .OfCategory("CPU_" + cpuId)
                    .OfGroup(ArgusMonitorUtilities.SanitizeId(CPU + "_" + CommonGroup.Core_Clock.ToString() + "_" + cpuId))
                    .Build());

                _service.Register(MoBroItem
                    .CreateMetric()
                    .WithId(ArgusMonitorUtilities.SanitizeId(CommonID.CPUTempMin.ToString() + "_" + cpuId))
                    .WithLabel("CPU Temperature Min")
                    .OfType(CoreMetricType.Temperature)
                    .OfCategory("CPU_" + cpuId)
                    .OfGroup(ArgusMonitorUtilities.SanitizeId(CPU + "_" + CommonGroup.Temperature.ToString() + "_" + cpuId))
                    .Build());

                _service.Register(MoBroItem
                    .CreateMetric()
                    .WithId(ArgusMonitorUtilities.SanitizeId(CommonID.CPUMultMin.ToString() + "_" + cpuId))
                    .WithLabel("CPU Multiplier Min")
                    .OfType(CoreMetricType.Multiplier)
                    .OfCategory("CPU_" + cpuId)
                    .OfGroup(ArgusMonitorUtilities.SanitizeId(CPU + "_" + CommonGroup.Multiplier.ToString() + "_" + cpuId))
                    .Build());

                _service.Register(MoBroItem
                    .CreateMetric()
                    .WithId(ArgusMonitorUtilities.SanitizeId(CommonID.CPUFreqClockMin.ToString() + "_" + cpuId))
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
        _argusMonitorLink.UpdateSensorData(_updateFloat);
    }
}
