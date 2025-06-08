using System;
using System.Runtime.InteropServices;


namespace Zeanon.Plugin.ArgusMonitor.Utility;


public static partial class ArgusMonitorWrapper
{
    #region dllimports
    private const string _dllImportPath = @"Resources/libs/ArgusMonitorLink.dll";

    [LibraryImport(_dllImportPath)]
    public static partial IntPtr Create();

    [DllImport(_dllImportPath, CallingConvention = CallingConvention.Cdecl)]
    public static extern int Open(this IntPtr argusMonitorLinkPtr);

    [DllImport(_dllImportPath, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool IsOpen(this IntPtr argusMonitorLinkPtr);

    [DllImport(_dllImportPath, CallingConvention = CallingConvention.Cdecl)]
    public static extern int Close(this IntPtr argusMonitorLinkPtr);

    [DllImport(_dllImportPath, CallingConvention = CallingConvention.Cdecl)]
    public static extern int GetTotalSensorCount(this IntPtr argusMonitorLinkPtr);

    [DllImport(_dllImportPath, CallingConvention = CallingConvention.Cdecl)]
    public static extern void GetSensorData(this IntPtr argusMonitorLinkPtr, ProcessSensorData processSensorData);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void ProcessSensorData([MarshalAs(UnmanagedType.LPStr)] string sensorName,
                                           [MarshalAs(UnmanagedType.LPStr)] string sensorValue,
                                           [MarshalAs(UnmanagedType.LPStr)] string sensorType,
                                           [MarshalAs(UnmanagedType.LPStr)] string hardwareType,
                                           [MarshalAs(UnmanagedType.LPStr)] string sensorGroup,
                                           [MarshalAs(UnmanagedType.LPStr)] string sensorIndex,
                                           [MarshalAs(UnmanagedType.LPStr)] string dataIndex);

    [DllImport(_dllImportPath, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool UpdateSensorData(this IntPtr argusMonitorLinkPtr, UpdateFloat update);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void UpdateFloat([MarshalAs(UnmanagedType.LPStr)] string sensorId,
                                     [MarshalAs(UnmanagedType.R4)] float sensorValue);

    [DllImport(_dllImportPath, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool CheckArgusSignature(this IntPtr argusMonitorLinkPtr);

    [DllImport(_dllImportPath, CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetHardwareEnabled(this IntPtr argusMonitorLinkPtr, string type, bool enabled);

    [DllImport(_dllImportPath, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool IsHardwareEnabled(this IntPtr argusMonitorLinkPtr, string type);

    [DllImport(_dllImportPath, CallingConvention = CallingConvention.Cdecl)]
    public static extern void Destroy(this IntPtr argusMonitorLinkPtr);
    #endregion
}
