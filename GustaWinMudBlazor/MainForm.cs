using System.Diagnostics;
using Microsoft.AspNetCore.Components.WebView.WindowsForms;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using WinFormsMudBlazor.Models.Interfaces;
using WinFormsMudBlazor.Services;
using WinFormsMudBlazor.Services.Data;
using WinFormsMudBlazor.Views;

namespace WinFormsMudBlazor;

public partial class MainForm : Form
{
    private FormBorderStyle _previousBorderStyle;
    private Rectangle _previousBounds;
    private bool _wasMaximized;
    private readonly JsonSettingsService _settingsService;
    private readonly WinFormsWindowService _windowService;
    private bool _isWebViewInitialized;

    private Panel? _loadingPanel;
    private Label? _loadingLabel;
    private ProgressBar? _loadingProgressBar;

    public bool IsFullScreen => FormBorderStyle == FormBorderStyle.None;

    public MainForm()
    {
        InitializeComponent();

        // Guarda a posição centralizada padrão caso o app já inicie em FullScreen
        var workingArea = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1080);
        _previousBounds = new Rectangle(
            workingArea.Left + (workingArea.Width - Width) / 2,
            workingArea.Top + (workingArea.Height - Height) / 2,
            Width,
            Height);
        _previousBorderStyle = FormBorderStyle;
        _wasMaximized = WindowState == FormWindowState.Maximized;

        // 1. Lê o appsettings.user.json antes de tudo
        _settingsService = new JsonSettingsService();
        _windowService = new WinFormsWindowService(this, _settingsService);

        // 2. Restaura o modo tela cheia se estava salvo
        if (_settingsService.Current.IsFullScreen)
        {
            ToggleFullScreenState();
        }

        // 3. Mostra o painel de loading por cima (BringToFront) com a cor exata do tema (Dark ou Light)
        InitializeLoadingOverlay(_settingsService.Current.IsDarkMode);

        var services = new ServiceCollection();
        ConfigureServices(services);
        var serviceProvider = services.BuildServiceProvider();

        // 4. Carrega o favicon.ico padrão como ícone de exemplo para os blocos iniciais e inicializa o banco SQLite
        var defaultIconDataUrl = LoadDefaultIconDataUrl();
        AppDbContext.InitializeDatabase(serviceProvider, defaultIconDataUrl);

        BlazorWebView.Services = serviceProvider;

        InitializeBlazorWebView();
        RegisterHotKey();
    }

    private static string LoadDefaultIconDataUrl()
    {
        var icoPath = Path.Combine(AppContext.BaseDirectory, "wwwroot", "favicon.ico");
        try
        {
            if (File.Exists(icoPath))
            {
                var bytes = File.ReadAllBytes(icoPath);
                return $"data:image/x-icon;base64,{Convert.ToBase64String(bytes)}";
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Trace.TraceError("Could not load the default icon from '{0}': {1}", icoPath, ex);
        }
        return string.Empty;
    }

    private void InitializeLoadingOverlay(bool isDarkMode)
    {
        var bgColor = isDarkMode
            ? Color.FromArgb(26, 26, 39)      // #1a1a27 (Dark)
            : Color.FromArgb(245, 245, 247);  // #f5f5f7 (Light)

        var textColor = isDarkMode
            ? Color.FromArgb(178, 176, 191)   // #b2b0bf (Dark)
            : Color.FromArgb(51, 51, 51);     // #333333 (Light)

        BackColor = bgColor;
        BlazorWebView.BackColor = bgColor;
        BlazorWebView.WebView.DefaultBackgroundColor = bgColor;

        _loadingPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = bgColor
        };

        var centerLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Color.Transparent
        };
        centerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        centerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        centerLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        centerLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        centerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

        _loadingLabel = new Label
        {
            Text = "Inicializando motor gráfico e banco de dados...",
            ForeColor = textColor,
            Font = new Font("Segoe UI", 11F, FontStyle.Regular),
            AutoSize = true,
            Anchor = AnchorStyles.None,
            Margin = new Padding(0, 0, 0, 12)
        };

        _loadingProgressBar = new ProgressBar
        {
            Style = ProgressBarStyle.Marquee,
            MarqueeAnimationSpeed = 25,
            Size = new Size(320, 18),
            Anchor = AnchorStyles.None
        };

        centerLayout.Controls.Add(_loadingLabel, 0, 1);
        centerLayout.Controls.Add(_loadingProgressBar, 0, 2);
        _loadingPanel.Controls.Add(centerLayout);

        Controls.Add(_loadingPanel);
        _loadingPanel.BringToFront();
    }

    private void HideLoadingOverlay()
    {
        if (_loadingPanel is null) return;

        _loadingPanel.Visible = false;
        Controls.Remove(_loadingPanel);
        _loadingPanel.Dispose();
        _loadingPanel = null;
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddWindowsFormsBlazorWebView();
        services.AddMudServices();

        // Banco de Dados SQLite com EF Core
        var dbPath = Path.Combine(AppContext.BaseDirectory, "app_data.db");
        services.AddDbContextFactory<AppDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        // Registro de Services (Arquitetura Component-Service-Model Direta, sem camada ViewModel)
        services.AddSingleton<ISettingsService>(_settingsService);
        services.AddSingleton<IWindowService>(_windowService);
        services.AddSingleton<IAuthService, AuthService>();
        services.AddScoped<IBlockService, BlockService>();
        services.AddScoped<IRecipeService, RecipeService>();
        services.AddScoped<IPlcExportService>(sp => new WinFormsPlcExportService(this, sp.GetRequiredService<IRecipeService>()));
        services.AddScoped<IWeatherService, WeatherService>();
    }

    private void RegisterHotKey()
    {
        KeyPreview = true;
        KeyDown += (sender, e) =>
        {
            if (e.KeyCode == Keys.F11)
            {
                _windowService.ToggleFullScreen();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape && IsFullScreen)
            {
                _windowService.ExitFullScreen();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.F12 && _isWebViewInitialized)
            {
                BlazorWebView.WebView.CoreWebView2.OpenDevToolsWindow();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        };
    }

    private void InitializeBlazorWebView()
    {
        BlazorWebView.HostPage = "wwwroot/index.html";
        BlazorWebView.RootComponents.Add<Routes>("#app");

        BlazorWebView.WebView.CoreWebView2InitializationCompleted += (s, e) =>
        {
            if (e.IsSuccess)
            {

                BlazorWebView.WebView.CoreWebView2.Settings.AreDevToolsEnabled = true;
                _isWebViewInitialized = true;

                _loadingLabel?.Text = "Carregando interface Blazor...";
                BlazorWebView.WebView.CoreWebView2.AddHostObjectToScript("winFormHost", new WinFormInterop(_windowService));
#if DEBUG
                BeginInvoke((Action)OpenDevToolsWindow);
#endif
            }
            else
            {
                MessageBox.Show(
                    $"Falha ao inicializar o WebView2: {e.InitializationException?.Message}",
                    "Erro de Inicialização",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        };

        BlazorWebView.WebView.NavigationCompleted += (s, e) =>
        {
            HideLoadingOverlay();
        };
    }

    private void OpenDevToolsWindow()
    {
        try
        {
            BlazorWebView.WebView.CoreWebView2.OpenDevToolsWindow();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Não foi possível abrir as ferramentas do WebView2: {ex.Message}",
                "Erro ao abrir DevTools",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    public void ToggleFullScreenState()
    {
        if (FormBorderStyle != FormBorderStyle.None)
        {
            _previousBorderStyle = FormBorderStyle;
            if (WindowState == FormWindowState.Normal)
            {
                _previousBounds = Bounds;
            }
            _wasMaximized = WindowState == FormWindowState.Maximized;

            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
        }
        else
        {
            FormBorderStyle = _previousBorderStyle;
            WindowState = _wasMaximized ? FormWindowState.Maximized : FormWindowState.Normal;
            if (WindowState == FormWindowState.Normal)
            {
                Bounds = _previousBounds;
            }
        }
    }

    private void MainForm_Load(object sender, EventArgs e)
    {

    }
}
