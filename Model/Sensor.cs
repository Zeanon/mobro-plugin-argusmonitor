namespace Zeanon.Plugin.ArgusMonitor.Model;

public readonly record struct Sensor(
  string Id,
  string Name,
  float? Value,
  string SensorType,
  string HardwareType,
  string GroupId,
  string GroupName
);