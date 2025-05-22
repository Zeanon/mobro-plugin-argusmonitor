using MoBro.Plugin.SDK.Enums;
using System;
using System.Text.RegularExpressions;
using Zeanon.Plugin.ArgusMonitor.Enums;

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
            "Mainboard" => CoreCategory.Mainboard,
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

    public static string SanitizeId(CommonID id)
    {
        return IdSanitationRegex.Replace(id.ToString(), "");
    }

    public static string SanitizeId(CommonGroup id)
    {
        return IdSanitationRegex.Replace(id.ToString(), "");
    }

    public static string CoreClockID(string hardwareType, string sensorName)
    {
        return SanitizeId(hardwareType + "_Frequency_" + CommonGroup.Core_Clock.ToString() + "_" + sensorName);
    }

    public static string SensorID(string hardwareType, string sensorType, string sensorGroup, string sensorName)
    {
        return SanitizeId(hardwareType + "_" + sensorType + "_" + sensorGroup + "_" + sensorName);
    }

    public static string GroupID(string hardwareType, string sensorGroup)
    {
        return SanitizeId(hardwareType + "_" + sensorGroup);
    }
}