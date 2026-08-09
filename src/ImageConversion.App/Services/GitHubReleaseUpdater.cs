using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ImageConversion.App.Services;

public sealed class GitHubReleaseUpdater
{
    public static readonly Uri LatestReleaseUri = new("https://api.github.com/repos/TrickyBanter/SE-Image-Converter/releases/latest");

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient httpClient;

    public GitHubReleaseUpdater(HttpClient? httpClient = null)
    {
        this.httpClient = httpClient ?? new HttpClient();
        this.httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd("SE-Image-Converter-Updater");
        this.httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
    }

    public static Version CurrentVersion =>
        Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0, 0);

    public async Task<GitHubUpdate?> CheckForUpdateAsync(CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await httpClient.GetAsync(LatestReleaseUri, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new UpdateCheckException("No published GitHub release was found.");
        }

        response.EnsureSuccessStatusCode();

        await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        GitHubReleaseResponse? release = await JsonSerializer.DeserializeAsync<GitHubReleaseResponse>(stream, SerializerOptions, cancellationToken);

        return GitHubUpdateSelector.TryCreateUpdate(release, CurrentVersion, out GitHubUpdate? update)
            ? update
            : null;
    }

    public async Task<string> DownloadInstallerAsync(
        GitHubUpdate update,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await httpClient.GetAsync(
            update.InstallerAsset.DownloadUrl,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        string extension = Path.GetExtension(update.InstallerAsset.Name);
        string fileName = $"SEImageConverter-{update.Version}{extension}";
        string updateDirectory = Path.Combine(Path.GetTempPath(), "SEImageConverter", "Updates");
        Directory.CreateDirectory(updateDirectory);
        string installerPath = Path.Combine(updateDirectory, fileName);

        await using Stream download = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using FileStream file = File.Create(installerPath);

        long? totalBytes = response.Content.Headers.ContentLength;
        byte[] buffer = new byte[81920];
        long bytesRead = 0;
        int read;

        while ((read = await download.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await file.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            bytesRead += read;

            if (totalBytes is > 0)
            {
                progress?.Report((double)bytesRead / totalBytes.Value * 100);
            }
        }

        progress?.Report(100);
        return installerPath;
    }

    public void LaunchInstaller(string installerPath)
    {
        Process? process = Process.Start(new ProcessStartInfo
        {
            FileName = installerPath,
            UseShellExecute = true,
        });

        if (process is null)
        {
            throw new InvalidOperationException("Windows could not start the installer.");
        }
    }
}

public sealed class UpdateCheckException : Exception
{
    public UpdateCheckException(string message)
        : base(message)
    {
    }
}

public sealed record GitHubUpdate(
    Version Version,
    string TagName,
    Uri ReleasePageUrl,
    GitHubReleaseAsset InstallerAsset);

public sealed record GitHubReleaseAsset(string Name, Uri DownloadUrl);

public sealed class GitHubReleaseResponse
{
    [JsonPropertyName("tag_name")]
    public string? TagName { get; init; }

    [JsonPropertyName("html_url")]
    public string? HtmlUrl { get; init; }

    [JsonPropertyName("draft")]
    public bool Draft { get; init; }

    [JsonPropertyName("prerelease")]
    public bool Prerelease { get; init; }

    [JsonPropertyName("assets")]
    public IReadOnlyList<GitHubReleaseAssetResponse> Assets { get; init; } = [];
}

public sealed class GitHubReleaseAssetResponse
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("browser_download_url")]
    public string? BrowserDownloadUrl { get; init; }
}

public static class GitHubUpdateSelector
{
    public static bool TryCreateUpdate(
        GitHubReleaseResponse? release,
        Version currentVersion,
        out GitHubUpdate? update)
    {
        update = null;

        if (release is null || release.Draft || release.Prerelease)
        {
            return false;
        }

        if (!TryParseVersion(release.TagName, out Version? releaseVersion) ||
            releaseVersion <= NormalizeVersion(currentVersion))
        {
            return false;
        }

        if (!Uri.TryCreate(release.HtmlUrl, UriKind.Absolute, out Uri? releasePageUrl) ||
            !TrySelectInstallerAsset(release.Assets, out GitHubReleaseAsset? installerAsset))
        {
            return false;
        }

        update = new GitHubUpdate(releaseVersion, release.TagName!, releasePageUrl, installerAsset);
        return true;
    }

    public static bool TryParseVersion(string? tagName, [NotNullWhen(true)] out Version? version)
    {
        version = null;

        if (string.IsNullOrWhiteSpace(tagName))
        {
            return false;
        }

        string normalized = tagName.Trim();

        if (normalized.StartsWith('v') || normalized.StartsWith('V'))
        {
            normalized = normalized[1..];
        }

        if (!Version.TryParse(normalized, out Version? parsed))
        {
            return false;
        }

        version = NormalizeVersion(parsed);
        return true;
    }

    public static bool TrySelectInstallerAsset(
        IEnumerable<GitHubReleaseAssetResponse> assets,
        [NotNullWhen(true)] out GitHubReleaseAsset? installerAsset)
    {
        installerAsset = assets
            .Select(TryCreateAsset)
            .Where(asset => asset is not null)
            .OrderBy(asset => GetAssetPriority(asset!.Name))
            .FirstOrDefault();

        return installerAsset is not null;
    }

    private static GitHubReleaseAsset? TryCreateAsset(GitHubReleaseAssetResponse asset)
    {
        if (string.IsNullOrWhiteSpace(asset.Name) ||
            string.IsNullOrWhiteSpace(asset.BrowserDownloadUrl) ||
            GetAssetPriority(asset.Name) == int.MaxValue ||
            !Uri.TryCreate(asset.BrowserDownloadUrl, UriKind.Absolute, out Uri? downloadUrl))
        {
            return null;
        }

        return new GitHubReleaseAsset(asset.Name, downloadUrl);
    }

    private static int GetAssetPriority(string name)
    {
        if (name.EndsWith(".msixbundle", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (name.EndsWith(".msix", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        return int.MaxValue;
    }

    private static Version NormalizeVersion(Version version) => new(
        Math.Max(version.Major, 0),
        Math.Max(version.Minor, 0),
        Math.Max(version.Build, 0),
        Math.Max(version.Revision, 0));
}
