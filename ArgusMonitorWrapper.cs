using System;
using System.Runtime.InteropServices;
using System.Text;

public static class ArgusMonitorWrapper
{
    #region dllimports
    private const string _dllImportPath = @"Resources/libs/ArgusMonitorLink.dll";

    [DllImport(_dllImportPath, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr Instantiate();

    [DllImport(_dllImportPath, CallingConvention = CallingConvention.Cdecl)]
    public static extern void Start(IntPtr t);

    [DllImport(_dllImportPath, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool CheckConnection(IntPtr t);

    [DllImport(_dllImportPath, CallingConvention = CallingConvention.Cdecl)]
    public static extern void Close(IntPtr t);

    [DllImport(_dllImportPath, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ParseSensorData(IntPtr t);

    [DllImport(_dllImportPath, CallingConvention = CallingConvention.Cdecl)]
    public static extern int GetDataLength(IntPtr t);

    [DllImport(_dllImportPath, CallingConvention = CallingConvention.Cdecl)]
    public static extern void GetSensorData(IntPtr t, StringBuilder sb, int maxlen);

    [DllImport(_dllImportPath, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool CheckData(IntPtr t);

    [DllImport(_dllImportPath, CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetSensorEnabled(IntPtr t, StringBuilder sb, bool enabled);
    #endregion
}