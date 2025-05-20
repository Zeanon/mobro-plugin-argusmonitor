using MoBro.Plugin.SDK.Enums;
using System;
using System.Text.RegularExpressions;
using Zeanon.Plugin.ArgusMonitor.Enums;

namespace Zeanon.Plugin.ArgusMonitor.Utility;

public static class ArgusMonitorUtilities
{
    private static readonly Regex IdSanitationRegex = new(@"[^\w\.\-]", RegexOptions.Compiled);

    public static object? GetMetricValue(string value, string sensorType)
    {
        if (value == null) return null;

        if (sensorType == "Text") return value;

        double doubleVal = Convert.ToDouble(value);
        return sensorType switch
        {
            "Transfer" => doubleVal * 8 / 1_000_000, // bytes => bit
            "Usage" => doubleVal, // MB => Byte
            "Total" => doubleVal, // MB => Byte
            "Frequency" => doubleVal,
            "Clock" => doubleVal,
            _ => doubleVal / 1_000_000
        };
    }

    public static CoreMetricType GetMetricType(string sensorType)
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

    public static CoreCategory GetCategory(string hardwareType)
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
        return SanitizeId(hardwareType + "_Frequency_" + CommonGroup.Core_Clock + "_" + sensorName);
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