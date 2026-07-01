using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using MudBlazor;
using QuantumSummerLab.Application.Teams.Commands;
using QuantumSummerLab.Web.Helpers;

namespace QuantumSummerLab.Web.Components.Layout;

public partial class MainLayout
{
    private MudThemeProvider _mudThemeProvider;
    private bool _drawerOpen = true;
    private bool _copilotEnabled = false;
    private bool _copilotOpen = false;
    private bool _isDarkMode = true;
    private MudTheme? _theme = null;
    private bool? _lastLoggedIn;
    private bool _shouldRefreshCodeTheme;

    private CopilotPane _copilotPane;

    private string TeamName { get; set; }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        DrawerHelper.ShouldPopout += OnShouldPopout;
        NavigationManager.LocationChanged += OnLocationChanged;

        _theme = new()
        {
            PaletteLight = _lightPalette,
            PaletteDark = _darkPalette,
            LayoutProperties = new LayoutProperties
            {
                DefaultBorderRadius = "8px"
            }
        };

        ApplyCodeBlockTheme();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _isDarkMode = await _mudThemeProvider.GetSystemDarkModeAsync();
            ApplyCodeBlockTheme();
        }

        if (_shouldRefreshCodeTheme)
        {
            _shouldRefreshCodeTheme = false;
            ApplyCodeBlockTheme();
        }

        var authToken = await ProtectedLocalStore.GetAsync<AuthenticationToken>("authToken");

        if (_lastLoggedIn == null || _lastLoggedIn != authToken.Success)
        {
            TeamName = authToken.Success ? authToken.Value.TeamName : string.Empty;
            _lastLoggedIn = authToken.Success;
            StateHasChanged();
        }
    }

    private void DrawerToggle()
    {
        _drawerOpen = !_drawerOpen;
    }

    private void DarkModeToggle()
    {
        _isDarkMode = !_isDarkMode;
        ApplyCodeBlockTheme();
    }

    private void ApplyCodeBlockTheme()
    {
        MarkdownThemeService.SetCodeBlockTheme(_isDarkMode ? CodeBlockTheme.GithubDark : CodeBlockTheme.Github);
    }

    private void Login()
    {
        NavigationManager.NavigateTo("/login");
    }

    private void OnShouldPopout(object? sender, ParametrizedEventArgs e)
    {
        _copilotOpen = true;
        _ = _copilotPane.Refresh(e.Parameter1, e.Parameter2);
        StateHasChanged();
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        _copilotEnabled = !e.Location.Contains("reference");
        _copilotOpen = false;
        _shouldRefreshCodeTheme = true;

        StateHasChanged();
    }

    private void CopilotHiddenChanged(bool hidden)
    {
        if (hidden)
        {
            _copilotOpen = false;
            StateHasChanged();
        }
    }

    protected void CloseCopilot()
    {
        _copilotOpen = false;
        StateHasChanged();
    }

    private readonly PaletteLight _lightPalette = new()
    {
        Primary = "#4f46e5",
        Secondary = "#0891b2",
        Tertiary = "#7c3aed",
        Info = "#0284c7",
        Success = "#059669",
        Warning = "#d97706",
        Error = "#dc2626",
        Black = "#0f172a",
        Background = "#f7f8fa",
        BackgroundGray = "#eef1f6",
        Surface = "#ffffff",
        AppbarText = "#1e293b",
        AppbarBackground = "rgba(255,255,255,0.8)",
        DrawerBackground = "#ffffff",
        DrawerText = "#475569",
        DrawerIcon = "#475569",
        TextPrimary = "#1e293b",
        TextSecondary = "#64748b",
        ActionDefault = "#64748b",
        LinesDefault = "#e2e8f0",
        LinesInputs = "#cbd5e1",
        TableLines = "#e2e8f0",
        Divider = "#e2e8f0",
        GrayLight = "#f1f5f9",
        GrayLighter = "#f8fafc",
    };

    private readonly PaletteDark _darkPalette = new()
    {
        Primary = "#818cf8",
        Secondary = "#22d3ee",
        Tertiary = "#a78bfa",
        Info = "#38bdf8",
        Success = "#34d399",
        Warning = "#fbbf24",
        Error = "#fb7185",
        Black = "#0b0d12",
        Surface = "#161922",
        Background = "#0f1117",
        BackgroundGray = "#0b0d12",
        AppbarText = "#cbd5e1",
        AppbarBackground = "rgba(15,17,23,0.8)",
        DrawerBackground = "#12141b",
        ActionDefault = "#94a3b8",
        ActionDisabled = "#9999994d",
        ActionDisabledBackground = "#605f6d4d",
        TextPrimary = "#e2e8f0",
        TextSecondary = "#94a3b8",
        TextDisabled = "#ffffff33",
        DrawerIcon = "#94a3b8",
        DrawerText = "#94a3b8",
        GrayLight = "#262b36",
        GrayLighter = "#1b1f29",
        LinesDefault = "#262b36",
        LinesInputs = "#3a4150",
        TableLines = "#262b36",
        Divider = "#262b36",
        OverlayLight = "#161922cc",
    };

    public string DarkLightModeButtonIcon => _isDarkMode switch
    {
        true => Icons.Material.Rounded.AutoMode,
        false => Icons.Material.Outlined.DarkMode,
    };
}
