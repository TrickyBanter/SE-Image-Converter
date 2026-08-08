using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    private byte[]? sourceBytes;

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
    public partial DitheringMode SelectedDitheringMode { get; set; } = DitheringMode.FloydSteinberg;

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

    public ObservableCollection<PanelPreset> PanelPresets { get; } = new(PanelPreset.Defaults);

    public ObservableCollection<ResizeMode> ResizeModes { get; } = new(Enum.GetValues<ResizeMode>());

    public ObservableCollection<DitheringMode> DitheringModes { get; } = new(Enum.GetValues<DitheringMode>());

    public bool HasImage => sourceBytes is not null;

    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusTitle) || !string.IsNullOrWhiteSpace(StatusMessage);

    public Visibility SourcePlaceholderVisibility => SourcePreview is null ? Visibility.Visible : Visibility.Collapsed;

    public Visibility ConvertedPlaceholderVisibility => ConvertedPreview is null ? Visibility.Visible : Visibility.Collapsed;

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

    partial void OnConvertedTextChanged(string value)
    {
        CharacterCountSummary = $"{value.Length:N0} characters";
        LineCountSummary = string.IsNullOrEmpty(value)
            ? "0 lines"
            : $"{value.Split(Environment.NewLine).Length:N0} lines";
        CopyCommand.NotifyCanExecuteChanged();
    }

    partial void OnSourcePreviewChanged(ImageSource? value)
    {
        OnPropertyChanged(nameof(SourcePlaceholderVisibility));
    }

    partial void OnConvertedPreviewChanged(ImageSource? value)
    {
        OnPropertyChanged(nameof(ConvertedPlaceholderVisibility));
    }

    partial void OnSelectedPanelPresetChanged(PanelPreset value) => QueueStaleConversionMessage();

    partial void OnSelectedResizeModeChanged(ResizeMode value) => QueueStaleConversionMessage();

    partial void OnSelectedDitheringModeChanged(DitheringMode value) => QueueStaleConversionMessage();

    partial void OnMaintainAspectRatioChanged(bool value) => QueueStaleConversionMessage();

    partial void OnPreserveTransparencyChanged(bool value) => QueueStaleConversionMessage();

    private bool CanConvert() => sourceBytes is not null;

    private bool HasConvertedText() => !string.IsNullOrEmpty(ConvertedText);

    private ConversionOptions BuildOptions() => new()
    {
        PanelPreset = SelectedPanelPreset,
        ResizeMode = SelectedResizeMode,
        DitheringMode = SelectedDitheringMode,
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
