using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Zeanon.Plugin.ArgusMonitor.Model;

public static class ArgusMonitorWrapper
{
    private static readonly Regex IdSanitationRegex = new(@"[^\w\.\-]", RegexOptions.Compiled);

    #region dllimports
    private const string _dllImportPath = @"Resources/ArgusMonitorLink.dll";

    [DllImport(_dllImportPath, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr Instantiate();

    [DllImport(_dllImportPath, CallingConvention = CallingConvention.Cdecl)]
    public static extern int Start(IntPtr t);

    [DllImport(_dllImportPath, CallingConvention = CallingConvention.Cdecl)]
    public static extern int CheckForConnection(IntPtr t);

    [DllImport(_dllImportPath, CallingConvention = CallingConvention.Cdecl)]
    public static extern void Close(IntPtr t);

    [DllImport(_dllImportPath, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ParseSensorData(IntPtr t);

    [DllImport(_dllImportPath, CallingConvention = CallingConvention.Cdecl)]
    public static extern int GetDataLength(IntPtr t);

    [DllImport(_dllImportPath, CallingConvention = CallingConvention.Cdecl)]
    public static extern void GetSensorData(IntPtr t, StringBuilder sb, int maxlen);

    [DllImport(_dllImportPath, CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetSensorEnabled(IntPtr t, StringBuilder sb, int enabled);
    #endregion

    public static List<Sensor> _GetSensorData(IntPtr t) {
        ParseSensorData(t);
        int n = GetDataLength(t);
        StringBuilder sb = new StringBuilder(n);
        GetSensorData(t, sb, n);
        string sensor_data = sb.ToString();

        List<Sensor> sensors = new List<Sensor>();

        string[] sensors_split = sensor_data.Split("^");

        for (int i = 0; i < sensors_split.Length; ++i) {
            string sensor_string = sensors_split[i];
            if (sensor_string.Length == 0) {
                continue;
            }

            string[] sensor_values = sensor_string.Split("|");

            if (sensor_values.Length < 5) {
                continue;
            }

            Sensor sensor = new Sensor(
                "ArgusMonitor" + SanitizeId(sensor_values[0]),
                sensor_values[0],
                float.Parse(sensor_values[1]),
                sensor_values[2],
                sensor_values[3],
                "ArgusMonitor" + SanitizeId(sensor_values[4]),
                sensor_values[4]
            );

            sensors.Add(sensor);
        }

        return sensors;
    }

    public static bool CheckConnection(IntPtr t) {
        ParseSensorData(t);
        return GetDataLength(t) > 0;
    }

    private static string SanitizeId(string id)
    {
        return IdSanitationRegex.Replace(id, "");
    }
}