using System.Runtime.InteropServices;
using WinFormsMudBlazor.Models.Interfaces;

namespace WinFormsMudBlazor;

[ComVisible(true)]
public class WinFormInterop(IWindowService windowService)
{
    private readonly IWindowService _windowService = windowService;

    public void ToggleFullScreenFromJs() => _windowService.ToggleFullScreen();

    public void ExitFullScreenFromJs() => _windowService.ExitFullScreen();

    public bool IsFullScreen() => _windowService.IsFullScreen;
}
