using System.Text.Json;
using WinFormsMudBlazor.Models.Entities;
using WinFormsMudBlazor.Models.Interfaces;

namespace WinFormsMudBlazor.Services;

public class JsonSettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _filePath;

    public AppSettings Current { get; private set; } = new();

    public event Action? SettingsChanged;

    public JsonSettingsService()
    {
        _filePath = Path.Combine(AppContext.BaseDirectory, "appsettings.user.json");
        Load();
    }

    public void Load()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                if (loaded is not null)
                {
                    Current = loaded;
                }
            }
        }
        catch
        {
            Current = new AppSettings();
        }
    }

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(Current, JsonOptions);
            File.WriteAllText(_filePath, json);
        }
        catch
        {
            // Ignora falhas de permissão de escrita em disco se houver
        }
    }

    public void UpdateDarkMode(bool isDarkMode)
    {
        if (Current.IsDarkMode == isDarkMode) return;
        Current.IsDarkMode = isDarkMode;
        Save();
        SettingsChanged?.Invoke();
    }

    public void UpdateFullScreen(bool isFullScreen)
    {
        if (Current.IsFullScreen == isFullScreen) return;
        Current.IsFullScreen = isFullScreen;
        Save();
        SettingsChanged?.Invoke();
    }
}
