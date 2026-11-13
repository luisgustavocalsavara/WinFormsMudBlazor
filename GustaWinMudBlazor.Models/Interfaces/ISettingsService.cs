using GustaMudBlazor.Models.Entities;

namespace GustaMudBlazor.Models.Interfaces;

public interface ISettingsService
{
    AppSettings Current { get; }
    event Action? SettingsChanged;
    void Load();
    void Save();
    void UpdateDarkMode(bool isDarkMode);
    void UpdateFullScreen(bool isFullScreen);
}
