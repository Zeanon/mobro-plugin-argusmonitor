using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MoBro.Plugin.SDK.Builders;
using MoBro.Plugin.SDK.Enums;
using MoBro.Plugin.SDK.Models.Categories;
using MoBro.Plugin.SDK.Services;
using static Zeanon.Plugin.ArgusMonitor.Utility.SensorUtilities;

namespace Zeanon.Plugin.ArgusMonitor;


public class ArgusMonitor : IDisposable
{
    private readonly IntPtr argus_monitor = ArgusMonitorWrapper.Instantiate();

    private readonly IMoBroService _service;

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
            ArgusMonitorWrapper.SetSensorEnabled(argus_monitor, new StringBuilder(setting.Key), settings.GetValue<bool>(setting.Key, bool.Parse(setting.Value)));
        }
    }

    public void Start()
    {
        ArgusMonitorWrapper.Start(argus_monitor);
    }

    public bool CheckConnection()
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

        var groups = new List<string> { "CPU_Temperature", "CPU_Multiplier", "CPU_Frequency" };

        _service.Register(MoBroItem.CreateGroup().WithId("CPU_Temperature").WithLabel("Temperature").Build());
        _service.Register(MoBroItem.CreateGroup().WithId("CPU_Multiplier").WithLabel("Multiplier").Build());
        _service.Register(MoBroItem.CreateGroup().WithId("CPU_Core_Clock").WithLabel("Core Clock").Build());


        _service.Register(MoBroItem
            .CreateMetric()
            .WithId(SanitizeId("CPU_Temperature_CPU_MAX"))
            .WithLabel("CPU Temperature Max")
            .OfType(GetMetricType("Temperature"))
            .OfCategory(GetCategory("CPU"))
            .OfGroup("CPU_Temperature")
            .Build());

        _service.Register(MoBroItem
            .CreateMetric()
            .WithId(SanitizeId("CPU_Multiplier_CPU_MAX"))
            .WithLabel("CPU Multiplier Max")
            .OfType(GetMetricType("Multiplier"))
            .OfCategory(GetCategory("CPU"))
            .OfGroup("CPU_Multiplier")
            .Build());

        _service.Register(MoBroItem
            .CreateMetric()
            .WithId(SanitizeId("CPU_Core_Clock_CPU_MAX"))
            .WithLabel("CPU Clock Max")
            .OfType(GetMetricType("Frequency"))
            .OfCategory(GetCategory("CPU"))
            .OfGroup("CPU_Core_Clock")
            .Build());

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

            var group_id = GroupID(sensor_values);

            if (!groups.Contains(group_id))
            {
                groups.Add(group_id);
                _service.Register(MoBroItem.CreateGroup().WithId(group_id).WithLabel(sensor_values[4]).Build());
            }

            var type = GetMetricType(sensor_values[2]);
            var category = GetCategory(sensor_values[3]);

            var type_stage = MoBroItem
                .CreateMetric()
                .WithId(SensorID(sensor_values))
                .WithLabel(sensor_values[0])
                .OfType(type);

            var category_stage = sensor_values[3] == "ArgusMonitor"
                    ? type_stage.OfCategory(ArgusMonitor.argusMonitorSynthetic)
                    : type_stage.OfCategory(category);

            var group_stage = category_stage.OfGroup(group_id);

            if (sensor_values[2] == "Text")
            {
                _service.Register(group_stage.AsStaticValue().Build());
            }
            else
            {
                _service.Register(group_stage.Build());
            }

            if (type == CoreMetricType.Multiplier && category == CoreCategory.Cpu && sensor_values[4] == "Multiplier")
            {
                var freq = MoBroItem
                    .CreateMetric()
                    .WithId(SanitizeId(sensor_values[3] + "_Frequency_Clock_" + sensor_values[0]))
                    .WithLabel(sensor_values[0] + " Clock")
                    .OfType(CoreMetricType.Frequency)
                    .OfCategory(category)
                    .OfGroup("CPU_Core_Clock")
                    .Build();
                _service.Register(freq);
            }
        }
    }

    public void UpdateMetricValues()
    {
        var cpu_temps = new HashSet<double>();
        double? fsb = null;
        var multipliers = new Dictionary<string, double>();
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

            var value = GetMetricValue(sensor_values[1], sensor_values[2]);

            if (value != null)
            {
                if (sensor_values[2] == "Temperature" && sensor_values[3] == "CPU" && sensor_values[4] == "Temperature")
                {
                    cpu_temps.Add((double)value);
                }

                if (sensor_values[2] == "Multiplier" && sensor_values[3] == "CPU" && sensor_values[4] == "Multiplier")
                {
                    multipliers.Add(SanitizeId(sensor_values[3] + "_Frequency_Clock_" + sensor_values[0]), (double) value);
                }

                if (GetMetricType(sensor_values[2]) == CoreMetricType.Frequency && sensor_values[3] == "CPU" && sensor_values[4] == "FSB")
                {
                    fsb = (double) value;
                }
            }

            _service.UpdateMetricValue(SensorID(sensor_values), value);
        }

        if (fsb != null)
        {
            foreach (KeyValuePair<string, double> multiplier in multipliers)
            {
                _service.UpdateMetricValue(multiplier.Key, multiplier.Value * fsb);
            }

            _service.UpdateMetricValue(SanitizeId("CPU_Multiplier_CPU_MAX"), multipliers.Values.Max());
            _service.UpdateMetricValue(SanitizeId("CPU_Core_Clock_CPU_MAX"), multipliers.Values.Max() * fsb);
        }

        if (cpu_temps.Count > 0)
        {
            _service.UpdateMetricValue(SanitizeId("CPU_Temperature_CPU_MAX"), cpu_temps.Max());
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

    private string SensorID(string[] sensor_values)
    {
        return SanitizeId(sensor_values[3] + "_" + sensor_values[2] + "_" + sensor_values[4] + "_" + sensor_values[0]);
    }

    private string GroupID(string[] sensor_values)
    {
        return SanitizeId(sensor_values[3] + "_" + sensor_values[4]);
    }
}