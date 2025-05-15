using System;
using MoBro.Plugin.SDK;
using Serilog.Events;
using Zeanon.Plugin.ArgusMonitor;

using var plugin = MoBroPluginBuilder
  .Create<Plugin>()
  .WithLogLevel(LogEventLevel.Debug)
  .WithSettings(ArgusMonitor.Settings)
  .Build();

Console.ReadLine();