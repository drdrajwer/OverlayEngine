using System.Runtime.InteropServices;
using OverlayEngine.Core.Services.Audio;
using OverlayEngine.Core.Services.NowPlaying;
using OverlayEngine.Core.Services.Sensors;

namespace OverlayEngine.Core.Platform;

public static class PlatformServiceFactory
{
    public static ISensorService CreateSensorService()
        => RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
            ? new LinuxSensorService()
            : new WindowsSensorService();

    public static IAudioService CreateAudioService()
        => RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
            ? new LinuxAudioService()
            : new WindowsAudioService();

    public static INowPlayingService CreateNowPlayingService()
        => RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
            ? new LinuxNowPlayingService()
            : new WindowsNowPlayingService();
}
