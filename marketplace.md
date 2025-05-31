Integrates PC hardware metrics into [MoBro](https://mobro.app) made available
by [ArgusMonitor](https://www.argusmonitor.com/index.php?language=en).

# Disclaimer

This plugin is created and maintained by Zeanon and is not associated with ArgusMonitor.  
It uses a custom [ArgusMonitorLink.dll](https://github.com/Zeanon/ArgusMonitorLink) to directly access the metrics read out by ArgusMonitor.

---

# Attention

**Breaking Change from 0.1.2 to 0.1.3 and to 0.1.5: I had to modify the sensor IDs slightly to ensure them being truly unique (Argus Monitor did some funky stuff I wasnt aware of but that got pointed out to me, sorry for the inconvenience, this is hopefully the last breaking change in that manner) and synthetic Temperatures now have their own dedicated hardware group so you need to re-select them in your layouts.**
**On the flip side, you now get some artificial metrics like the max temp of all cores, max multiplier and core clocks (which Argus doesnt expose but can easily be calculated with the FSB frequency and the multiplier) and obviously the max of those, feel free to suggest any other artificial values that can be created with the existing ones**
**Another breaking change when updating to 0.2.0, I reworked metric ids and hardware groups to handle multi cpu and multi gpu systems and not have IDs break when you change the name of a fan/sensor in argus monitor**

This plugin is in an alpha state rn, it seems to work well for me but it might still contain bugs, if you find any pls feel free to hit me up on the MoBro Discord (LastZeanon).

---

# Setup

You need to have ArgusMonitor running and enable the Argus Data API in Settings->Stability.

---

# Metrics

All metrics offered by ArgusMonitor are fully integrated and accessible within MoBro.  
This includes data from various devices such as:

- Motherboards
- Intel and AMD processors
- NVIDIA and AMD graphics cards
- HDDs, SSDs, and NVMe drives
- Network cards
- Synthetic Temperatures
- And more...

Small disclaimer: ArgusMonitor somehow doesnt export the CPU-Core temp to the API, easiest solution to get it is to just create a synthetic `max` temperature that only contains CPU-Core.
You can also use the artificial `CPU Temperature Max` metric.

---

## Settings

This plugin provides the following configurable settings:

| Setting              | Default | Description                                                                                                                                              |
|----------------------|---------|----------------------------------------------------------------------------------------------------------------------------------------------------------|
| Update frequency     | 1000 ms | The interval (in milliseconds) for reading and updating metrics from shared memory. Lower values allow more frequent updates but may increase CPU usage. |
| Processor            | enabled | Enables monitoring and inclusion of processor (CPU) metrics.                                                                                             |
| Graphics Card        | enabled | Enables monitoring and inclusion of graphics card (GPU) metrics.                                                                                         |
| Memory               | enabled | Enables monitoring and inclusion of memory (RAM) metrics.                                                                                                |
| Motherboard          | enabled | Enables monitoring and inclusion of motherboard metrics.                                                                                                 |
| Drives               | enabled | Enables monitoring and inclusion of drive (HDDs, SSDs, etc.) metrics.                                                                                    |
| Network              | enabled | Enables monitoring and inclusion of network (NIC) metrics.                                                                                               |
| Battery              | enabled | Enables monitoring and inclusion of battery metrics.                                                                                                     |
| Argus Monitor        | enabled | Enables monitoring and inclusion of Argus Monitor Synthetic temperatures and info about ArgusMonitor and the API.                                        |
| Initialization delay | 10s     | The delay to wait before initialization and querying sensors.                                                                                            |
| Poll delay           | 1s      | The delay after initialization before polling the sensors for the first time.                                                                            |

## SDK

This plugin is built using the [MoBro Plugin SDK](https://github.com/ModBros/mobro-plugin-sdk).  
Developer documentation is available at [developer.mobro.app](https://developer.mobro.app).
