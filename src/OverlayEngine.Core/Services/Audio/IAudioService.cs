namespace OverlayEngine.Core.Services.Audio;

public record AudioSnapshot(double MicVolumeDb, bool IsMuted);

public interface IAudioService : IDisposable
{
    Task<AudioSnapshot> ReadAsync();
    /// <summary>Real-time peak input level 0.0–1.0, or -1.0 if unavailable.</summary>
    double PeakLevel { get; }
}
