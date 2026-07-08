using QuantumSummerLab.Application.Teams.Commands;
using QuantumSummerLab.Web.Components;

namespace QuantumSummerLab.Web.Components.Pages;

public partial class Copilot
{
    private bool? _lastLoggedIn;
    private bool IsLoggedIn { get; set; }
    private string TeamName { get; set; } = string.Empty;

    private CopilotPane? _copilotPane;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        var authToken = await ProtectedLocalStore.GetAsync<AuthenticationToken>("authToken");

        if (_lastLoggedIn == null || _lastLoggedIn != authToken.Success)
        {
            IsLoggedIn = authToken.Success;
            TeamName = authToken.Success ? authToken.Value!.TeamName : string.Empty;
            _lastLoggedIn = authToken.Success;

            StateHasChanged();
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        await Task.Delay(500);
        if (_copilotPane != null)
        {
            await _copilotPane.Refresh(string.Empty, string.Empty);
        }
    }
}
