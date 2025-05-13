Integrates PC hardware metrics made available
by [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor).

# Disclaimer

This plugin is created and maintained by ModBros and is not associated with LibreHardwareMonitor.  
It leverages the [LibreHardwareMonitorLib](https://www.nuget.org/packages/LibreHardwareMonitorLib/) library, provided by
LibreHardwareMonitor, to fetch PC hardware metrics.

---

# Setup

No additional setup required.  
The plugin comes with LibreHardwareMonitor already integrated, so no separate installation is necessary.

---

# Metrics

All metrics offered by LibreHardwareMonitor are fully integrated and accessible within MoBro.  
This includes data from various devices such as:

- Motherboards
- Intel and AMD processors
- NVIDIA and AMD graphics cards
- HDDs, SSDs, and NVMe drives
- Network cards
- And more...

---

# Settings

This plugin provides the following configurable settings:

| Setting          | Default | Description                                                                                                                                              |
|------------------|---------|----------------------------------------------------------------------------------------------------------------------------------------------------------|
| Update frequency | 1000 ms | The interval (in milliseconds) for reading and updating metrics from shared memory. Lower values allow more frequent updates but may increase CPU usage. |
| Processor        | enabled | Enables monitoring and inclusion of processor (CPU) metrics.                                                                                             |
| Graphics Card    | enabled | Enables monitoring and inclusion of graphics card (GPU) metrics.                                                                                         |
| Memory           | enabled | Enables monitoring and inclusion of memory (RAM) metrics.                                                                                                |
| Motherboard      | enabled | Enables monitoring and inclusion of motherboard metrics.                                                                                                 |
| Drives           | enabled | Enables monitoring and inclusion of drive (HDDs, SSDs, etc.) metrics.                                                                                    |
| Controller       | enabled | Enables monitoring and inclusion of controller metrics.                                                                                                  |
| Network          | enabled | Enables monitoring and inclusion of network (NIC) metrics.                                                                                               |
| Power supply     | enabled | Enables monitoring and inclusion of power supply (PSU) metrics.                                                                                          |
| Battery          | enabled | Enables monitoring and inclusion of battery metrics.                                                                                                     |
