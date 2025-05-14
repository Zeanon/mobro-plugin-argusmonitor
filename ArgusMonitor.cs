using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MoBro.Plugin.SDK.Models;
using MoBro.Plugin.SDK.Models.Metrics;
using MoBro.Plugin.SDK.Services;
using MoBro.Plugin.SDK.Models.Categories;
using MoBro.Plugin.SDK.Builders;
using MoBro.Plugin.SDK.Builders;
using MoBro.Plugin.SDK.Enums;
using Zeanon.Plugin.ArgusMonitor;

namespace Zeanon.Plugin.ArgusMonitor;


public class ArgusMonitor : IDisposable
{
  private readonly IntPtr argus_monitor = ArgusMonitorWrapper.Instantiate();

  private static readonly Regex IdSanitationRegex = new(@"[^\w\.\-]", RegexOptions.Compiled);


  public static Category argusMonitorSynthetic = MoBroItem
    .CreateCategory()
    .WithId("argusmonitor_synthetic")
    .WithLabel("Argus Monitor")
    .Build();

  public void Update(IMoBroSettings settings)
  {
    ArgusMonitorWrapper.SetSensorEnabled(argus_monitor, new StringBuilder("CPU"), settings.GetValue<bool>("cpu_enabled") ? 1 : 0);
    ArgusMonitorWrapper.SetSensorEnabled(argus_monitor, new StringBuilder("GPU"), settings.GetValue<bool>("gpu_enabled") ? 1 : 0);
    ArgusMonitorWrapper.SetSensorEnabled(argus_monitor, new StringBuilder("RAM"), settings.GetValue<bool>("ram_enabled") ? 1 : 0);
    ArgusMonitorWrapper.SetSensorEnabled(argus_monitor, new StringBuilder("Mainboard"), settings.GetValue<bool>("motherboard_enabled") ? 1 : 0);
    ArgusMonitorWrapper.SetSensorEnabled(argus_monitor, new StringBuilder("Drive"), settings.GetValue<bool>("hdd_enabled") ? 1 : 0);
    ArgusMonitorWrapper.SetSensorEnabled(argus_monitor, new StringBuilder("Network"), settings.GetValue<bool>("network_enabled") ? 1 : 0);
    ArgusMonitorWrapper.SetSensorEnabled(argus_monitor, new StringBuilder("Battery"), settings.GetValue<bool>("battery_enabled") ? 1 : 0);
    ArgusMonitorWrapper.SetSensorEnabled(argus_monitor, new StringBuilder("ArgusMonitor"), settings.GetValue<bool>("argus_enabled") ? 1 : 0);
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

  public void RegisterItems(IMoBroService service)
  {
    string[] sensor_data = SensorData();

    List<string> groups = new List<string>();
    for (int i = 0; i < sensor_data.Length; ++i)
    {
      string sensor_string = sensor_data[i];
      if (sensor_string.Length == 0)
      {
        continue;
      }

      string[] sensor_values = sensor_string.Split("|");

      if (sensor_values.Length < 5 || sensor_values[2] == "Invalid")
      {
        continue;
      }

      string group_id = SanitizeId(sensor_values[3] + "_" + sensor_values[4]);

      if (!groups.Contains(group_id))
      {
        groups.Add(group_id);
        service.Register(MoBroItem.CreateGroup().WithId(group_id).WithLabel(sensor_values[4]).Build());
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
        service.Register(group_stage.AsStaticValue().Build());
      }
      else
      {
        service.Register(group_stage.Build());
      }
    }
  }

  public void UpdateMetricValues(IMoBroService service)
  {
    string[] sensor_data = SensorData();

    for (int i = 0; i < sensor_data.Length; ++i)
    {
      string sensor_string = sensor_data[i];
      if (sensor_string.Length == 0)
      {
        continue;
      }

      string[] sensor_values = sensor_string.Split("|");

      if (sensor_values.Length < 5 || sensor_values[2] == "Invalid")
      {
        continue;
      }

      service.UpdateMetricValue(SanitizeId(sensor_values[3] + "_" + sensor_values[2] + "_" + sensor_values[0]), _GetMetricValue(sensor_values[1], sensor_values[2]));
    }
  }

  private string[] SensorData()
  {
    ArgusMonitorWrapper.ParseSensorData(argus_monitor);
    int n = ArgusMonitorWrapper.GetDataLength(argus_monitor);
    StringBuilder sb = new StringBuilder(n);
    ArgusMonitorWrapper.GetSensorData(argus_monitor, sb, n);
    string sensor_data = sb.ToString();


    return sensor_data.Split("^");
  }

  private static object? _GetMetricValue(string value, string sensorType)
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

  private static string SanitizeId(string id)
  {
    return IdSanitationRegex.Replace(id, "");
  }
}

