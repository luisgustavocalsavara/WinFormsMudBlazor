using GustaMudBlazor.Models.Interfaces;
using System.Runtime.InteropServices;

namespace GustaMudBlazor;

[ComVisible(true)]
public class WinFormInterop(IWindowService windowService, Action hideLoadingOverlay)
{
    private readonly IWindowService _windowService = windowService;
    private readonly Action _hideLoadingOverlay = hideLoadingOverlay;

    public void ToggleFullScreenFromJs() => _windowService.ToggleFullScreen();

    public void ExitFullScreenFromJs() => _windowService.ExitFullScreen();

    public bool IsFullScreen() => _windowService.IsFullScreen;

    public void HideLoadingOverlayFromJs() => _hideLoadingOverlay();
}
