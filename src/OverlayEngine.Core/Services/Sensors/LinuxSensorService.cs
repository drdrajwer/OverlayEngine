namespace OverlayEngine.Core.Services.Sensors;

/// <summary>
/// Reads CPU stats from /proc/stat (load) and /sys/class/thermal (temp).
/// GPU temp is read from hwmon entries matching amdgpu/nvidia driver names.
/// </summary>
public sealed class LinuxSensorService : ISensorService
{
    private long _prevIdle;
    private long _prevTotal;

    public async Task<SensorSnapshot> ReadAsync()
    {
        var cpuLoad = await ReadCpuLoadAsync();
        var cpuTemp = await ReadCpuTempAsync();
        var (gpuLoad, gpuTemp) = await ReadGpuAsync();

        return new SensorSnapshot(cpuLoad, cpuTemp, gpuLoad, gpuTemp);
    }

    private async Task<double> ReadCpuLoadAsync()
    {
        try
        {
            var line = (await File.ReadAllLinesAsync("/proc/stat"))[0];
            // Format: cpu  user nice system idle iowait irq softirq steal guest guest_nice
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            long user    = long.Parse(parts[1]);
            long nice    = long.Parse(parts[2]);
            long system  = long.Parse(parts[3]);
            long idle    = long.Parse(parts[4]);
            long iowait  = long.Parse(parts[5]);
            long irq     = long.Parse(parts[6]);
            long softirq = long.Parse(parts[7]);

            long currentIdle  = idle + iowait;
            long currentTotal = user + nice + system + idle + iowait + irq + softirq;

            long diffIdle  = currentIdle  - _prevIdle;
            long diffTotal = currentTotal - _prevTotal;

            _prevIdle  = currentIdle;
            _prevTotal = currentTotal;

            if (diffTotal == 0) return 0.0;
            return (1.0 - (double)diffIdle / diffTotal) * 100.0;
        }
        catch { return 0.0; }
    }

    private static async Task<double> ReadCpuTempAsync()
    {
        // Try coretemp zone first, then generic thermal_zone0
        var paths = new[]
        {
            "/sys/class/thermal/thermal_zone0/temp",
            "/sys/class/thermal/thermal_zone1/temp",
        };
        foreach (var path in paths)
        {
            try
            {
                if (!File.Exists(path)) continue;
                var raw = await File.ReadAllTextAsync(path);
                if (int.TryParse(raw.Trim(), out var millideg))
                    return millideg / 1000.0;
            }
            catch { /* next path */ }
        }

        // Try hwmon coretemp
        try
        {
            foreach (var dir in Directory.GetDirectories("/sys/class/hwmon"))
            {
                var nameFile = Path.Combine(dir, "name");
                if (!File.Exists(nameFile)) continue;
                var name = (await File.ReadAllTextAsync(nameFile)).Trim();
                if (name != "coretemp" && name != "k10temp") continue;

                var inputs = Directory.GetFiles(dir, "temp*_input");
                if (inputs.Length == 0) continue;
                var val = await File.ReadAllTextAsync(inputs[0]);
                if (int.TryParse(val.Trim(), out var mc))
                    return mc / 1000.0;
            }
        }
        catch { /* not available */ }

        return 0.0;
    }

    private static async Task<(double load, double temp)> ReadGpuAsync()
    {
        // AMD GPU via amdgpu hwmon
        try
        {
            foreach (var dir in Directory.GetDirectories("/sys/class/hwmon"))
            {
                var nameFile = Path.Combine(dir, "name");
                if (!File.Exists(nameFile)) continue;
                if ((await File.ReadAllTextAsync(nameFile)).Trim() != "amdgpu") continue;

                double temp = 0, load = 0;
                var tempInput = Path.Combine(dir, "temp1_input");
                if (File.Exists(tempInput))
                {
                    var t = await File.ReadAllTextAsync(tempInput);
                    if (int.TryParse(t.Trim(), out var mc)) temp = mc / 1000.0;
                }

                // GPU busy percent (amdgpu exposes via sysfs)
                var busyPath = Directory.GetFiles(dir, "../device/gpu_busy_percent",
                    SearchOption.AllDirectories).FirstOrDefault();
                if (busyPath is null)
                {
                    // alternative location
                    var deviceDir = Path.Combine(dir, "..", "device");
                    var busyFile = Path.Combine(deviceDir, "gpu_busy_percent");
                    if (File.Exists(busyFile))
                    {
                        var b = await File.ReadAllTextAsync(busyFile);
                        if (double.TryParse(b.Trim(), out var pct)) load = pct;
                    }
                }
                return (load, temp);
            }
        }
        catch { /* AMD not present */ }

        // NVIDIA via nvidia-smi (subprocess)
        try
        {
            var result = await RunCommandAsync("nvidia-smi",
                "--query-gpu=utilization.gpu,temperature.gpu --format=csv,noheader,nounits");
            if (result is not null)
            {
                var parts = result.Split(',');
                if (parts.Length >= 2
                    && double.TryParse(parts[0].Trim(), out var loadPct)
                    && double.TryParse(parts[1].Trim(), out var tempC))
                    return (loadPct, tempC);
            }
        }
        catch { /* NVIDIA not present */ }

        return (0.0, 0.0);
    }

    private static async Task<string?> RunCommandAsync(string cmd, string args)
    {
        using var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = cmd,
                Arguments = args,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };
        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();
        return output.Trim();
    }

    public void Dispose() { }
}
