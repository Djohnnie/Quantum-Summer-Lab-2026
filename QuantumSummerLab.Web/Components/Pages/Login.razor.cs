using Microsoft.AspNetCore.Components.Web;
using QuantumSummerLab.Application.Teams.Commands;
using QuantumSummerLab.Web.Helpers;

namespace QuantumSummerLab.Web.Components.Pages;

public partial class Login
{
    private int currentCount = 0;

    private bool IsLoggedIn { get; set; }
    private string LoggedInTeamName { get; set; }
    private string ErrorMessage { get; set; } = string.Empty;
    private string InfoMessage { get; set; } = string.Empty;
    private string TeamName { get; set; } = string.Empty;
    private string Password { get; set; } = string.Empty;
    private bool IsAuthenticating { get; set; }
    private bool IsRegistering { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var authentication = await ProtectedLocalStore.GetAsync<AuthenticationToken>("authToken");
            IsLoggedIn = authentication.Success;
            LoggedInTeamName = authentication.Value?.TeamName ?? string.Empty;

            StateHasChanged();
        }
    }

    protected async Task Authenticate()
    {
        if (IsAuthenticating || IsRegistering)
        {
            return;
        }

        ErrorMessage = string.Empty;
        InfoMessage = string.Empty;
        IsAuthenticating = true;
        StateHasChanged();

        var response = await Mediator.Send(new LoginCommand
        {
            TeamName = TeamName,
            Password = Password
        });

        if (response.Success)
        {
            await ProtectedLocalStore.SetAsync("authToken", response.Token);
            NavigationHelper.Update();
            NavigationManager.NavigateTo("/");
        }
        else
        {
            ErrorMessage = response.ErrorMessage;
            IsAuthenticating = false;
            StateHasChanged();
        }
    }

    protected async Task Register()
    {
        if (IsAuthenticating || IsRegistering)
        {
            return;
        }

        ErrorMessage = string.Empty;
        InfoMessage = string.Empty;
        IsRegistering = true;
        StateHasChanged();

        var response = await Mediator.Send(new RegisterCommand
        {
            TeamName = TeamName,
            Password = Password
        });

        if (response.Success && response.Token != null)
        {
            await ProtectedLocalStore.SetAsync("authToken", response.Token);
            NavigationHelper.Update();
            NavigationManager.NavigateTo("/");
        }
        else if (response.Success && response.RequiresApproval)
        {
            Password = string.Empty;
            InfoMessage = response.ErrorMessage;
            IsRegistering = false;
            StateHasChanged();
        }
        else
        {
            ErrorMessage = response.ErrorMessage;
            IsRegistering = false;
            StateHasChanged();
        }
    }

    protected async Task Logout()
    {
        await ProtectedLocalStore.DeleteAsync("authToken");
        NavigationHelper.Update();
        NavigationManager.NavigateTo("/");
    }

    protected async Task OnKeyUp(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            await Authenticate();
        }
    }
}
