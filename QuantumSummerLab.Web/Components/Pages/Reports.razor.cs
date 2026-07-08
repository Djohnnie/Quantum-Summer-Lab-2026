using QuantumSummerLab.Application.Scores.Commands;
using QuantumSummerLab.Application.Teams.Commands;
using QuantumSummerLab.Application.Teams.Queries;

namespace QuantumSummerLab.Web.Components.Pages;

public partial class Reports
{
    private bool IsLoading { get; set; } = true;
    private bool IsLoggedIn { get; set; }
    private bool IsAdmin { get; set; }
    private bool IsErrorMessage { get; set; }
    private string Message { get; set; } = string.Empty;
    private AuthenticationToken? AuthToken { get; set; }
    private List<ManagedTeamDto> ManagedTeams { get; set; } = new List<ManagedTeamDto>();

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        var authToken = await ProtectedLocalStore.GetAsync<AuthenticationToken>("authToken");
        IsLoggedIn = authToken.Success;
        AuthToken = authToken.Success ? authToken.Value : null;
        IsAdmin = authToken.Success && authToken.Value!.IsAdmin;

        if (IsAdmin)
        {
            await RefreshTeams();
        }

        IsLoading = false;
        StateHasChanged();
    }

    private async Task RefreshTeams()
    {
        if (AuthToken == null)
        {
            return;
        }

        var response = await Mediator.Send(new GetTeamManagementOverviewQuery
        {
            RequestingTeamId = AuthToken.TeamId
        });

        if (!response.IsAuthorized)
        {
            IsAdmin = false;
            ManagedTeams = new List<ManagedTeamDto>();
            return;
        }

        ManagedTeams = response.Teams;
    }

    private async Task ResetAllChallenges(Guid teamId)
    {
        if (AuthToken == null)
        {
            return;
        }

        var response = await Mediator.Send(new ResetTeamChallengesCommand
        {
            RequestingTeamId = AuthToken.TeamId,
            TeamId = teamId
        });

        IsErrorMessage = !response.Success;
        Message = response.Success ? "All challenges have been reset for this team." : response.ErrorMessage;
        await RefreshTeams();
        StateHasChanged();
    }
}
