#if NET8_0_WINDOWS
using Velopack;
using Velopack.Sources;

namespace OverlayEngine.UI.Services;

// ── TODO ──────────────────────────────────────────────────────────────────────
// Przed wgraniem pierwszej wersji na GitHub:
//   1. Zmień GithubRepoUrl na adres swojego repozytorium
//   2. Wgraj release na GitHub używając: build_installer.bat
// ─────────────────────────────────────────────────────────────────────────────
internal sealed class UpdateService
{
    private const string GithubRepoUrl = "https://github.com/YOUR_GITHUB_USERNAME/OverlayEngine";

    public async Task<string?> CheckForUpdateAsync()
    {
        var mgr = new UpdateManager(new GithubSource(GithubRepoUrl, null, false));
        var info = await mgr.CheckForUpdatesAsync();
        return info?.TargetFullRelease.Version.ToString();
    }

    public async Task DownloadAndApplyAsync(Action<int> onProgress)
    {
        var mgr = new UpdateManager(new GithubSource(GithubRepoUrl, null, false));
        var info = await mgr.CheckForUpdatesAsync();
        if (info is null) return;
        await mgr.DownloadUpdatesAsync(info, onProgress);
        mgr.ApplyUpdatesAndRestart(info);
    }
}
#endif
