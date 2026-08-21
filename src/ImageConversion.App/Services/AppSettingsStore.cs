using System.Text.Json;
using ImageConversion.App.ViewModels;

namespace ImageConversion.App.Services;

public interface IAppSettingsStore
{
    AppSettings Load();

    void Save(AppSettings settings);
}

public sealed class AppSettingsStore : IAppSettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };

    private readonly string settingsPath;

    public AppSettingsStore()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SE Image Converter",
            "settings.json"))
    {
    }

    public AppSettingsStore(string settingsPath)
    {
        this.settingsPath = settingsPath;
    }

    public AppSettings Load()
    {
        if (!File.Exists(settingsPath))
        {
            return new AppSettings();
        }

        try
        {
            string json = File.ReadAllText(settingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json, SerializerOptions) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        string? directory = Path.GetDirectoryName(settingsPath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string json = JsonSerializer.Serialize(settings, SerializerOptions);
        File.WriteAllText(settingsPath, json);
    }
}

public sealed record AppSettings
{
    public bool CheckForUpdatesOnStartup { get; init; } = true;

    public MainFeature DefaultAppView { get; init; } = MainFeature.ImageConverter;

    public AppTheme Theme { get; init; } = AppTheme.System;
}

public enum AppTheme
{
    System,
    Light,
    Dark,
}
