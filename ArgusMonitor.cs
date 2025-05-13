using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MoBro.Plugin.SDK.Models;
using MoBro.Plugin.SDK.Models.Metrics;
using MoBro.Plugin.SDK.Services;
using Zeanon.Plugin.ArgusMonitor.Extensions;

namespace Zeanon.Plugin.ArgusMonitor;


public class ArgusMonitor : IDisposable
{
  private readonly IntPtr argus_monitor = ArgusMonitorWrapper.Instantiate();

  private bool cpu_enabled = true;
  private bool gpu_enabled = true;
  private bool memory_enabled = true;
  private bool mainboard_enabled = true;
  private bool network_enabled = true;
  private bool battery_enabled = true;
  private bool drive_enabled = true;

  public void Update(IMoBroSettings settings)
  {
    ArgusMonitorWrapper.SetSensorEnabled(argus_monitor, new StringBuilder("CPU"), settings.GetValue<bool>("cpu_enabled") ? 1 : 0);
    ArgusMonitorWrapper.SetSensorEnabled(argus_monitor, new StringBuilder("GPU"), settings.GetValue<bool>("gpu_enabled") ? 1 : 0);
    ArgusMonitorWrapper.SetSensorEnabled(argus_monitor, new StringBuilder("RAM"), settings.GetValue<bool>("ram_enabled") ? 1 : 0);
    ArgusMonitorWrapper.SetSensorEnabled(argus_monitor, new StringBuilder("Mainboard"), settings.GetValue<bool>("motherboard_enabled") ? 1 : 0);
    ArgusMonitorWrapper.SetSensorEnabled(argus_monitor, new StringBuilder("Drive"), settings.GetValue<bool>("hdd_enabled") ? 1 : 0);
    ArgusMonitorWrapper.SetSensorEnabled(argus_monitor, new StringBuilder("Network"), settings.GetValue<bool>("network_enabled") ? 1 : 0);
    ArgusMonitorWrapper.SetSensorEnabled(argus_monitor, new StringBuilder("Battery"), settings.GetValue<bool>("battery_enabled") ? 1 : 0);
    ArgusMonitorWrapper.SetSensorEnabled(argus_monitor, new StringBuilder("ArgusMonitor"), settings.GetValue<bool>("argus_enabled") ? 1 : 0);
  }

  public void Start()
  {
    ArgusMonitorWrapper.Start(argus_monitor);
  }

  public int CheckConnection()
  {
    return ArgusMonitorWrapper.CheckConnection(argus_monitor);
  }

  public bool CheckData()
  {
    return ArgusMonitorWrapper.CheckData(argus_monitor);
  }

  public void Dispose()
  {
    ArgusMonitorWrapper.Close(argus_monitor);
  }

  public IEnumerable<IMoBroItem> GetMetricItems()
  {
    var sensors = ArgusMonitorWrapper._GetSensorData(argus_monitor);
    var sensorsArray = sensors.ToArray();
    var metrics = sensorsArray
      .Select(IMoBroItem (sensor) => sensor.AsMetric());

    var groups = sensorsArray
      .Select(IMoBroItem (sensor) => sensor.AsGroup())
      .DistinctBy(g => g.Id);

    return groups.Concat(metrics);
  }

  public IEnumerable<MetricValue> GetMetricValues()
  {
    return ArgusMonitorWrapper._GetSensorData(argus_monitor).Select(s => s.AsMetricValue());
  }
}

