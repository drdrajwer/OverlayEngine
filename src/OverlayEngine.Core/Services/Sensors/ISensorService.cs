namespace OverlayEngine.Core.Services.Sensors;

public record SensorSnapshot(
    double CpuLoadPercent,
    double CpuTempCelsius,
    double GpuLoadPercent,
    double GpuTempCelsius);

public interface ISensorService : IDisposable
{
    Task<SensorSnapshot> ReadAsync();
}
