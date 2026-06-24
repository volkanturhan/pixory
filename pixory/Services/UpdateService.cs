using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Diagnostics;

namespace pixory.Services;

/// <summary>
/// Checks GitHub Releases for a newer pixory and, when the user asks for it,
/// downloads and launches the installer for that release.
///
/// The check is deliberately quiet: any network, rate-limit or parsing problem
/// simply means "no update found" rather than an error in the user's face.
/// pixory should never nag, block startup, or break because GitHub happened to
/// be unreachable.
/// </summary>
public sealed class UpdateService
{
    // The public repo whose Releases we watch. The "latest" endpoint returns the
    // newest published (non-draft, non-prerelease) release — it 404s until the
    // very first release exists, which we treat as simply "no update".
    private const string LatestReleaseApi =
        "https://api.github.com/repos/volkanturhan/pixory/releases/latest";

    private static readonly HttpClient Http = CreateClient();

    /// <summary>A release found to be newer than the running build.</summary>
    public sealed record AvailableUpdate(Version Version, string InstallerUrl);

    /// <summary>
    /// Asks GitHub for the latest release and returns it only when it is newer
    /// than the running build and ships an installer asset. Returns null in every
    /// other case (already up to date, no release yet, offline, odd response…).
    /// </summary>
    public async Task<AvailableUpdate?> CheckForUpdateAsync()
    {
        try
        {
            // The check is tiny; cap it at 10s so a flaky network can't hang it.
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await using var stream = await Http.GetStreamAsync(LatestReleaseApi, cts.Token);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cts.Token);
            var release = doc.RootElement;

            // "v1.2.0" → 1.2.0; ignore anything that isn't a parseable version.
            if (!TryParseVersion(release.GetProperty("tag_name").GetString(), out var latest))
                return null;

            // Only a strictly newer build counts as an update worth offering.
            if (latest <= CurrentVersion)
                return null;

            // It also has to actually ship an installer we can launch.
            var installerUrl = FindInstallerUrl(release);
            if (installerUrl is null)
                return null;

            return new AvailableUpdate(latest, installerUrl);
        }
        catch
        {
            // Offline, rate-limited, no release yet, schema changed — all of these
            // mean the same thing to the user: there is nothing to update to.
            return null;
        }
    }

    /// <summary>
    /// Downloads the installer to a temp file and launches it. The caller should
    /// then quit pixory so the installer can replace the running files. Throws
    /// if the download or launch fails so the caller can tell the user.
    /// </summary>
    public async Task DownloadAndLaunchInstallerAsync(AvailableUpdate update)
    {
        // Save under %TEMP% with a recognisable, versioned name.
        var path = Path.Combine(Path.GetTempPath(),
            $"pixory-setup-v{update.Version.ToString(3)}.exe");

        // The installer is ~60 MB; allow a generous window and stream it straight
        // to disk instead of buffering it all in memory. (A short total timeout
        // here was the original "could not download" failure on slower links.)
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
        using (var response = await Http.GetAsync(update.InstallerUrl,
                   HttpCompletionOption.ResponseHeadersRead, cts.Token))
        {
            response.EnsureSuccessStatusCode();
            await using var file = File.Create(path);
            await response.Content.CopyToAsync(file, cts.Token);
        }

        // Hand off to the installer; it offers to close pixory and update it.
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }

    // The installer asset is the release .exe whose name mentions "setup"
    // (e.g. pixory-setup-v1.1.0.exe), as opposed to the portable exe.
    private static string? FindInstallerUrl(JsonElement release)
    {
        if (!release.TryGetProperty("assets", out var assets))
            return null;

        foreach (var asset in assets.EnumerateArray())
        {
            var name = asset.GetProperty("name").GetString() ?? string.Empty;
            if (name.Contains("setup", StringComparison.OrdinalIgnoreCase) &&
                name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                return asset.GetProperty("browser_download_url").GetString();
            }
        }
        return null;
    }

    // Strip an optional leading "v" and parse the rest as a version, normalised
    // to three parts so "1.0.0" and the assembly's "1.0.0.0" compare as equals.
    private static bool TryParseVersion(string? tag, out Version version)
    {
        version = new Version(0, 0, 0);
        if (string.IsNullOrWhiteSpace(tag))
            return false;
        if (!Version.TryParse(tag.TrimStart('v', 'V'), out var parsed))
            return false;
        version = Normalize(parsed);
        return true;
    }

    private static Version CurrentVersion =>
        Normalize(Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0));

    // Collapse to Major.Minor.Build, treating an undefined component as 0, so
    // comparisons don't trip over the "-1" that an unspecified part carries.
    private static Version Normalize(Version v) =>
        new(v.Major, v.Minor, v.Build < 0 ? 0 : v.Build);

    private static HttpClient CreateClient()
    {
        // No global timeout: the small version check and the large installer
        // download need very different limits, so each call sets its own through
        // a CancellationToken instead.
        var client = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        // GitHub's API rejects requests without a User-Agent; identify ourselves.
        client.DefaultRequestHeaders.UserAgent.ParseAdd("pixory-update-check");
        return client;
    }
}
