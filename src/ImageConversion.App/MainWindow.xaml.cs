using ImageConversion.App.ViewModels;
using ImageConversion.App.Services;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
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
    private bool isFormattingShipMass;

    public MainWindowViewModel ViewModel { get; } = new();

    public MainWindow()
    {
        InitializeComponent();
        ExtendsContentIntoTitleBar = false;
        Root.DataContext = ViewModel;
        FeatureNavigation.SelectedItem = ImageConverterNavigationItem;
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

    private void FeatureNavigation_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is not NavigationViewItem item || item.Tag is not string tag)
        {
            return;
        }

        ViewModel.CurrentFeature = tag switch
        {
            "JumpDriveCalculator" => MainFeature.JumpDriveCalculator,
            "Settings" => MainFeature.Settings,
            _ => MainFeature.ImageConverter,
        };
    }

    private void ShipMassTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (isFormattingShipMass || sender is not TextBox textBox)
        {
            return;
        }

        FormatShipMassTextBox(textBox, includeDecimals: false);
    }

    private void ShipMassTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            FormatShipMassTextBox(textBox, includeDecimals: true);
        }
    }

    private void FormatShipMassTextBox(TextBox textBox, bool includeDecimals)
    {
        int significantCharactersBeforeCaret = CountSignificantCharacters(textBox.Text, textBox.SelectionStart);
        string formattedText = includeDecimals
            ? FormatFinalShipMass(textBox.Text)
            : FormatPartialShipMass(textBox.Text);

        if (formattedText == textBox.Text)
        {
            return;
        }

        isFormattingShipMass = true;
        textBox.Text = formattedText;
        textBox.SelectionStart = Math.Min(
            FindCaretPositionAfterSignificantCharacters(formattedText, significantCharactersBeforeCaret),
            formattedText.Length);
        isFormattingShipMass = false;

        textBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
    }

    private static string FormatPartialShipMass(string text)
    {
        string sanitized = SanitizeShipMassText(text, keepDecimal: true);

        if (string.IsNullOrEmpty(sanitized))
        {
            return string.Empty;
        }

        bool hasDecimalPoint = sanitized.Contains('.');
        string[] parts = sanitized.Split('.', 2);
        string integerPart = FormatIntegerPart(parts[0]);

        if (!hasDecimalPoint)
        {
            return integerPart;
        }

        string decimalPart = parts.Length > 1 ? parts[1][..Math.Min(parts[1].Length, 2)] : string.Empty;
        return $"{integerPart}.{decimalPart}";
    }

    private static string FormatFinalShipMass(string text)
    {
        string sanitized = SanitizeShipMassText(text, keepDecimal: true);

        if (string.IsNullOrWhiteSpace(sanitized) ||
            !decimal.TryParse(sanitized, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value))
        {
            return text;
        }

        return value.ToString("N2", CultureInfo.InvariantCulture);
    }

    private static string SanitizeShipMassText(string text, bool keepDecimal)
    {
        StringBuilder builder = new();
        bool hasDecimalPoint = false;

        foreach (char character in text)
        {
            if (char.IsDigit(character))
            {
                builder.Append(character);
            }
            else if (keepDecimal && character == '.' && !hasDecimalPoint)
            {
                builder.Append(character);
                hasDecimalPoint = true;
            }
        }

        return builder.ToString();
    }

    private static string FormatIntegerPart(string integerText)
    {
        string trimmed = integerText.TrimStart('0');

        if (trimmed.Length == 0)
        {
            trimmed = "0";
        }

        StringBuilder builder = new(trimmed.Length + (trimmed.Length / 3));
        int firstGroupLength = trimmed.Length % 3;

        if (firstGroupLength == 0)
        {
            firstGroupLength = 3;
        }

        builder.Append(trimmed.AsSpan(0, firstGroupLength));

        for (int index = firstGroupLength; index < trimmed.Length; index += 3)
        {
            builder.Append(',');
            builder.Append(trimmed.AsSpan(index, 3));
        }

        return builder.ToString();
    }

    private static int CountSignificantCharacters(string text, int caretPosition)
    {
        int count = 0;

        for (int index = 0; index < Math.Min(caretPosition, text.Length); index++)
        {
            if (char.IsDigit(text[index]) || text[index] == '.')
            {
                count++;
            }
        }

        return count;
    }

    private static int FindCaretPositionAfterSignificantCharacters(string text, int significantCharacterCount)
    {
        if (significantCharacterCount <= 0)
        {
            return 0;
        }

        int seen = 0;

        for (int index = 0; index < text.Length; index++)
        {
            if (char.IsDigit(text[index]) || text[index] == '.')
            {
                seen++;
            }

            if (seen == significantCharacterCount)
            {
                return index + 1;
            }
        }

        return text.Length;
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
                        Text = "Use the side menu to switch between the Image Converter and Jump Drive Calculator.",
                        TextWrapping = TextWrapping.Wrap,
                    },
                    new TextBlock
                    {
                        Text = "Image Converter: open or drop an image, choose the LCD/Text Panel type, resize mode, dithering mode, and transparency options.",
                        TextWrapping = TextWrapping.Wrap,
                    },
                    new TextBlock
                    {
                        Text = "Convert the image, then copy or export the generated text.",
                        TextWrapping = TextWrapping.Wrap,
                    },
                    new TextBlock
                    {
                        Text = "Jump Drive Calculator: paste GPS coordinates or enter X/Y/Z values, then add jump drive count and ship mass.",
                        TextWrapping = TextWrapping.Wrap,
                    },
                    new TextBlock
                    {
                        Text = "In Space Engineers, set the LCD content to Text and Images, paste the string, select Monospace, and start with font size 0.1.",
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
