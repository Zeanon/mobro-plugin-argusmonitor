using MoBro.Plugin.SDK;
using Serilog.Events;
using System;
using Zeanon.Plugin.ArgusMonitor;

using MoBroPluginWrapper plugin = MoBroPluginBuilder
  .Create<Plugin>()
  .WithLogLevel(LogEventLevel.Debug)
  .WithSettings(ArgusMonitor.Settings)
  .Build();

Console.ReadLine();
