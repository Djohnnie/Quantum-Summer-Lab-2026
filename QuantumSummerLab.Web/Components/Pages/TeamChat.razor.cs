using Microsoft.AspNetCore.Components;
using QuantumSummerLab.Application.Chats.Queries;
using QuantumSummerLab.Application.Teams.Commands;
using QuantumSummerLab.Application.Teams.Queries;
using QuantumSummerLab.Copilot;
using QuantumSummerLab.Copilot.Extensions;

namespace QuantumSummerLab.Web.Components.Pages;

public partial class TeamChat
{
    [Parameter]
    public string TeamId { get; set; }

    private bool IsLoading { get; set; } = true;
    private bool IsLoggedIn { get; set; }
    private bool IsAdmin { get; set; }
    private bool TeamNotFound { get; set; }
    private string TeamName { get; set; } = string.Empty;

    private ChatHistory _chatHistory = new ChatHistory();

    private bool HasMessages => _chatHistory.Messages
        .Any(x => !string.IsNullOrWhiteSpace(x.Content) && x.Role != ChatRole.System && x.Role != ChatRole.Reduced);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        var authToken = await ProtectedLocalStore.GetAsync<AuthenticationToken>("authToken");
        IsLoggedIn = authToken.Success;
        IsAdmin = authToken.Success && authToken.Value.IsAdmin;

        if (!IsAdmin || authToken.Value is null)
        {
            IsLoading = false;
            StateHasChanged();
            return;
        }

        if (!Guid.TryParse(TeamId, out var teamId))
        {
            TeamNotFound = true;
            IsLoading = false;
            StateHasChanged();
            return;
        }

        var overview = await Mediator.Send(new GetTeamManagementOverviewQuery
        {
            RequestingTeamId = authToken.Value.TeamId
        });

        if (!overview.IsAuthorized)
        {
            IsAdmin = false;
            IsLoading = false;
            StateHasChanged();
            return;
        }

        var team = overview.Teams.SingleOrDefault(x => x.TeamId == teamId);
        if (team is null)
        {
            TeamNotFound = true;
            IsLoading = false;
            StateHasChanged();
            return;
        }

        TeamName = team.TeamName;

        var chatResponse = await Mediator.Send(new GetChatsQuery { TeamName = TeamName, ShouldIncludeDeleted = true });
        if (chatResponse != null)
        {
            RefreshChatHistory(chatResponse);
        }

        IsLoading = false;
        StateHasChanged();
    }

    private void RefreshChatHistory(GetChatsResponse response)
    {
        _chatHistory = new ChatHistory();
        foreach (var chat in response.Messages)
        {
            switch (chat.Role)
            {
                case "User":
                    _chatHistory.AddUserMessage(chat.Message, chat.TokensUsed, chat.Timestamp.AsTimeAgo(), chat.Id, chat.IsReduced, chat.IsDeleted);
                    break;
                case "Assistant":
                    _chatHistory.AddAssistantMessage(chat.Message, chat.TokensUsed, chat.Timestamp.AsTimeAgo(), chat.Id, chat.IsReduced, chat.IsDeleted);
                    break;
                case "Reduced":
                    _chatHistory.AddReducedMessage(chat.Message, chat.TokensUsed, chat.Id, chat.IsReduced);
                    break;
                default:
                    _chatHistory.AddSystemMessage(chat.Message, chat.Id, chat.IsReduced);
                    break;
            }
        }
    }
}
