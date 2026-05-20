#if WIN_PLATFORM
using Windows.Media.Control;
#endif

namespace OverlayEngine.Core.Services.NowPlaying;

#if WIN_PLATFORM

/// <summary>
/// Reads currently playing track from Windows System Media Transport Controls (SMTC).
/// Works with Spotify, Chrome, Edge, Windows Media Player, and any SMTC-aware app.
/// Requires Windows 10 1803+ (10.0.17134).
/// </summary>
public sealed class WindowsNowPlayingService : INowPlayingService
{
    public async Task<NowPlayingSnapshot> ReadAsync()
    {
        try
        {
            var manager = await GlobalSystemMediaTransportControlsSessionManager
                .RequestAsync();

            var session = manager.GetCurrentSession();
            if (session is null) return new NowPlayingSnapshot(null, null, false);

            var playbackInfo = session.GetPlaybackInfo();
            if (playbackInfo.PlaybackStatus
                != GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing)
                return new NowPlayingSnapshot(null, null, false);

            var props = await session.TryGetMediaPropertiesAsync();
            return new NowPlayingSnapshot(
                props.Artist ?? "",
                props.Title  ?? "",
                true);
        }
        catch { return new NowPlayingSnapshot(null, null, false); }
    }

    public void Dispose() { }
}

#else

// Stub for non-Windows compilation.
public sealed class WindowsNowPlayingService : INowPlayingService
{
    public Task<NowPlayingSnapshot> ReadAsync()
        => Task.FromResult(new NowPlayingSnapshot(null, null, false));
    public void Dispose() { }
}

#endif
