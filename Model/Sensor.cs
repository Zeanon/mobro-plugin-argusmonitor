namespace Zeanon.Plugin.ArgusMonitor.Model;

public readonly record struct Sensor(
  string Id,
  string Name,
  string Value,
  string SensorType,
  string HardwareType,
  string GroupId,
  string GroupName
);