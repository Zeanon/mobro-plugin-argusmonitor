using System;
using System.Runtime.InteropServices;


namespace Zeanon.Plugin.ArgusMonitor.Utility;

public static partial class ArgusMonitorWrapper
{
    #region dllimports
    private const string _dllImportPath = @"Resources/libs/ArgusMonitorLink.dll";

    [LibraryImport(_dllImportPath)]
    public static partial IntPtr Instantiate();

    [DllImport(_dllImportPath, CallingConvention = CallingConvention.Cdecl)]
    public static extern int Open(this IntPtr t);

    [DllImport(_dllImportPath, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool IsOpen(this IntPtr t);

    [DllImport(_dllImportPath, CallingConvention = CallingConvention.Cdecl)]
    public static extern int Close(this IntPtr t);

    [DllImport(_dllImportPath, CallingConvention = CallingConvention.Cdecl)]
    public static extern int GetTotalSensorCount(this IntPtr t);

    [DllImport(_dllImportPath, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool GetSensorData(this IntPtr t, ProcessSensorData processSensorData);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void ProcessSensorData([MarshalAs(UnmanagedType.LPStr)] string sensorName,
                                           [MarshalAs(UnmanagedType.LPStr)] string sensorValue,
                                           [MarshalAs(UnmanagedType.LPStr)] string sensorType,
                                           [MarshalAs(UnmanagedType.LPStr)] string hardwareType,
                                           [MarshalAs(UnmanagedType.LPStr)] string sensorGroup);

    [DllImport(_dllImportPath, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool UpdateSensorData(this IntPtr t, UpdateSensor UpdateSensorData);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void UpdateSensor([MarshalAs(UnmanagedType.LPStr)] string sensorId,
                                      [MarshalAs(UnmanagedType.LPStr)] string sensorValue);

    [DllImport(_dllImportPath, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool CheckArgusSignature(this IntPtr t);

    [DllImport(_dllImportPath, CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetHardwareEnabled(this IntPtr t, string type, bool enabled);

    [DllImport(_dllImportPath, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool IsHardwareEnabled(this IntPtr t, string type);
    #endregion
}