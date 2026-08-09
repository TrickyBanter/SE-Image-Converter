using ImageConversion.App.Services;
using System.Net;
using Xunit;

namespace ImageConversion.App.Tests;

public sealed class GitHubUpdateSelectorTests
{
    [Fact]
    public async Task CheckForUpdateShowsFriendlyMessageWhenNoReleaseExists()
    {
        HttpClient httpClient = new(new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound)));
        GitHubReleaseUpdater updater = new(httpClient);

        UpdateCheckException exception = await Assert.ThrowsAsync<UpdateCheckException>(
            () => updater.CheckForUpdateAsync(TestContext.Current.CancellationToken));
        Assert.Equal("No published GitHub release was found.", exception.Message);
    }

    [Theory]
    [InlineData("v1.2.3", 1, 2, 3)]
    [InlineData("1.2.3", 1, 2, 3)]
    [InlineData("V2.0.1", 2, 0, 1)]
    public void ParsesReleaseTagVersions(string tagName, int major, int minor, int build)
    {
        Assert.True(GitHubUpdateSelector.TryParseVersion(tagName, out Version? version));
        Assert.Equal(new Version(major, minor, build, 0), version);
    }

    [Theory]
    [InlineData("")]
    [InlineData("latest")]
    [InlineData("v1.2.beta")]
    public void RejectsInvalidReleaseTagVersions(string tagName)
    {
        Assert.False(GitHubUpdateSelector.TryParseVersion(tagName, out Version? version));
        Assert.Null(version);
    }

    [Fact]
    public void SelectsSetupExeBeforeOtherExeAssets()
    {
        GitHubReleaseAssetResponse[] assets =
        [
            Asset("SEImageConverter-portable-helper.exe", "https://example.com/helper.exe"),
            Asset("SEImageConverter-Setup-1.2.3.exe", "https://example.com/setup.exe"),
        ];

        Assert.True(GitHubUpdateSelector.TrySelectInstallerAsset(assets, out GitHubReleaseAsset? installerAsset));
        Assert.NotNull(installerAsset);
        Assert.Equal("SEImageConverter-Setup-1.2.3.exe", installerAsset.Name);
    }

    [Fact]
    public void FallsBackToAnyExeInstaller()
    {
        GitHubReleaseAssetResponse[] assets =
        [
            Asset("SEImageConverter.zip", "https://example.com/app.zip"),
            Asset("SEImageConverter-1.2.3.exe", "https://example.com/app.exe"),
        ];

        Assert.True(GitHubUpdateSelector.TrySelectInstallerAsset(assets, out GitHubReleaseAsset? installerAsset));
        Assert.NotNull(installerAsset);
        Assert.Equal("SEImageConverter-1.2.3.exe", installerAsset.Name);
    }

    [Fact]
    public void IgnoresNonInstallerAssets()
    {
        GitHubReleaseAssetResponse[] assets =
        [
            Asset("SEImageConverter.zip", "https://example.com/app.zip"),
            Asset("checksums.txt", "https://example.com/checksums.txt"),
            Asset("SEImageConverter.msixbundle", "https://example.com/app.msixbundle"),
        ];

        Assert.False(GitHubUpdateSelector.TrySelectInstallerAsset(assets, out GitHubReleaseAsset? installerAsset));
        Assert.Null(installerAsset);
    }

    [Fact]
    public void CreatesUpdateOnlyWhenReleaseIsNewer()
    {
        GitHubReleaseResponse release = Release("v1.2.0");

        Assert.True(GitHubUpdateSelector.TryCreateUpdate(release, new Version(1, 1, 0, 0), out GitHubUpdate? update));
        Assert.NotNull(update);
        Assert.Equal(new Version(1, 2, 0, 0), update.Version);

        Assert.False(GitHubUpdateSelector.TryCreateUpdate(release, new Version(1, 2, 0, 0), out _));
        Assert.False(GitHubUpdateSelector.TryCreateUpdate(release, new Version(1, 3, 0, 0), out _));
    }

    [Fact]
    public void IgnoresDraftAndPrereleaseReleases()
    {
        Assert.False(GitHubUpdateSelector.TryCreateUpdate(
            Release("v2.0.0", draft: true),
            new Version(1, 0, 0, 0),
            out _));

        Assert.False(GitHubUpdateSelector.TryCreateUpdate(
            Release("v2.0.0", prerelease: true),
            new Version(1, 0, 0, 0),
            out _));
    }

    private static GitHubReleaseResponse Release(string tagName, bool draft = false, bool prerelease = false) => new()
    {
        TagName = tagName,
        HtmlUrl = "https://github.com/TrickyBanter/SE-Image-Converter/releases/tag/" + tagName,
        Draft = draft,
        Prerelease = prerelease,
        Assets =
        [
            Asset("SEImageConverter-Setup-1.2.0.exe", "https://example.com/setup.exe"),
        ],
    };

    private static GitHubReleaseAssetResponse Asset(string name, string downloadUrl) => new()
    {
        Name = name,
        BrowserDownloadUrl = downloadUrl,
    };

    private sealed class StubHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(response);
        }
    }
}
