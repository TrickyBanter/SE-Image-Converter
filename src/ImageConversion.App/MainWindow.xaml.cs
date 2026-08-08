using ImageConversion.App.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
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
        Closed += MainWindow_Closed;
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
