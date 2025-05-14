using System;
using Zeanon.Plugin.ArgusMonitor;
using MoBro.Plugin.SDK;
using Serilog.Events;

using var plugin = MoBroPluginBuilder
  .Create<Plugin>()
  .WithLogLevel(LogEventLevel.Debug)
  .WithSettings(ArgusMonitor.Settings)
  .Build();

Console.ReadLine();