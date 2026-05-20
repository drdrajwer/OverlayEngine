using System.Runtime.InteropServices;

namespace OverlayEngine.UI.Platform;

public sealed class WindowsWindowHelper : IWindowHelper
{
    private const int GWL_EXSTYLE      = -20;
    private const int WS_EX_LAYERED    = 0x00080000;
    private const int WS_EX_TRANSPARENT = 0x00000020;

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(nint hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(nint hWnd, int nIndex, int dwNewLong);

    public void EnableClickThrough(nint windowHandle)
    {
        var style = GetWindowLong(windowHandle, GWL_EXSTYLE);
        SetWindowLong(windowHandle, GWL_EXSTYLE, style | WS_EX_LAYERED | WS_EX_TRANSPARENT);
    }

    public void DisableClickThrough(nint windowHandle)
    {
        var style = GetWindowLong(windowHandle, GWL_EXSTYLE);
        SetWindowLong(windowHandle, GWL_EXSTYLE, style & ~WS_EX_TRANSPARENT);
    }
}
