using System.Runtime.InteropServices;

namespace OverlayEngine.UI.Platform;

public static class WindowHelperFactory
{
    public static IWindowHelper Create()
        => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? new WindowsWindowHelper()
            : new LinuxWindowHelper();
}
