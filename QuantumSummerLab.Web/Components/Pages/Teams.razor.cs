using QuantumSummerLab.Application.Teams.Commands;
using QuantumSummerLab.Application.Teams.Queries;
using QuantumSummerLab.Web.Helpers;

namespace QuantumSummerLab.Web.Components.Pages;

public partial class Teams
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

    private async Task ApproveTeam(Guid teamId)
    {
        if (AuthToken == null)
        {
            return;
        }

        var response = await Mediator.Send(new ApproveTeamCommand
        {
            RequestingTeamId = AuthToken.TeamId,
            TeamId = teamId
        });

        IsErrorMessage = !response.Success;
        Message = response.Success ? "Team approved successfully." : response.ErrorMessage;
        await RefreshTeams();
        StateHasChanged();
    }

    private async Task SetAdmin(Guid teamId, bool isAdmin)
    {
        if (AuthToken == null)
        {
            return;
        }

        var response = await Mediator.Send(new SetTeamAdminStatusCommand
        {
            RequestingTeamId = AuthToken.TeamId,
            TeamId = teamId,
            IsAdmin = isAdmin
        });

        IsErrorMessage = !response.Success;
        Message = response.Success ? "Team role updated successfully." : response.ErrorMessage;

        if (response.Success && AuthToken.TeamId == teamId)
        {
            AuthToken.IsAdmin = isAdmin;
            await ProtectedLocalStore.SetAsync("authToken", AuthToken);
            NavigationHelper.Update();
            IsAdmin = isAdmin;
        }

        await RefreshTeams();
        StateHasChanged();
    }

    private async Task ArchiveTeam(Guid teamId)
    {
        if (AuthToken == null)
        {
            return;
        }

        var response = await Mediator.Send(new ArchiveTeamCommand
        {
            RequestingTeamId = AuthToken.TeamId,
            TeamId = teamId
        });

        IsErrorMessage = !response.Success;
        Message = response.Success ? "Team archived successfully." : response.ErrorMessage;
        await RefreshTeams();
        StateHasChanged();
    }
}
