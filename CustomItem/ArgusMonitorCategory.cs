using MoBro.Plugin.SDK.Builders;
using MoBro.Plugin.SDK.Models.Categories;

namespace Zeanon.Plugin.ArgusMonitor.CustomItem;

public static class ArgusMonitorCategory
{
    public static readonly Category Synthetic = MoBroItem
      .CreateCategory()
      .WithId("argusmonitor_synthetic")
      .WithLabel("Argus Monitor")
      .Build();
}
