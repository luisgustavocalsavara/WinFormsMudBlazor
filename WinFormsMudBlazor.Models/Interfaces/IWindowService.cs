namespace WinFormsMudBlazor.Models.Interfaces;

public interface IWindowService
{
    bool IsFullScreen { get; }
    event Action<bool>? FullScreenChanged;
    void ToggleFullScreen();
    void ExitFullScreen();
    void CloseWindow();
}
