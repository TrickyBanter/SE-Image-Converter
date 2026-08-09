using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImageConversion.App.Services;
using ImageConversion.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Streams;

namespace ImageConversion.App.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly ImageToLcdConverter converter = new();
    private readonly GitHubReleaseUpdater updater = new();
    private byte[]? sourceBytes;
    private GitHubUpdate? availableUpdate;

    [ObservableProperty]
    public partial ImageSource? SourcePreview { get; set; }

    [ObservableProperty]
    public partial ImageSource? ConvertedPreview { get; set; }

    [ObservableProperty]
    public partial string SourceSummary { get; set; } = "No image selected.";

    [ObservableProperty]
    public partial string ConvertedText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ResultSizeSummary { get; set; } = "-";

    [ObservableProperty]
    public partial string CharacterCountSummary { get; set; } = "0 characters";

    [ObservableProperty]
    public partial string LineCountSummary { get; set; } = "0 lines";

    [ObservableProperty]
    public partial PanelPreset SelectedPanelPreset { get; set; } = PanelPreset.Defaults[0];

    [ObservableProperty]
    public partial ResizeMode SelectedResizeMode { get; set; } = ResizeMode.Fit;

    [ObservableProperty]
    public partial bool MaintainAspectRatio { get; set; } = true;

    [ObservableProperty]
    public partial bool PreserveTransparency { get; set; } = true;

    [ObservableProperty]
    public partial string StatusTitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial InfoBarSeverity StatusSeverity { get; set; } = InfoBarSeverity.Informational;

    [ObservableProperty]
    public partial bool IsOutputStale { get; set; }

    [ObservableProperty]
    public partial bool IsCheckingForUpdates { get; set; }

    [ObservableProperty]
    public partial bool IsDownloadingUpdate { get; set; }

    [ObservableProperty]
    public partial bool IsUpdateAvailable { get; set; }

    [ObservableProperty]
    public partial double UpdateDownloadProgress { get; set; }

    [ObservableProperty]
    public partial string AvailableUpdateSummary { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string UpdateStatusMessage { get; set; } = "Checking GitHub Releases on startup.";

    public ObservableCollection<PanelPreset> PanelPresets { get; } = new(PanelPreset.Defaults);

    public ObservableCollection<ResizeMode> ResizeModes { get; } = new(Enum.GetValues<ResizeMode>());

    public ObservableCollection<DitheringModeOption> DitheringModes { get; } =
    [
        new(DitheringMode.None, "None", "Nearest color match with no dithering. Fast and crisp, but gradients can band."),
        new(DitheringMode.FloydSteinberg, "Floyd-Steinberg", "Classic error diffusion with sharp detail and balanced grain. A strong general-purpose choice."),
        new(DitheringMode.Atkinson, "Atkinson", "Apple-style error diffusion that preserves contrast and creates a lighter, more stylized texture."),
        new(DitheringMode.SierraLite, "Sierra Lite", "Compact error diffusion that is quick, clean, and often works well on small LCD panels."),
        new(DitheringMode.Stucki, "Stucki", "Wide error diffusion that smooths gradients and photos, especially on larger panels."),
        new(DitheringMode.Burkes, "Burkes", "A sharper, cheaper cousin of Stucki that balances photo smoothness with good edge detail."),
        new(DitheringMode.OrderedBayer2, "Ordered Bayer 2x2", "Very coarse ordered pattern. Useful for a chunky pixel-art look."),
        new(DitheringMode.OrderedBayer4, "Ordered Bayer 4x4", "Predictable ordered pattern with moderate texture. Good for logos and UI images."),
        new(DitheringMode.OrderedBayer8, "Ordered Bayer 8x8", "Finer ordered pattern with less visible grid texture than 2x2 or 4x4."),
    ];

    [ObservableProperty]
    public partial DitheringModeOption SelectedDitheringMode { get; set; }

    public bool HasImage => sourceBytes is not null;

    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusTitle) || !string.IsNullOrWhiteSpace(StatusMessage);

    public bool CanExport => HasConvertedText();

    public string CurrentVersionSummary => $"Version {FormatVersion(GitHubReleaseUpdater.CurrentVersion)}";

    public Visibility SourcePlaceholderVisibility => SourcePreview is null ? Visibility.Visible : Visibility.Collapsed;

    public Visibility ConvertedPlaceholderVisibility => ConvertedPreview is null ? Visibility.Visible : Visibility.Collapsed;

    public Visibility UpdateAvailableVisibility => IsUpdateAvailable ? Visibility.Visible : Visibility.Collapsed;

    public Visibility UpdateDownloadVisibility => IsDownloadingUpdate ? Visibility.Visible : Visibility.Collapsed;

    public string InstallUpdateButtonText => availableUpdate is null
        ? "Download and install"
        : $"Download and install {availableUpdate.TagName}";

    public MainWindowViewModel()
    {
        SelectedDitheringMode = DitheringModes[0];
    }

    public async Task CheckForUpdatesOnStartupAsync()
    {
        await CheckForUpdatesAsync(showUpToDateMessage: false);
    }

    public async Task CheckForUpdatesManuallyAsync()
    {
        await CheckForUpdatesAsync(showUpToDateMessage: true);
    }

    public async Task LoadImageAsync(string path)
    {
        try
        {
            sourceBytes = await File.ReadAllBytesAsync(path);
            SourcePreview = new BitmapImage(new Uri(path));
            SourceSummary = Path.GetFileName(path);
            ConvertedText = string.Empty;
            ConvertedPreview = null;
            ResultSizeSummary = "-";
            CharacterCountSummary = "0 characters";
            LineCountSummary = "0 lines";
            IsOutputStale = false;
            SetStatus("Loaded", "Image ready to convert.", InfoBarSeverity.Success);
            OnPropertyChanged(nameof(HasImage));
            ConvertCommand.NotifyCanExecuteChanged();
        }
        catch (Exception ex)
        {
            sourceBytes = null;
            SourcePreview = null;
            SetStatus("Could not load image", ex.Message, InfoBarSeverity.Error);
            OnPropertyChanged(nameof(HasImage));
            ConvertCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand(CanExecute = nameof(CanConvert))]
    private async Task ConvertAsync()
    {
        if (sourceBytes is null)
        {
            return;
        }

        try
        {
            ConversionResult result = await Task.Run(() => converter.Convert(sourceBytes, BuildOptions()));
            ConvertedText = result.Text;
            ConvertedPreview = await CreateImageSourceAsync(result.PreviewPng);
            ResultSizeSummary = $"{result.Width} x {result.Height}";
            CharacterCountSummary = $"{result.EstimatedCharacterCount:N0} characters";
            LineCountSummary = $"{result.Height:N0} lines";
            IsOutputStale = false;
            SetStatus("Converted", "The LCD string is ready to copy into Space Engineers.", InfoBarSeverity.Success);
        }
        catch (Exception ex)
        {
            SetStatus("Conversion failed", ex.Message, InfoBarSeverity.Error);
        }
    }

    [RelayCommand(CanExecute = nameof(HasConvertedText))]
    private void Copy()
    {
        DataPackage package = new();
        package.SetText(ConvertedText);
        Clipboard.SetContent(package);
        SetStatus("Copied", "Converted string copied to the clipboard.", InfoBarSeverity.Success);
    }

    public async Task ExportTextAsync(string path)
    {
        if (!HasConvertedText())
        {
            SetStatus("Nothing to export", "Convert an image before exporting.", InfoBarSeverity.Warning);
            return;
        }

        await File.WriteAllTextAsync(path, ConvertedText);
        SetStatus("Exported", $"Saved {Path.GetFileName(path)}.", InfoBarSeverity.Success);
    }

    [RelayCommand(CanExecute = nameof(CanCheckForUpdates))]
    private async Task CheckForUpdatesAsync()
    {
        await CheckForUpdatesManuallyAsync();
    }

    [RelayCommand(CanExecute = nameof(CanInstallUpdate))]
    private async Task InstallUpdateAsync()
    {
        if (availableUpdate is null)
        {
            return;
        }

        try
        {
            IsDownloadingUpdate = true;
            UpdateDownloadProgress = 0;
            UpdateStatusMessage = $"Downloading {availableUpdate.InstallerAsset.Name}...";
            NotifyUpdateStateChanged();

            Progress<double> progress = new(value => UpdateDownloadProgress = value);
            string installerPath = await updater.DownloadInstallerAsync(availableUpdate, progress);

            UpdateStatusMessage = "Opening the installer...";
            updater.LaunchInstaller(installerPath);
            Application.Current.Exit();
        }
        catch (Exception ex)
        {
            UpdateStatusMessage = $"Could not install update: {ex.Message}";
        }
        finally
        {
            IsDownloadingUpdate = false;
            NotifyUpdateStateChanged();
        }
    }

    partial void OnConvertedTextChanged(string value)
    {
        CharacterCountSummary = $"{value.Length:N0} characters";
        LineCountSummary = string.IsNullOrEmpty(value)
            ? "0 lines"
            : $"{value.Split(Environment.NewLine).Length:N0} lines";
        CopyCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(CanExport));
    }

    partial void OnSourcePreviewChanged(ImageSource? value)
    {
        OnPropertyChanged(nameof(SourcePlaceholderVisibility));
    }

    partial void OnConvertedPreviewChanged(ImageSource? value)
    {
        OnPropertyChanged(nameof(ConvertedPlaceholderVisibility));
    }

    partial void OnIsUpdateAvailableChanged(bool value)
    {
        OnPropertyChanged(nameof(UpdateAvailableVisibility));
        OnPropertyChanged(nameof(InstallUpdateButtonText));
        InstallUpdateCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsCheckingForUpdatesChanged(bool value)
    {
        CheckForUpdatesCommand.NotifyCanExecuteChanged();
        InstallUpdateCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsDownloadingUpdateChanged(bool value)
    {
        OnPropertyChanged(nameof(UpdateDownloadVisibility));
        CheckForUpdatesCommand.NotifyCanExecuteChanged();
        InstallUpdateCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedPanelPresetChanged(PanelPreset value) => QueueStaleConversionMessage();

    partial void OnSelectedResizeModeChanged(ResizeMode value) => QueueStaleConversionMessage();

    partial void OnSelectedDitheringModeChanged(DitheringModeOption value) => QueueStaleConversionMessage();

    partial void OnMaintainAspectRatioChanged(bool value) => QueueStaleConversionMessage();

    partial void OnPreserveTransparencyChanged(bool value) => QueueStaleConversionMessage();

    private bool CanConvert() => sourceBytes is not null;

    private bool HasConvertedText() => !string.IsNullOrEmpty(ConvertedText);

    private bool CanCheckForUpdates() => !IsCheckingForUpdates && !IsDownloadingUpdate;

    private bool CanInstallUpdate() => availableUpdate is not null && !IsCheckingForUpdates && !IsDownloadingUpdate;

    private async Task CheckForUpdatesAsync(bool showUpToDateMessage)
    {
        if (IsCheckingForUpdates || IsDownloadingUpdate)
        {
            return;
        }

        try
        {
            IsCheckingForUpdates = true;
            UpdateStatusMessage = "Checking GitHub Releases...";

            availableUpdate = await updater.CheckForUpdateAsync();
            IsUpdateAvailable = availableUpdate is not null;

            if (availableUpdate is not null)
            {
                AvailableUpdateSummary = $"{availableUpdate.TagName} is available.";
                UpdateStatusMessage = $"Update {availableUpdate.TagName} is ready to download.";
            }
            else
            {
                AvailableUpdateSummary = string.Empty;
                UpdateStatusMessage = showUpToDateMessage
                    ? "You are running the latest version."
                    : "No update available.";
            }
        }
        catch (Exception ex)
        {
            UpdateStatusMessage = $"Could not check for updates: {ex.Message}";
        }
        finally
        {
            IsCheckingForUpdates = false;
            NotifyUpdateStateChanged();
        }
    }

    private ConversionOptions BuildOptions() => new()
    {
        PanelPreset = SelectedPanelPreset,
        ResizeMode = SelectedResizeMode,
        DitheringMode = SelectedDitheringMode.Mode,
        MaintainAspectRatio = MaintainAspectRatio,
        PreserveTransparency = PreserveTransparency,
    };

    private void QueueStaleConversionMessage()
    {
        if (HasImage && HasConvertedText())
        {
            IsOutputStale = true;
        }
    }

    private void SetStatus(string title, string message, InfoBarSeverity severity)
    {
        StatusTitle = title;
        StatusMessage = message;
        StatusSeverity = severity;
        OnPropertyChanged(nameof(HasStatusMessage));
    }

    private void NotifyUpdateStateChanged()
    {
        OnPropertyChanged(nameof(UpdateAvailableVisibility));
        OnPropertyChanged(nameof(UpdateDownloadVisibility));
        OnPropertyChanged(nameof(InstallUpdateButtonText));
        CheckForUpdatesCommand.NotifyCanExecuteChanged();
        InstallUpdateCommand.NotifyCanExecuteChanged();
    }

    private static string FormatVersion(Version version)
    {
        return version.Build >= 0
            ? $"{version.Major}.{version.Minor}.{version.Build}"
            : $"{version.Major}.{version.Minor}";
    }

    private static async Task<ImageSource> CreateImageSourceAsync(byte[] pngBytes)
    {
        BitmapImage image = new();

        using InMemoryRandomAccessStream stream = new();
        using (DataWriter writer = new(stream.GetOutputStreamAt(0)))
        {
            writer.WriteBytes(pngBytes);
            await writer.StoreAsync();
        }

        stream.Seek(0);
        await image.SetSourceAsync(stream);
        return image;
    }
}

public sealed record DitheringModeOption(DitheringMode Mode, string Name, string Description);
