namespace OverlayEngine.Core.Services.Network;

public record NetworkSnapshot(double DownloadBytesPerSec, double UploadBytesPerSec);

public interface INetworkService : IDisposable
{
    Task<NetworkSnapshot> ReadAsync();
}
