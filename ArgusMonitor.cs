using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using MoBro.Plugin.SDK.Services;
using MoBro.Plugin.SDK.Models.Categories;
using MoBro.Plugin.SDK.Builders;
using MoBro.Plugin.SDK.Enums;

namespace Zeanon.Plugin.ArgusMonitor;


public class ArgusMonitor : IDisposable
{
    private readonly IntPtr argus_monitor = ArgusMonitorWrapper.Instantiate();

    private readonly IMoBroService _service;

    private static readonly Regex IdSanitationRegex = new(@"[^\w\.\-]", RegexOptions.Compiled);


    public static readonly Dictionary<string, string> Settings = new Dictionary<string, string>
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

    public static readonly Category argusMonitorSynthetic = MoBroItem
      .CreateCategory()
      .WithId("argusmonitor_synthetic")
      .WithLabel("Argus Monitor")
      .Build();

    public ArgusMonitor(IMoBroService service)
    {
        _service = service;
    }

    public void UpdateSettings(IMoBroSettings settings)
    {
        foreach (KeyValuePair<string, string> setting in Settings)
        {
            ArgusMonitorWrapper.SetSensorEnabled(argus_monitor, new StringBuilder(setting.Key), settings.GetValue<bool>(setting.Key, bool.Parse(setting.Value)) ? 1 : 0);
        }
    }

    public void Start()
    {
        ArgusMonitorWrapper.Start(argus_monitor);
    }

    public int CheckConnection()
    {
        return ArgusMonitorWrapper.CheckConnection(argus_monitor);
    }

    public bool CheckData()
    {
        return ArgusMonitorWrapper.CheckData(argus_monitor);
    }

    public void Dispose()
    {
        ArgusMonitorWrapper.Close(argus_monitor);
    }

    public void RegisterCategories()
    {
        _service.Register(ArgusMonitor.argusMonitorSynthetic);
    }

    public void RegisterItems()
    {
        var sensor_data = SensorData();

        var groups = new List<string>();

        for (int i = 0; i < sensor_data.Length; ++i)
        {
            var sensor_string = sensor_data[i];
            if (sensor_string.Length == 0)
            {
                continue;
            }

            var sensor_values = sensor_string.Split(new string[] { "[<|>]" }, StringSplitOptions.None);

            if (sensor_values.Length < 5 || sensor_values[2] == "Invalid")
            {
                continue;
            }

            var group_id = SanitizeId(sensor_values[3] + "_" + sensor_values[4]);

            if (!groups.Contains(group_id))
            {
                groups.Add(group_id);
                _service.Register(MoBroItem.CreateGroup().WithId(group_id).WithLabel(sensor_values[4]).Build());
            }

            var type_stage = MoBroItem
                .CreateMetric()
                .WithId(SanitizeId(sensor_values[3] + "_" + sensor_values[2] + "_" + sensor_values[0]))
                .WithLabel(sensor_values[0])
                .OfType(GetMetricType(sensor_values[2]));

            var category_stage = sensor_values[3] == "ArgusMonitor"
                    ? type_stage.OfCategory(ArgusMonitor.argusMonitorSynthetic)
                    : type_stage.OfCategory(GetCategory(sensor_values[3]));

            var group_stage = category_stage.OfGroup(group_id);

            if (sensor_values[2] == "Text")
            {
                _service.Register(group_stage.AsStaticValue().Build());
            }
            else
            {
                _service.Register(group_stage.Build());
            }
        }
    }

    public void UpdateMetricValues()
    {
        var sensor_data = SensorData();

        for (int i = 0; i < sensor_data.Length; ++i)
        {
            var sensor_string = sensor_data[i];
            if (sensor_string.Length == 0)
            {
                continue;
            }

            var sensor_values = sensor_string.Split(new string[] { "[<|>]" }, StringSplitOptions.None);

            if (sensor_values.Length < 5 || sensor_values[2] == "Invalid")
            {
                continue;
            }

            _service.UpdateMetricValue(SanitizeId(sensor_values[3] + "_" + sensor_values[2] + "_" + sensor_values[0]), GetMetricValue(sensor_values[1], sensor_values[2]));
        }
    }

    private string[] SensorData()
    {
        ArgusMonitorWrapper.ParseSensorData(argus_monitor);
        var n = ArgusMonitorWrapper.GetDataLength(argus_monitor);
        StringBuilder sb = new StringBuilder(n);
        ArgusMonitorWrapper.GetSensorData(argus_monitor, sb, n);
        var sensor_data = sb.ToString();

        return sensor_data.Split(new string[] { "]<|>[" }, StringSplitOptions.None);
    }

    private static string SanitizeId(string id)
    {
        return IdSanitationRegex.Replace(id, "");
    }

    private static object? GetMetricValue(string value, string sensorType)
    {
        if (value == null) return null;

        if (sensorType == "Text")
        {
            return value;
        }

        var doubleVal = Convert.ToDouble(value);
        return sensorType switch
        {
            "Transfer" => doubleVal * 8, // bytes => bit
            "Usage" => doubleVal,
            "Total" => doubleVal,
            _ => doubleVal / 1_000_000
        };
    }

    private static CoreMetricType GetMetricType(string sensorType)
    {
        switch (sensorType)
        {
            case "Clock":
            case "Frequency":
                return CoreMetricType.Frequency;
            case "Temperature":
                return CoreMetricType.Temperature;
            case "Load":
            case "Percentage":
                return CoreMetricType.Usage;
            case "Total":
            case "Usage":
                return CoreMetricType.Data;
            case "Power":
                return CoreMetricType.Power;
            case "Transfer":
                return CoreMetricType.DataFlow;
            case "RPM":
                return CoreMetricType.Rotation;
            case "Multiplier":
                return CoreMetricType.Multiplier;
            case "Text":
                return CoreMetricType.Text;
            default:
                return CoreMetricType.Numeric;
        }
    }

    private static CoreCategory GetCategory(string hardwareType)
    {
        switch (hardwareType)
        {
            case "CPU":
                return CoreCategory.Cpu;
            case "GPU":
                return CoreCategory.Gpu;
            case "RAM":
                return CoreCategory.Ram;
            case "Mainboard":
                return CoreCategory.Mainboard;
            case "Drive":
                return CoreCategory.Storage;
            case "Network":
                return CoreCategory.Network;
            case "Battery":
                return CoreCategory.Battery;
            default:
                return CoreCategory.Miscellaneous;
        }
    }
}