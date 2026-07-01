using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using QuantumSummerLab.Application.Chats.Queries;
using QuantumSummerLab.Copilot;
using QuantumSummerLab.Copilot.Extensions;

namespace QuantumSummerLab.Web.Components;

public partial class CopilotPane
{
    [Parameter]
    public string TeamName { get; set; }

    [Parameter]
    public bool ShouldAutoScroll { get; set; }

    private ChatHistory _chatHistory = new ChatHistory();
    private bool _isLoading;
    private bool _alert1;
    private bool _alert2;
    private string Chat { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var alert1 = await ProtectedLocalStore.GetAsync<bool>("ALERT_1");
            _alert1 = alert1.Success ? alert1.Value : true;

            var alert2 = await ProtectedLocalStore.GetAsync<bool>("ALERT_2");
            _alert2 = alert2.Success ? alert2.Value : true;

            StateHasChanged();
        }
    }

    public async Task Refresh(string challengeName, string instructions)
    {
        if (!string.IsNullOrEmpty(TeamName))
        {
            await RefreshChatHistory(challengeName, instructions);
            StateHasChanged();

            if (ShouldAutoScroll)
            {
                await Task.Delay(1000);
                await ScrollManager.ScrollToBottomAsync("scrollableContainer", ScrollBehavior.Smooth);
            }
        }
    }

    protected async Task AskQuestion()
    {
        if (string.IsNullOrWhiteSpace(Chat))
        {
            return;
        }
        _chatHistory.TeamName = TeamName;
        _chatHistory.LatestUserMessage = Chat;
        Chat = string.Empty;
        StateHasChanged();

        _isLoading = true;
        var instructions = _chatHistory.Instructions;
        _chatHistory = await CopilotHelper.Chat(_chatHistory);
        _chatHistory.Instructions = instructions;
        _isLoading = false;

        StateHasChanged();

        if (ShouldAutoScroll)
        {
            await Task.Delay(500);
            await ScrollManager.ScrollToBottomAsync("scrollableContainer", ScrollBehavior.Smooth);
        }
    }

    private async Task RefreshChatHistory(string challengeName, string instructions)
    {
        var response = await Mediator.Send(new GetChatsQuery { TeamName = TeamName });
        _chatHistory = new ChatHistory();

        if (!string.IsNullOrEmpty(challengeName))
        {
            _chatHistory.Instructions = $"The current selected challenge is {challengeName}. ";
        }
        else
        {
            _chatHistory.Instructions = "There is no specific challenge selected. The user must specify which challenge he is working on.";
        }

        if (!string.IsNullOrEmpty(instructions))
        {
            _chatHistory.Instructions += $"Your additional instructions are: {instructions}";
        }

        foreach (var chat in response.Messages)
        {
            switch (chat.Role)
            {
                case "User":
                    _chatHistory.AddUserMessage(chat.Message, chat.TokensUsed, chat.Timestamp.AsTimeAgo(), chat.Id, chat.IsReduced);
                    break;
                case "Assistant":
                    _chatHistory.AddAssistantMessage(chat.Message, chat.TokensUsed, chat.Timestamp.AsTimeAgo(), chat.Id, chat.IsReduced);
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

    protected async Task OnKeyUp(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            await AskQuestion();
        }
    }

    protected async Task CloseAlert1()
    {
        await ProtectedLocalStore.SetAsync("ALERT_1", false);
        _alert1 = false;
        StateHasChanged();
    }

    protected async Task CloseAlert2()
    {
        await ProtectedLocalStore.SetAsync("ALERT_2", false);
        _alert2 = false;
        StateHasChanged();
    }
}
