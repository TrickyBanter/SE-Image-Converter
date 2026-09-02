using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Reflection;
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
    private readonly JumpDriveCalculator jumpDriveCalculator = new();
    private readonly ResourceCalculator resourceCalculator = new(SpaceEngineersBlockCatalog.DefaultBlocks);
    private readonly ResourceRecipeStorageService resourceRecipeStorage;
    private readonly GitHubReleaseUpdater updater = new();
    private readonly IAppSettingsStore settingsStore;
    private byte[]? sourceBytes;
    private GitHubUpdate? availableUpdate;
    private AppSettings settings;
    private bool isLoadingSettings;

    [ObservableProperty]
    public partial MainFeature CurrentFeature { get; set; } = MainFeature.ImageConverter;

    [ObservableProperty]
    public partial bool CheckForUpdatesOnStartup { get; set; }

    [ObservableProperty]
    public partial FeatureOption SelectedDefaultAppView { get; set; }

    [ObservableProperty]
    public partial ThemeOption SelectedTheme { get; set; }

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

    [ObservableProperty]
    public partial string StartGps { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StartX { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StartY { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StartZ { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DestinationGps { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DestinationX { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DestinationY { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DestinationZ { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int SelectedJumpDriveCount { get; set; } = 1;

    [ObservableProperty]
    public partial JumpDriveTypeOption SelectedJumpDriveType { get; set; }

    [ObservableProperty]
    public partial string ShipMassKg { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string JumpStatusTitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string JumpStatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial InfoBarSeverity JumpStatusSeverity { get; set; } = InfoBarSeverity.Informational;

    [ObservableProperty]
    public partial string JumpDistanceSummary { get; set; } = "-";

    [ObservableProperty]
    public partial string JumpRangeSummary { get; set; } = "-";

    [ObservableProperty]
    public partial string JumpCountSummary { get; set; } = "-";

    [ObservableProperty]
    public partial string JumpTravelTimeSummary { get; set; } = "-";

    [ObservableProperty]
    public partial string BlockSearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial SpaceEngineersBlockDefinition? SelectedResourceBlock { get; set; }

    [ObservableProperty]
    public partial int SelectedResourceBlockQuantity { get; set; } = 1;

    [ObservableProperty]
    public partial ResourceRecipeViewModel? SelectedSavedResourceRecipe { get; set; }

    [ObservableProperty]
    public partial int SelectedResourceRecipeQuantity { get; set; } = 1;

    [ObservableProperty]
    public partial string ResourceStatusTitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ResourceStatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial InfoBarSeverity ResourceStatusSeverity { get; set; } = InfoBarSeverity.Informational;

    public bool HasImage => sourceBytes is not null;

    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusTitle) || !string.IsNullOrWhiteSpace(StatusMessage);

    public bool HasJumpStatusMessage => !string.IsNullOrWhiteSpace(JumpStatusTitle) || !string.IsNullOrWhiteSpace(JumpStatusMessage);

    public bool HasResourceStatusMessage => !string.IsNullOrWhiteSpace(ResourceStatusTitle) || !string.IsNullOrWhiteSpace(ResourceStatusMessage);

    public bool CanExport => HasConvertedText();

    public string CurrentVersionSummary => $"v{FormatVersion(GitHubReleaseUpdater.CurrentVersion)}";

    public string AppDateSummary => GetAppDate().ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);

    public Visibility ImageConverterVisibility => CurrentFeature == MainFeature.ImageConverter ? Visibility.Visible : Visibility.Collapsed;

    public Visibility JumpDriveCalculatorVisibility => CurrentFeature == MainFeature.JumpDriveCalculator ? Visibility.Visible : Visibility.Collapsed;

    public Visibility ResourceCalculatorVisibility => CurrentFeature == MainFeature.ResourceCalculator ? Visibility.Visible : Visibility.Collapsed;

    public Visibility SettingsVisibility => CurrentFeature == MainFeature.Settings ? Visibility.Visible : Visibility.Collapsed;

    public Visibility ResourceBuildEmptyVisibility => ResourceBuildRows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

    public Visibility ResourceRecipeRowsEmptyVisibility => ResourceRecipeRows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

    public Visibility ResourceTotalsEmptyVisibility => ResourceComponentTotals.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

    public Visibility SourcePlaceholderVisibility => SourcePreview is null ? Visibility.Visible : Visibility.Collapsed;

    public Visibility ConvertedPlaceholderVisibility => ConvertedPreview is null ? Visibility.Visible : Visibility.Collapsed;

    public Visibility UpdateAvailableVisibility => IsUpdateAvailable ? Visibility.Visible : Visibility.Collapsed;

    public Visibility UpdateDownloadVisibility => IsDownloadingUpdate ? Visibility.Visible : Visibility.Collapsed;

    public string InstallUpdateButtonText => availableUpdate is null
        ? "Download and install"
        : $"Download and install {availableUpdate.TagName}";

    public ObservableCollection<JumpDriveLegViewModel> JumpLegs { get; } = [];

    public ObservableCollection<SpaceEngineersBlockDefinition> FilteredResourceBlocks { get; } = [];

    public ObservableCollection<ResourceBuildRowViewModel> ResourceBuildRows { get; } = [];

    public ObservableCollection<ResourceRecipeViewModel> SavedResourceRecipes { get; } = [];

    public ObservableCollection<ResourceRecipeRowViewModel> ResourceRecipeRows { get; } = [];

    public ObservableCollection<ResourceComponentTotal> ResourceComponentTotals { get; } = [];

    public ObservableCollection<int> JumpDriveCounts { get; } = new(Enumerable.Range(1, 10));

    public ObservableCollection<JumpDriveTypeOption> JumpDriveTypes { get; } =
    [
        new(JumpDriveType.Standard, JumpDriveProfile.Standard.Name),
        new(JumpDriveType.Prototech, JumpDriveProfile.Prototech.Name),
    ];

    public ObservableCollection<FeatureOption> DefaultAppViews { get; } =
    [
        new(MainFeature.ImageConverter, "Image Converter"),
        new(MainFeature.JumpDriveCalculator, "Jump Drive Calculator"),
        new(MainFeature.ResourceCalculator, "Resource Calculator"),
    ];

    public ObservableCollection<ThemeOption> Themes { get; } =
    [
        new(AppTheme.System, "Use system setting"),
        new(AppTheme.Light, "Light"),
        new(AppTheme.Dark, "Dark"),
    ];

    public MainWindowViewModel()
        : this(new AppSettingsStore(), new ResourceRecipeStorageService())
    {
    }

    public MainWindowViewModel(IAppSettingsStore settingsStore)
        : this(settingsStore, new ResourceRecipeStorageService())
    {
    }

    public MainWindowViewModel(ResourceRecipeStorageService resourceRecipeStorage)
        : this(new AppSettingsStore(), resourceRecipeStorage)
    {
    }

    public MainWindowViewModel(IAppSettingsStore settingsStore, ResourceRecipeStorageService resourceRecipeStorage)
    {
        this.settingsStore = settingsStore;
        this.resourceRecipeStorage = resourceRecipeStorage;
        settings = settingsStore.Load();

        isLoadingSettings = true;
        CheckForUpdatesOnStartup = settings.CheckForUpdatesOnStartup;
        SelectedDefaultAppView = FindFeatureOption(settings.DefaultAppView);
        SelectedTheme = FindThemeOption(settings.Theme);
        CurrentFeature = SelectedDefaultAppView.Feature;
        isLoadingSettings = false;
        SelectedDitheringMode = DitheringModes[0];
        SelectedJumpDriveType = JumpDriveTypes[0];
        LoadSavedResourceRecipes();
        RefreshFilteredResourceBlocks();
    }

    public async Task CheckForUpdatesOnStartupAsync()
    {
        if (!CheckForUpdatesOnStartup)
        {
            UpdateStatusMessage = "Startup update checks are turned off.";
            return;
        }

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

    [RelayCommand]
    private void CalculateJumpDrive()
    {
        if (!TryBuildJumpDriveRequest(out JumpDriveCalculationRequest? request))
        {
            return;
        }

        try
        {
            JumpDriveCalculationResult result = jumpDriveCalculator.Calculate(request!);

            JumpDistanceSummary = $"{result.TotalDistanceKm:N1} km";
            JumpRangeSummary = $"{result.EffectiveMaxRangeKm:N1} km";
            JumpCountSummary = $"{result.JumpCount:N0} jump{(result.JumpCount == 1 ? string.Empty : "s")}";
            JumpTravelTimeSummary = FormatDuration(result.TotalTravelTime);

            JumpLegs.Clear();
            foreach (JumpDriveLeg leg in result.Legs)
            {
                JumpLegs.Add(new JumpDriveLegViewModel(
                    leg.Number,
                    $"{leg.DistanceKm:N1} km",
                    leg.RechargeWaitBeforeNextJump == TimeSpan.Zero
                        ? "No recharge needed"
                        : FormatDuration(leg.RechargeWaitBeforeNextJump)));
            }

            SetJumpStatus("Calculated", "Jump route ready.", InfoBarSeverity.Success);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            ClearJumpResult();
            SetJumpStatus("Cannot calculate route", ex.Message, InfoBarSeverity.Warning);
        }
    }

    [RelayCommand(CanExecute = nameof(CanAddResourceBlock))]
    private void AddResourceBlock()
    {
        if (SelectedResourceBlock is null)
        {
            SetResourceStatus("Choose a block", "Search for a block before adding it.", InfoBarSeverity.Warning);
            return;
        }

        if (SelectedResourceBlockQuantity <= 0)
        {
            SetResourceStatus("Check quantity", "Quantity must be greater than 0.", InfoBarSeverity.Warning);
            return;
        }

        ResourceBuildRowViewModel? existingRow = ResourceBuildRows.FirstOrDefault(row => row.Block.Id == SelectedResourceBlock.Id);

        if (existingRow is not null)
        {
            existingRow.Quantity = checked(existingRow.Quantity + SelectedResourceBlockQuantity);
        }
        else
        {
            ResourceBuildRowViewModel row = new(SelectedResourceBlock, SelectedResourceBlockQuantity);
            row.PropertyChanged += ResourceBuildRow_PropertyChanged;
            ResourceBuildRows.Add(row);
        }

        SelectedResourceBlockQuantity = 1;
        RecalculateResourceTotals();
        SetResourceStatus("Added", $"{SelectedResourceBlock.DisplayLabel} added to the build.", InfoBarSeverity.Success);
    }

    [RelayCommand]
    private void RemoveResourceBuildRow(ResourceBuildRowViewModel row)
    {
        row.PropertyChanged -= ResourceBuildRow_PropertyChanged;
        ResourceBuildRows.Remove(row);
        RecalculateResourceTotals();
        SetResourceStatus("Removed", $"{row.Block.DisplayLabel} removed from the build.", InfoBarSeverity.Informational);
    }

    [RelayCommand(CanExecute = nameof(HasResourceBuildRows))]
    private void ClearResourceBuildRows()
    {
        foreach (ResourceBuildRowViewModel row in ResourceBuildRows)
        {
            row.PropertyChanged -= ResourceBuildRow_PropertyChanged;
        }

        ResourceBuildRows.Clear();
        RecalculateResourceTotals();
        SetResourceStatus("Cleared", "Resource build list cleared.", InfoBarSeverity.Informational);
    }

    [RelayCommand(CanExecute = nameof(CanSaveCurrentResourceRecipe))]
    public void SaveCurrentResourceRecipe(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            SetResourceStatus("Name required", "Enter a recipe name before saving.", InfoBarSeverity.Warning);
            return;
        }

        if (ResourceBuildRows.Count == 0)
        {
            SetResourceStatus("Nothing to save", "Add at least one block before saving a recipe.", InfoBarSeverity.Warning);
            return;
        }

        ResourceRecipeViewModel? existingRecipe = FindSavedRecipeByName(name);
        ResourceRecipe recipe = new(
            existingRecipe?.Id ?? Guid.NewGuid().ToString("N"),
            name.Trim(),
            ResourceBuildRows.Select(row => new SpaceEngineersBlockQuantity(row.Block.Id, row.Quantity)).ToList());

        try
        {
            IReadOnlyList<ResourceRecipe> updatedRecipes = resourceRecipeStorage.UpsertRecipe(
                SavedResourceRecipes.Select(recipe => recipe.Recipe),
                recipe);

            ReplaceSavedRecipes(updatedRecipes);
            SelectedSavedResourceRecipe = SavedResourceRecipes.FirstOrDefault(saved => saved.Id == recipe.Id);
            SetResourceStatus("Saved", $"{recipe.Name} saved.", InfoBarSeverity.Success);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            SetResourceStatus("Could not save recipe", ex.Message, InfoBarSeverity.Error);
        }
    }

    [RelayCommand(CanExecute = nameof(CanAddResourceRecipe))]
    private void AddResourceRecipe()
    {
        if (SelectedSavedResourceRecipe is null)
        {
            SetResourceStatus("Choose a recipe", "Select a saved recipe before adding it.", InfoBarSeverity.Warning);
            return;
        }

        if (SelectedResourceRecipeQuantity <= 0)
        {
            SetResourceStatus("Check quantity", "Recipe quantity must be greater than 0.", InfoBarSeverity.Warning);
            return;
        }

        ResourceRecipeRowViewModel? existingRow = ResourceRecipeRows.FirstOrDefault(row => row.Recipe.Id == SelectedSavedResourceRecipe.Id);

        if (existingRow is not null)
        {
            existingRow.Quantity = checked(existingRow.Quantity + SelectedResourceRecipeQuantity);
        }
        else
        {
            ResourceRecipeRowViewModel row = new(SelectedSavedResourceRecipe.Recipe, SelectedResourceRecipeQuantity);
            row.PropertyChanged += ResourceRecipeRow_PropertyChanged;
            ResourceRecipeRows.Add(row);
        }

        SelectedResourceRecipeQuantity = 1;
        RecalculateResourceTotals();
        SetResourceStatus("Added", $"{SelectedSavedResourceRecipe.Name} recipe added to the calculation.", InfoBarSeverity.Success);
    }

    [RelayCommand]
    private void RemoveResourceRecipeRow(ResourceRecipeRowViewModel row)
    {
        row.PropertyChanged -= ResourceRecipeRow_PropertyChanged;
        ResourceRecipeRows.Remove(row);
        RecalculateResourceTotals();
        SetResourceStatus("Removed", $"{row.Name} recipe removed from the calculation.", InfoBarSeverity.Informational);
    }

    [RelayCommand(CanExecute = nameof(HasSelectedSavedResourceRecipe))]
    public void DeleteSelectedResourceRecipe()
    {
        if (SelectedSavedResourceRecipe is null)
        {
            return;
        }

        string deletedRecipeId = SelectedSavedResourceRecipe.Id;
        string deletedRecipeName = SelectedSavedResourceRecipe.Name;
        SavedResourceRecipes.Remove(SelectedSavedResourceRecipe);
        SelectedSavedResourceRecipe = SavedResourceRecipes.FirstOrDefault();

        foreach (ResourceRecipeRowViewModel row in ResourceRecipeRows.Where(row => row.Recipe.Id == deletedRecipeId).ToList())
        {
            row.PropertyChanged -= ResourceRecipeRow_PropertyChanged;
            ResourceRecipeRows.Remove(row);
        }

        try
        {
            resourceRecipeStorage.Save(SavedResourceRecipes.Select(recipe => recipe.Recipe));
            RecalculateResourceTotals();
            NotifyResourceCollectionStateChanged();
            NotifyResourceRecipeCommandsChanged();
            SetResourceStatus("Deleted", $"{deletedRecipeName} deleted.", InfoBarSeverity.Informational);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            SetResourceStatus("Could not delete recipe", ex.Message, InfoBarSeverity.Error);
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelectedSavedResourceRecipe))]
    public void LoadSelectedResourceRecipe()
    {
        if (SelectedSavedResourceRecipe is null)
        {
            return;
        }

        ReplaceResourceBuildRows(SelectedSavedResourceRecipe.Recipe.Blocks);
        RecalculateResourceTotals();
        SetResourceStatus("Loaded", $"{SelectedSavedResourceRecipe.Name} loaded into the block list.", InfoBarSeverity.Success);
    }

    [RelayCommand]
    private void RecalculateResources()
    {
        RecalculateResourceTotals();
        SetResourceStatus("Calculated", "Component totals are up to date.", InfoBarSeverity.Success);
    }

    public void SelectResourceBlock(SpaceEngineersBlockDefinition block)
    {
        SelectedResourceBlock = block;
        BlockSearchText = block.DisplayLabel;
    }

    public void SelectBestResourceBlockMatch()
    {
        SpaceEngineersBlockDefinition? block = FilteredResourceBlocks.FirstOrDefault();

        if (block is not null)
        {
            SelectResourceBlock(block);
        }
    }

    public bool HasSavedResourceRecipeName(string name) => FindSavedRecipeByName(name) is not null;

    public bool HasUnsavedResourceBuildRows() => ResourceBuildRows.Count > 0;

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

    partial void OnCurrentFeatureChanged(MainFeature value)
    {
        OnPropertyChanged(nameof(ImageConverterVisibility));
        OnPropertyChanged(nameof(JumpDriveCalculatorVisibility));
        OnPropertyChanged(nameof(ResourceCalculatorVisibility));
        OnPropertyChanged(nameof(SettingsVisibility));
    }

    partial void OnCheckForUpdatesOnStartupChanged(bool value)
    {
        if (isLoadingSettings)
        {
            return;
        }

        settings = settings with { CheckForUpdatesOnStartup = value };
        SaveSettings();
    }

    partial void OnSelectedDefaultAppViewChanged(FeatureOption value)
    {
        if (isLoadingSettings)
        {
            return;
        }

        settings = settings with { DefaultAppView = value.Feature };
        SaveSettings();
    }

    partial void OnSelectedThemeChanged(ThemeOption value)
    {
        if (isLoadingSettings)
        {
            return;
        }

        settings = settings with { Theme = value.Theme };
        SaveSettings();
    }

    partial void OnJumpStatusTitleChanged(string value)
    {
        OnPropertyChanged(nameof(HasJumpStatusMessage));
    }

    partial void OnJumpStatusMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasJumpStatusMessage));
    }

    partial void OnBlockSearchTextChanged(string value)
    {
        RefreshFilteredResourceBlocks();
        AddResourceBlockCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedResourceBlockChanged(SpaceEngineersBlockDefinition? value)
    {
        AddResourceBlockCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedResourceBlockQuantityChanged(int value)
    {
        AddResourceBlockCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedSavedResourceRecipeChanged(ResourceRecipeViewModel? value)
    {
        NotifyResourceRecipeCommandsChanged();
    }

    partial void OnSelectedResourceRecipeQuantityChanged(int value)
    {
        AddResourceRecipeCommand.NotifyCanExecuteChanged();
    }

    partial void OnResourceStatusTitleChanged(string value)
    {
        OnPropertyChanged(nameof(HasResourceStatusMessage));
    }

    partial void OnResourceStatusMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasResourceStatusMessage));
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

    private bool CanAddResourceBlock() => SelectedResourceBlock is not null && SelectedResourceBlockQuantity > 0;

    private bool HasResourceBuildRows() => ResourceBuildRows.Count > 0;

    private bool CanSaveCurrentResourceRecipe(string name) => ResourceBuildRows.Count > 0 && !string.IsNullOrWhiteSpace(name);

    private bool CanAddResourceRecipe() => SelectedSavedResourceRecipe is not null && SelectedResourceRecipeQuantity > 0;

    private bool HasSelectedSavedResourceRecipe() => SelectedSavedResourceRecipe is not null;

    private void RefreshFilteredResourceBlocks()
    {
        string searchText = BlockSearchText.Trim();
        IEnumerable<SpaceEngineersBlockDefinition> matches = SpaceEngineersBlockCatalog.DefaultBlocks;

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            matches = matches.Where(block => block.SearchText.Contains(searchText, StringComparison.OrdinalIgnoreCase));
        }

        FilteredResourceBlocks.Clear();
        foreach (SpaceEngineersBlockDefinition block in matches.Take(60))
        {
            FilteredResourceBlocks.Add(block);
        }

        SelectedResourceBlock = FilteredResourceBlocks.FirstOrDefault(block =>
            block.DisplayLabel.Equals(searchText, StringComparison.OrdinalIgnoreCase));
    }

    private void ResourceBuildRow_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ResourceBuildRowViewModel.Quantity))
        {
            RecalculateResourceTotals();
        }
    }

    private void ResourceRecipeRow_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ResourceRecipeRowViewModel.Quantity))
        {
            RecalculateResourceTotals();
        }
    }

    private void RecalculateResourceTotals()
    {
        try
        {
            ResourceCalculationResult result = resourceCalculator.Calculate(new ResourceCalculationRequest(
                ResourceBuildRows.Select(row => new SpaceEngineersBlockQuantity(row.Block.Id, row.Quantity)).ToList(),
                ResourceRecipeRows.Select(row => new ResourceRecipeQuantity(row.Recipe.Id, row.Quantity)).ToList(),
                SavedResourceRecipes.Select(recipe => recipe.Recipe).ToList()));

            ResourceComponentTotals.Clear();
            foreach (ResourceComponentTotal total in result.ComponentTotals)
            {
                ResourceComponentTotals.Add(total);
            }

            NotifyResourceCollectionStateChanged();
            ClearResourceBuildRowsCommand.NotifyCanExecuteChanged();
        }
        catch (Exception ex) when (ex is ArgumentOutOfRangeException or OverflowException or KeyNotFoundException)
        {
            ResourceComponentTotals.Clear();
            NotifyResourceCollectionStateChanged();
            SetResourceStatus("Cannot calculate", ex.Message, InfoBarSeverity.Warning);
        }
    }

    private void NotifyResourceCollectionStateChanged()
    {
        OnPropertyChanged(nameof(ResourceBuildEmptyVisibility));
        OnPropertyChanged(nameof(ResourceRecipeRowsEmptyVisibility));
        OnPropertyChanged(nameof(ResourceTotalsEmptyVisibility));
        SaveCurrentResourceRecipeCommand.NotifyCanExecuteChanged();
    }

    private void LoadSavedResourceRecipes()
    {
        ResourceRecipeStorageLoadResult loadResult = resourceRecipeStorage.Load();
        ReplaceSavedRecipes(loadResult.Recipes);

        if (!string.IsNullOrWhiteSpace(loadResult.WarningMessage))
        {
            SetResourceStatus("Saved recipes unavailable", loadResult.WarningMessage, InfoBarSeverity.Warning);
        }
    }

    private void ReplaceSavedRecipes(IEnumerable<ResourceRecipe> recipes)
    {
        SavedResourceRecipes.Clear();
        foreach (ResourceRecipe recipe in recipes)
        {
            SavedResourceRecipes.Add(new ResourceRecipeViewModel(recipe));
        }

        SelectedSavedResourceRecipe ??= SavedResourceRecipes.FirstOrDefault();
        if (SelectedSavedResourceRecipe is not null && SavedResourceRecipes.All(recipe => recipe.Id != SelectedSavedResourceRecipe.Id))
        {
            SelectedSavedResourceRecipe = SavedResourceRecipes.FirstOrDefault();
        }

        foreach (ResourceRecipeRowViewModel row in ResourceRecipeRows)
        {
            ResourceRecipeViewModel? savedRecipe = SavedResourceRecipes.FirstOrDefault(recipe => recipe.Id == row.Recipe.Id);

            if (savedRecipe is not null)
            {
                row.UpdateRecipe(savedRecipe.Recipe);
            }
        }

        NotifyResourceRecipeCommandsChanged();
    }

    private void ReplaceResourceBuildRows(IEnumerable<SpaceEngineersBlockQuantity> blocks)
    {
        foreach (ResourceBuildRowViewModel row in ResourceBuildRows)
        {
            row.PropertyChanged -= ResourceBuildRow_PropertyChanged;
        }

        ResourceBuildRows.Clear();

        foreach (SpaceEngineersBlockQuantity item in blocks)
        {
            SpaceEngineersBlockDefinition? block = SpaceEngineersBlockCatalog.DefaultBlocks.FirstOrDefault(block =>
                block.Id.Equals(item.BlockId, StringComparison.OrdinalIgnoreCase));

            if (block is null)
            {
                continue;
            }

            ResourceBuildRowViewModel row = new(block, item.Quantity);
            row.PropertyChanged += ResourceBuildRow_PropertyChanged;
            ResourceBuildRows.Add(row);
        }

        NotifyResourceCollectionStateChanged();
        ClearResourceBuildRowsCommand.NotifyCanExecuteChanged();
    }

    private ResourceRecipeViewModel? FindSavedRecipeByName(string name)
    {
        return SavedResourceRecipes.FirstOrDefault(recipe => recipe.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private void NotifyResourceRecipeCommandsChanged()
    {
        AddResourceRecipeCommand.NotifyCanExecuteChanged();
        DeleteSelectedResourceRecipeCommand.NotifyCanExecuteChanged();
        LoadSelectedResourceRecipeCommand.NotifyCanExecuteChanged();
    }

    private bool TryBuildJumpDriveRequest(out JumpDriveCalculationRequest? request)
    {
        request = null;

        if (!TryParsePosition(StartGps, StartX, StartY, StartZ, "starting position", out JumpDriveVector start) ||
            !TryParsePosition(DestinationGps, DestinationX, DestinationY, DestinationZ, "destination", out JumpDriveVector destination))
        {
            ClearJumpResult();
            return false;
        }

        if (!double.TryParse(ShipMassKg, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double shipMassKg) || shipMassKg <= 0)
        {
            ClearJumpResult();
            SetJumpStatus("Check ship mass", "Ship mass must be greater than 0 kg.", InfoBarSeverity.Warning);
            return false;
        }

        request = new JumpDriveCalculationRequest(start, destination, SelectedJumpDriveCount, shipMassKg, SelectedJumpDriveType.Type);
        return true;
    }

    private bool TryParsePosition(
        string gps,
        string xText,
        string yText,
        string zText,
        string label,
        out JumpDriveVector vector)
    {
        if (!string.IsNullOrWhiteSpace(gps))
        {
            if (SpaceEngineersGpsParser.TryParse(gps, out vector))
            {
                return true;
            }

            SetJumpStatus("Check coordinates", $"The {label} GPS string is not valid.", InfoBarSeverity.Warning);
            vector = default;
            return false;
        }

        if (double.TryParse(xText, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out double x) &&
            double.TryParse(yText, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out double y) &&
            double.TryParse(zText, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out double z))
        {
            vector = new JumpDriveVector(x, y, z);
            return true;
        }

        SetJumpStatus("Check coordinates", $"Enter a valid {label} GPS string or X/Y/Z coordinates.", InfoBarSeverity.Warning);
        vector = default;
        return false;
    }

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

    private void SetJumpStatus(string title, string message, InfoBarSeverity severity)
    {
        JumpStatusTitle = title;
        JumpStatusMessage = message;
        JumpStatusSeverity = severity;
        OnPropertyChanged(nameof(HasJumpStatusMessage));
    }

    private void SetResourceStatus(string title, string message, InfoBarSeverity severity)
    {
        ResourceStatusTitle = title;
        ResourceStatusMessage = message;
        ResourceStatusSeverity = severity;
        OnPropertyChanged(nameof(HasResourceStatusMessage));
    }

    private void ClearJumpResult()
    {
        JumpDistanceSummary = "-";
        JumpRangeSummary = "-";
        JumpCountSummary = "-";
        JumpTravelTimeSummary = "-";
        JumpLegs.Clear();
    }

    private void NotifyUpdateStateChanged()
    {
        OnPropertyChanged(nameof(UpdateAvailableVisibility));
        OnPropertyChanged(nameof(UpdateDownloadVisibility));
        OnPropertyChanged(nameof(InstallUpdateButtonText));
        CheckForUpdatesCommand.NotifyCanExecuteChanged();
        InstallUpdateCommand.NotifyCanExecuteChanged();
    }

    private FeatureOption FindFeatureOption(MainFeature feature) =>
        DefaultAppViews.FirstOrDefault(option => option.Feature == feature) ?? DefaultAppViews[0];

    private ThemeOption FindThemeOption(AppTheme theme) =>
        Themes.FirstOrDefault(option => option.Theme == theme) ?? Themes[0];

    private void SaveSettings()
    {
        settingsStore.Save(settings);
    }

    private static string FormatVersion(Version version)
    {
        return version.Build >= 0
            ? $"{version.Major}.{version.Minor}.{version.Build}"
            : $"{version.Major}.{version.Minor}";
    }

    private static DateTime GetAppDate()
    {
        string assemblyPath = Assembly.GetExecutingAssembly().Location;

        return string.IsNullOrWhiteSpace(assemblyPath)
            ? DateTime.Today
            : File.GetLastWriteTime(assemblyPath);
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalHours >= 1)
        {
            return $"{(int)duration.TotalHours:N0}h {duration.Minutes:N0}m {duration.Seconds:N0}s";
        }

        if (duration.TotalMinutes >= 1)
        {
            return $"{(int)duration.TotalMinutes:N0}m {duration.Seconds:N0}s";
        }

        return $"{duration.TotalSeconds:N1}s";
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

public enum MainFeature
{
    ImageConverter,
    JumpDriveCalculator,
    ResourceCalculator,
    Settings,
}

public sealed record JumpDriveLegViewModel(int Number, string Distance, string RechargeWait);

public sealed record JumpDriveTypeOption(JumpDriveType Type, string Name);

public sealed record FeatureOption(MainFeature Feature, string Name);

public sealed record ThemeOption(AppTheme Theme, string Name);
public sealed partial class ResourceBuildRowViewModel : ObservableObject
{
    [ObservableProperty]
    public partial int Quantity { get; set; }

    public ResourceBuildRowViewModel(SpaceEngineersBlockDefinition block, int quantity)
    {
        Block = block;
        Quantity = quantity;
    }

    public SpaceEngineersBlockDefinition Block { get; }

    public string DisplayName => Block.DisplayName;

    public string GridSize => Block.GridSize;

    public string BlockId => Block.Id;
}

public sealed class ResourceRecipeViewModel
{
    public ResourceRecipeViewModel(ResourceRecipe recipe)
    {
        Recipe = recipe;
    }

    public ResourceRecipe Recipe { get; }

    public string Id => Recipe.Id;

    public string Name => Recipe.Name;

    public int BlockCount => Recipe.Blocks.Sum(block => block.Quantity);

    public string Summary => $"{Name} ({BlockCount:N0} block{(BlockCount == 1 ? string.Empty : "s")})";
}

public sealed partial class ResourceRecipeRowViewModel : ObservableObject
{
    [ObservableProperty]
    public partial int Quantity { get; set; }

    public ResourceRecipeRowViewModel(ResourceRecipe recipe, int quantity)
    {
        Recipe = recipe;
        Quantity = quantity;
    }

    public ResourceRecipe Recipe { get; private set; }

    public string Name => Recipe.Name;

    public int BlockCount => Recipe.Blocks.Sum(block => block.Quantity);

    public string Summary => $"{BlockCount:N0} block{(BlockCount == 1 ? string.Empty : "s")} per recipe";

    public void UpdateRecipe(ResourceRecipe recipe)
    {
        Recipe = recipe;
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(BlockCount));
        OnPropertyChanged(nameof(Summary));
    }
}
