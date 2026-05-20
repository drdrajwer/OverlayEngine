namespace OverlayEngine.UI.Platform;

public interface IWindowHelper
{
    /// <summary>Makes the window invisible to mouse input (click-through).</summary>
    void EnableClickThrough(nint windowHandle);

    /// <summary>Restores normal mouse interaction.</summary>
    void DisableClickThrough(nint windowHandle);
}
