using MudBlazor;

namespace GustaMudBlazor.Views.Components.Layout;

public static class AppTheme
{
    public static readonly MudTheme Default = new()
    {
        PaletteLight = new PaletteLight
        {
            Black = "#110e2d",
            Background = "#f5f5f7",
            BackgroundGray = "#eaeaf0",
            Surface = "#ffffff",
            AppbarText = "#424242",
            AppbarBackground = "rgba(255,255,255,0.8)",
            DrawerBackground = "#ffffff",
            DrawerText = "#333333",
            DrawerIcon = "#666666",
            TextPrimary = "#222222",
            TextSecondary = "#666666",
            ActionDefault = "#74718e",
            LinesDefault = "#e0e0e0",
            TableLines = "#e0e0e0",
            Divider = "#e0e0e0",
            GrayLight = "#f0f0f0",
            GrayLighter = "#f9f9f9",
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#7e6fff",
            PrimaryDarken = "#5748db",
            HoverOpacity = 0.16,
            Surface = "#1e1e2d",
            Background = "#1a1a27",
            BackgroundGray = "#151521",
            AppbarText = "#92929f",
            AppbarBackground = "rgba(26,26,39,0.8)",
            DrawerBackground = "#1a1a27",
            ActionDefault = "#74718e",
            ActionDisabled = "#9999994d",
            ActionDisabledBackground = "#605f6d4d",
            TextPrimary = "#b2b0bf",
            TextSecondary = "#92929f",
            TextDisabled = "#ffffff33",
            DrawerIcon = "#92929f",
            DrawerText = "#92929f",
            GrayLight = "#2a2833",
            GrayLighter = "#1e1e2d",
            Info = "#4a86ff",
            Success = "#3dcb6c",
            Warning = "#ffb545",
            Error = "#ff3f5f",
            LinesDefault = "#33323e",
            TableLines = "#33323e",
            TableHover = "#39384a",
            Divider = "#292838",
            OverlayLight = "#1e1e2d80",
        },
        LayoutProperties = new LayoutProperties()
    };
}
