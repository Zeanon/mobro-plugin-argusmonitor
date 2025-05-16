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
    public static extern int Start(this IntPtr t);

    [DllImport(_dllImportPath, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool CheckConnection(this IntPtr t);

    [DllImport(_dllImportPath, CallingConvention = CallingConvention.Cdecl)]
    public static extern int Stop(this IntPtr t);

    [DllImport(_dllImportPath, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ParseSensorData(this IntPtr t);

    [DllImport(_dllImportPath, CallingConvention = CallingConvention.Cdecl)]
    public static extern int GetDataLength(this IntPtr t);

    [DllImport(_dllImportPath, CallingConvention = CallingConvention.Cdecl)]
    public static extern void GetSensorData(this IntPtr t, StringBuilder buffer, int maxlen);

    [DllImport(_dllImportPath, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool CheckData(this IntPtr t);

    [DllImport(_dllImportPath, CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetSensorEnabled(this IntPtr t, StringBuilder name, bool enabled);

    [DllImport(_dllImportPath, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool GetSensorEnabled(this IntPtr t, StringBuilder name);
    #endregion
}