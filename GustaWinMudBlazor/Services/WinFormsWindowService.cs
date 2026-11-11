using WinFormsMudBlazor.Models.Interfaces;

namespace WinFormsMudBlazor.Services;

public class WinFormsWindowService(MainForm mainForm, ISettingsService settingsService) : IWindowService
{
    private readonly MainForm _mainForm = mainForm;
    private readonly ISettingsService _settingsService = settingsService;

    public bool IsFullScreen => _mainForm.IsFullScreen;

    public event Action<bool>? FullScreenChanged;

    public void ToggleFullScreen()
    {
        if (_mainForm.InvokeRequired)
        {
            _mainForm.Invoke(ToggleFullScreenInternal);
        }
        else
        {
            ToggleFullScreenInternal();
        }
    }

    public void ExitFullScreen()
    {
        if (_mainForm.IsFullScreen)
        {
            ToggleFullScreen();
        }
    }

    public void CloseWindow()
    {
        if (_mainForm.InvokeRequired)
        {
            _mainForm.Invoke(_mainForm.Close);
        }
        else
        {
            _mainForm.Close();
        }
    }

    private void ToggleFullScreenInternal()
    {
        _mainForm.ToggleFullScreenState();
        _settingsService.UpdateFullScreen(_mainForm.IsFullScreen);
        FullScreenChanged?.Invoke(_mainForm.IsFullScreen);
    }

    public void NotifyFullScreenChanged()
    {
        _settingsService.UpdateFullScreen(_mainForm.IsFullScreen);
        FullScreenChanged?.Invoke(_mainForm.IsFullScreen);
    }
}
