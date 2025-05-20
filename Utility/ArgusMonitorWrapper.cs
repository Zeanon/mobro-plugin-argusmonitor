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
    public static extern bool GetSensorData(this IntPtr t, AddArray add);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void AddArray([MarshalAs(UnmanagedType.LPArray, SizeConst = 5)] string[] sensor);

    [DllImport(_dllImportPath, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool CheckArgusSignature(this IntPtr t);

    [DllImport(_dllImportPath, CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetHardwareEnabled(this IntPtr t, string type, bool enabled);

    [DllImport(_dllImportPath, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool GetHardwareEnabled(this IntPtr t, string type);
    #endregion
}