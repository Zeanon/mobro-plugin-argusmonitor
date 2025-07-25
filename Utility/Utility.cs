﻿using MoBro.Plugin.SDK.Enums;
using System.Text.RegularExpressions;


namespace Zeanon.Plugin.ArgusMonitor.Utility;


public static class ArgusMonitorUtilities
{
    private static readonly Regex IdSanitationRegex = new(@"[^\w\.\-]", RegexOptions.Compiled);

    public static CoreMetricType GetMetricType(string sensorType)
    {
        return sensorType switch
        {
            "Clock" or "Frequency" => CoreMetricType.Frequency,
            "Temperature" => CoreMetricType.Temperature,
            "Load" or "Percentage" => CoreMetricType.Usage,
            "Total" or "Usage" => CoreMetricType.Data,
            "Power" => CoreMetricType.Power,
            "Transfer" => CoreMetricType.DataFlow,
            "RPM" => CoreMetricType.Rotation,
            "Multiplier" => CoreMetricType.Multiplier,
            "Text" => CoreMetricType.Text,
            _ => CoreMetricType.Numeric,
        };
    }

    public static CoreCategory GetCategory(string hardwareType)
    {
        return hardwareType switch
        {
            "CPU" => CoreCategory.Cpu,
            "GPU" => CoreCategory.Gpu,
            "RAM" => CoreCategory.Ram,
            "Fan" => CoreCategory.Fan,
            "Drive" => CoreCategory.Storage,
            "Network" => CoreCategory.Network,
            "Battery" => CoreCategory.Battery,
            _ => CoreCategory.Miscellaneous,
        };
    }

    public static string SanitizeId(string id)
    {
        return IdSanitationRegex.Replace(id, "");
    }

    public static string SensorID(string hardwareType, string sensorType, string sensorGroup, string sensorIndex, string dataIndex)
    {
        return SanitizeId(hardwareType + "_" + sensorType + "_" + sensorGroup + "_" + sensorIndex + "_" + dataIndex);
    }

    public static string GroupID(string hardwareType, string sensorGroup)
    {
        return SanitizeId(hardwareType + "_" + sensorGroup);
    }

    public static string GroupID(string hardwareType, string sensorGroup, string sensorIndex)
    {
        return SanitizeId(hardwareType + "_" + sensorGroup + "_" + sensorIndex);
    }
}