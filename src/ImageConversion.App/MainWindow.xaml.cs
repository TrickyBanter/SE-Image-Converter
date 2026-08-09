using ImageConversion.App.ViewModels;
using ImageConversion.App.Services;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using System.Runtime.InteropServices;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics;
using Windows.Storage.Pickers;
using Windows.Storage;
using WinRT.Interop;

namespace ImageConversion.App;

public sealed partial class MainWindow : Window
{
    private const int IconSmall = 0;
    private const int IconBig = 1;
    private const int ImageIcon = 1;
    private const int LoadFromFile = 0x00000010;
    private const int WmSetIcon = 0x0080;

    private IntPtr bigIconHandle;
    private IntPtr smallIconHandle;

    public MainWindowViewModel ViewModel { get; } = new();

    public MainWindow()
    {
        InitializeComponent();
        ExtendsContentIntoTitleBar = false;
        Root.DataContext = ViewModel;
        ConfigureLaunchWindow();
        Root.Loaded += Root_Loaded;
        Closed += MainWindow_Closed;
    }

    private async void Root_Loaded(object sender, RoutedEventArgs e)
    {
        Root.Loaded -= Root_Loaded;
        await ViewModel.CheckForUpdatesOnStartupAsync();
    }

    private void ConfigureLaunchWindow()
    {
        IntPtr windowHandle = WindowNative.GetWindowHandle(this);
        WindowId windowId = Win32Interop.GetWindowIdFromWindow(windowHandle);
        AppWindow appWindow = AppWindow.GetFromWindowId(windowId);

        string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
        appWindow.SetIcon(iconPath);
        SetNativeWindowIcons(windowHandle, iconPath);
        appWindow.Resize(new SizeInt32(1250, 1040));
    }

    private void SetNativeWindowIcons(IntPtr windowHandle, string iconPath)
    {
        smallIconHandle = LoadImage(IntPtr.Zero, iconPath, ImageIcon, 16, 16, LoadFromFile);
        bigIconHandle = LoadImage(IntPtr.Zero, iconPath, ImageIcon, 32, 32, LoadFromFile);

        if (smallIconHandle != IntPtr.Zero)
        {
            SendMessage(windowHandle, WmSetIcon, IconSmall, smallIconHandle);
        }

        if (bigIconHandle != IntPtr.Zero)
        {
            SendMessage(windowHandle, WmSetIcon, IconBig, bigIconHandle);
        }
    }

    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        if (smallIconHandle != IntPtr.Zero)
        {
            DestroyIcon(smallIconHandle);
            smallIconHandle = IntPtr.Zero;
        }

        if (bigIconHandle != IntPtr.Zero)
        {
            DestroyIcon(bigIconHandle);
            bigIconHandle = IntPtr.Zero;
        }
    }

    private async void PickImageButton_Click(object sender, RoutedEventArgs e)
    {
        await PickImageAsync();
    }

    private async void SourceDropZone_Tapped(object sender, TappedRoutedEventArgs e)
    {
        await PickImageAsync();
    }

    private void SourceDropZone_DragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Copy;
        e.DragUIOverride.Caption = "Open image";
        e.DragUIOverride.IsContentVisible = true;
    }

    private async void SourceDropZone_Drop(object sender, DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            return;
        }

        IReadOnlyList<IStorageItem> items = await e.DataView.GetStorageItemsAsync();
        StorageFile? file = items.OfType<StorageFile>().FirstOrDefault(IsSupportedImageFile);

        if (file is not null)
        {
            await ViewModel.LoadImageAsync(file.Path);
        }
    }

    private async Task PickImageAsync()
    {
        FileOpenPicker picker = new()
        {
            SuggestedStartLocation = PickerLocationId.PicturesLibrary,
        };

        picker.FileTypeFilter.Add(".png");
        picker.FileTypeFilter.Add(".jpg");
        picker.FileTypeFilter.Add(".jpeg");
        picker.FileTypeFilter.Add(".bmp");
        picker.FileTypeFilter.Add(".webp");
        picker.FileTypeFilter.Add(".gif");

        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));
        Windows.Storage.StorageFile? file = await picker.PickSingleFileAsync();

        if (file is not null)
        {
            await ViewModel.LoadImageAsync(file.Path);
        }
    }

    private static bool IsSupportedImageFile(StorageFile file)
    {
        string extension = Path.GetExtension(file.Path);
        return extension.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".bmp", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".webp", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".gif", StringComparison.OrdinalIgnoreCase);
    }

    private async void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        FileSavePicker picker = new()
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = "space-engineers-lcd-image",
        };

        picker.FileTypeChoices.Add("Text file", [".txt"]);

        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));
        Windows.Storage.StorageFile? file = await picker.PickSaveFileAsync();

        if (file is not null)
        {
            await ViewModel.ExportTextAsync(file.Path);
        }
    }

    private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Exit();
    }

    private async void AboutMenuItem_Click(object sender, RoutedEventArgs e)
    {
        ContentDialog dialog = new()
        {
            XamlRoot = Root.XamlRoot,
            Title = "About SE Image Converter",
            CloseButtonText = "Close",
            Content = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new TextBlock
                    {
                        Text = "SE Image Converter",
                        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    },
                    new TextBlock
                    {
                        Text = $"Version {FormatVersion(GitHubReleaseUpdater.CurrentVersion)}",
                    },
                    new TextBlock
                    {
                        Text = "Converts images into paste-ready Space Engineers LCD/Text Panel Monospace text.",
                        TextWrapping = TextWrapping.Wrap,
                    },
                },
            },
        };

        await dialog.ShowAsync();
    }

    private async void GuideMenuItem_Click(object sender, RoutedEventArgs e)
    {
        ContentDialog dialog = new()
        {
            XamlRoot = Root.XamlRoot,
            Title = "Guide",
            CloseButtonText = "Close",
            Content = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new TextBlock
                    {
                        Text = "1. Open or drop an image.",
                        TextWrapping = TextWrapping.Wrap,
                    },
                    new TextBlock
                    {
                        Text = "2. Choose the LCD/Text Panel type, resize mode, dithering mode, and transparency options.",
                        TextWrapping = TextWrapping.Wrap,
                    },
                    new TextBlock
                    {
                        Text = "3. Convert the image, then copy or export the generated text.",
                        TextWrapping = TextWrapping.Wrap,
                    },
                    new TextBlock
                    {
                        Text = "4. In Space Engineers, set the LCD content to Text and Images, paste the string, select Monospace, and start with font size 0.1.",
                        TextWrapping = TextWrapping.Wrap,
                    },
                },
            },
        };

        await dialog.ShowAsync();
    }

    private async void CheckForUpdatesMenuItem_Click(object sender, RoutedEventArgs e)
    {
        StackPanel content = new()
        {
            Spacing = 12,
            DataContext = ViewModel,
            MinWidth = 360,
        };

        content.Children.Add(new TextBlock
        {
            Text = ViewModel.CurrentVersionSummary,
            Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
        });

        TextBlock statusText = new()
        {
            TextWrapping = TextWrapping.Wrap,
        };
        statusText.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath(nameof(ViewModel.UpdateStatusMessage)) });
        content.Children.Add(statusText);

        TextBlock availableText = new()
        {
            TextWrapping = TextWrapping.Wrap,
            Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["AccentTextFillColorPrimaryBrush"],
        };
        availableText.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath(nameof(ViewModel.AvailableUpdateSummary)) });
        availableText.SetBinding(UIElement.VisibilityProperty, new Binding { Path = new PropertyPath(nameof(ViewModel.UpdateAvailableVisibility)) });
        content.Children.Add(availableText);

        ProgressBar progressBar = new()
        {
            Minimum = 0,
            Maximum = 100,
        };
        progressBar.SetBinding(ProgressBar.ValueProperty, new Binding { Path = new PropertyPath(nameof(ViewModel.UpdateDownloadProgress)) });
        progressBar.SetBinding(UIElement.VisibilityProperty, new Binding { Path = new PropertyPath(nameof(ViewModel.UpdateDownloadVisibility)) });
        content.Children.Add(progressBar);

        Button installButton = new()
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Padding = new Thickness(14, 8, 14, 8),
            Command = ViewModel.InstallUpdateCommand,
        };
        installButton.SetBinding(ContentControl.ContentProperty, new Binding { Path = new PropertyPath(nameof(ViewModel.InstallUpdateButtonText)) });
        installButton.SetBinding(UIElement.VisibilityProperty, new Binding { Path = new PropertyPath(nameof(ViewModel.UpdateAvailableVisibility)) });
        content.Children.Add(installButton);

        ContentDialog dialog = new()
        {
            XamlRoot = Root.XamlRoot,
            Title = "Update Status",
            CloseButtonText = "Close",
            Content = content,
        };

        _ = ViewModel.CheckForUpdatesManuallyAsync();
        await dialog.ShowAsync();
    }

    private static string FormatVersion(Version version)
    {
        return version.Build >= 0
            ? $"{version.Major}.{version.Minor}.{version.Build}"
            : $"{version.Major}.{version.Minor}";
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr LoadImage(
        IntPtr instance,
        string name,
        int type,
        int desiredWidth,
        int desiredHeight,
        int load);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SendMessage(IntPtr windowHandle, int message, int wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr iconHandle);
}
