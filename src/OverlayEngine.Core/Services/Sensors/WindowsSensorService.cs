#if WIN_PLATFORM
using LibreHardwareMonitor.Hardware;
#endif

namespace OverlayEngine.Core.Services.Sensors;

#if WIN_PLATFORM

/// <summary>
/// Reads CPU/GPU load and temperature via LibreHardwareMonitor.
/// CPU temperature requires Admin (ring0 MSR access). Load works without elevation.
/// </summary>
public sealed class WindowsSensorService : ISensorService
{
    private readonly Computer _computer;

    public WindowsSensorService()
    {
        _computer = new Computer { IsCpuEnabled = true, IsGpuEnabled = true };
        try { _computer.Open(); }
        catch { /* ring0 may need elevation; partial data still available */ }
    }

    public Task<SensorSnapshot> ReadAsync()
    {
        double cpuLoad = 0, cpuTemp = 0, gpuLoad = 0, gpuTemp = 0;

        foreach (var hw in _computer.Hardware)
        {
            try
            {
                hw.Update();
                foreach (var sub in hw.SubHardware) sub.Update();

                if (hw.HardwareType == HardwareType.Cpu)
                {
                    double tempPrimary = 0, tempAny = 0;
                    foreach (var s in AllSensors(hw))
                    {
                        if (s.SensorType == SensorType.Load &&
                            s.Name.Contains("Total", StringComparison.OrdinalIgnoreCase))
                            cpuLoad = Math.Max(cpuLoad, (double)(s.Value ?? 0));

                        if (s.SensorType == SensorType.Temperature)
                        {
                            double val = (double)(s.Value ?? 0);
                            if (val > 0) tempAny = Math.Max(tempAny, val);
                            // Prefer aggregate/package sensors over per-core values
                            if (s.Name.Contains("Package",  StringComparison.OrdinalIgnoreCase) ||
                                s.Name.Contains("Average",  StringComparison.OrdinalIgnoreCase) ||
                                s.Name.Contains("Tdie",     StringComparison.OrdinalIgnoreCase) ||
                                s.Name.Contains("Core Max", StringComparison.OrdinalIgnoreCase) ||
                                s.Name.Equals("CPU",        StringComparison.OrdinalIgnoreCase))
                                tempPrimary = Math.Max(tempPrimary, val);
                        }
                    }
                    cpuTemp = tempPrimary > 0 ? tempPrimary : tempAny;
                }

                if (hw.HardwareType is HardwareType.GpuNvidia
                                     or HardwareType.GpuAmd
                                     or HardwareType.GpuIntel)
                {
                    foreach (var s in AllSensors(hw))
                    {
                        if (s.SensorType == SensorType.Load &&
                            s.Name.Contains("Core", StringComparison.OrdinalIgnoreCase))
                            gpuLoad = Math.Max(gpuLoad, (double)(s.Value ?? 0));
                        if (s.SensorType == SensorType.Temperature &&
                            s.Name.Contains("Core", StringComparison.OrdinalIgnoreCase))
                            gpuTemp = Math.Max(gpuTemp, (double)(s.Value ?? 0));
                    }
                }
            }
            catch { /* ignore faults for individual hardware */ }
        }

        return Task.FromResult(new SensorSnapshot(cpuLoad, cpuTemp, gpuLoad, gpuTemp));
    }

    private static IEnumerable<ISensor> AllSensors(IHardware hw)
    {
        foreach (var s in hw.Sensors) yield return s;
        foreach (var sub in hw.SubHardware)
            foreach (var s in sub.Sensors) yield return s;
    }

    public void Dispose() { try { _computer.Close(); } catch { } }
}

#else

// Stub for non-Windows compilation (Linux build never instantiates this).
public sealed class WindowsSensorService : ISensorService
{
    public Task<SensorSnapshot> ReadAsync() => Task.FromResult(new SensorSnapshot(0, 0, 0, 0));
    public void Dispose() { }
}

#endif
