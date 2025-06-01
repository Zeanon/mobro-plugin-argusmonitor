using MoBro.Plugin.SDK.Builders;
using MoBro.Plugin.SDK.Models.Categories;

namespace Zeanon.Plugin.ArgusMonitor.CustomItem;

public static class ArgusMonitorCategory
{
    public static readonly Category ArgusMonitor = MoBroItem
      .CreateCategory()
      .WithId("argusmonitor")
      .WithLabel("Argus Monitor")
      .Build();

    public static readonly Category Temperature = MoBroItem
      .CreateCategory()
      .WithId("temperature")
      .WithLabel("Temperature")
      .Build();
}
