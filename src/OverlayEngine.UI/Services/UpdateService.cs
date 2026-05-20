using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace OverlayEngine.UI.Services;

internal sealed class UpdateService : IDisposable
{
    private const string ApiUrl = "https://api.github.com/repos/drdrajwer/OverlayEngine/releases/latest";

    private readonly HttpClient _http = new();

    public UpdateService()
    {
        _http.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("OverlayEngine", Program.AppVersion));
    }

    public async Task<string?> CheckForUpdateAsync()
    {
        var json   = await _http.GetStringAsync(ApiUrl);
        using var doc = JsonDocument.Parse(json);
        var tag    = doc.RootElement.GetProperty("tag_name").GetString(); // "v1.2.0"
        if (tag is null) return null;

        var remote = tag.TrimStart('v');
        return IsNewer(remote, Program.AppVersion) ? remote : null;
    }

    public async Task DownloadAndInstallAsync(string newVersion, Action<int> onProgress)
    {
        // Find the installer asset on GitHub
        var json = await _http.GetStringAsync(ApiUrl);
        using var doc = JsonDocument.Parse(json);

        string? downloadUrl = null;
        foreach (var asset in doc.RootElement.GetProperty("assets").EnumerateArray())
        {
            var name = asset.GetProperty("name").GetString() ?? "";
            if (name.Contains("Setup", StringComparison.OrdinalIgnoreCase) &&
                name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                downloadUrl = asset.GetProperty("browser_download_url").GetString();
                break;
            }
        }

        if (downloadUrl is null)
            throw new InvalidOperationException("Nie znaleziono instalatora w GitHub Release.");

        // Download to temp
        var tempExe = Path.Combine(Path.GetTempPath(), $"OverlayEngine-{newVersion}-Setup.exe");

        using var response = await _http.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        var total = response.Content.Headers.ContentLength ?? -1L;

        await using var src  = await response.Content.ReadAsStreamAsync();
        await using var dest = File.Create(tempExe);

        var buf        = new byte[81_920];
        long received  = 0;
        int  read;
        while ((read = await src.ReadAsync(buf)) > 0)
        {
            await dest.WriteAsync(buf.AsMemory(0, read));
            received += read;
            if (total > 0) onProgress((int)(received * 100 / total));
        }
        dest.Close();

        // Run installer silently, then exit current process
        Process.Start(new ProcessStartInfo
        {
            FileName         = tempExe,
            Arguments        = "/VERYSILENT /NORESTART /CLOSEAPPLICATIONS",
            UseShellExecute  = true,
        });
        Environment.Exit(0);
    }

    private static bool IsNewer(string remote, string local)
    {
        if (Version.TryParse(remote, out var r) && Version.TryParse(local, out var l))
            return r > l;
        return false;
    }

    public void Dispose() => _http.Dispose();
}
