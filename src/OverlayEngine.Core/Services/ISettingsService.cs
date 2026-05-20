using OverlayEngine.Core.Models;

namespace OverlayEngine.Core.Services;

public interface ISettingsService
{
    AppSettings Current { get; }
    Task LoadAsync();
    Task SaveAsync();
}
