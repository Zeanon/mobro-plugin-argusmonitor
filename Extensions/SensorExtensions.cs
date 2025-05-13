using System;
using MoBro.Plugin.SDK.Builders;
using MoBro.Plugin.SDK.Enums;
using MoBro.Plugin.SDK.Models.Categories;
using MoBro.Plugin.SDK.Models.Metrics;
using Zeanon.Plugin.ArgusMonitor.Model;

namespace Zeanon.Plugin.ArgusMonitor.Extensions;

internal static class SensorExtensions
{
  public static Metric AsMetric(this Sensor sensor)
  {
    return MoBroItem
      .CreateMetric()
      .WithId(sensor.Id)
      .WithLabel(sensor.Name)
      .OfType(GetMetricType(sensor.SensorType))
      .OfCategory(GetCategory(sensor.HardwareType))
      .OfGroup(sensor.GroupId)
      .Build();
  }

  public static Group AsGroup(this Sensor sensor)
  {
    return MoBroItem.CreateGroup()
      .WithId(sensor.GroupId)
      .WithLabel(sensor.GroupName)
      .Build();
  }

  public static MetricValue AsMetricValue(this Sensor sensor)
  {
    return new MetricValue(sensor.Id, GetMetricValue(sensor));
  }

  private static object? GetMetricValue(in Sensor sensor)
  {
    if (sensor.Value == null) return null;

    var doubleVal = Convert.ToDouble(sensor.Value);
    return sensor.SensorType switch
    {
      "Transfer" => doubleVal * 8, // bytes => bit
      "Usage" => doubleVal * 1_000_000, // MB => Byte
      "Total" => doubleVal * 1_000_000, // MB => Byte
      _ => doubleVal
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