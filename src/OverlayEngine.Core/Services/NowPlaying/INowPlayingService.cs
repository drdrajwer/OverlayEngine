namespace OverlayEngine.Core.Services.NowPlaying;

public record NowPlayingSnapshot(string? Artist, string? Title, bool IsPlaying);

public interface INowPlayingService : IDisposable
{
    Task<NowPlayingSnapshot> ReadAsync();
}
