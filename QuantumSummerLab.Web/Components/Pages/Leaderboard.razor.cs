using QuantumSummerLab.Application.Scores.Queries;

namespace QuantumSummerLab.Web.Components.Pages;

public partial class Leaderboard : IDisposable
{
    private List<LeaderboardEntry> Entries { get; set; }
    private string SearchString = string.Empty;
    private PeriodicTimer RefreshTimer;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await LoadEntriesAsync();

            RefreshTimer = new PeriodicTimer(TimeSpan.FromSeconds(30));
            _ = RefreshLoopAsync();
        }
    }

    private async Task RefreshLoopAsync()
    {
        while (await RefreshTimer.WaitForNextTickAsync())
        {
            await LoadEntriesAsync();
        }
    }

    private async Task LoadEntriesAsync()
    {
        var result = await Mediator.Send(new GetLeaderboardQuery());
        Entries = result.Entries;

        await InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        RefreshTimer?.Dispose();
    }

    private static string GetMedalStyle(int position) => position switch
    {
        1 => "background-color:#FFD700;color:#5c4400;",
        2 => "background-color:#C0C0C0;color:#3a3a3a;",
        3 => "background-color:#CD7F32;color:#ffffff;",
        _ => string.Empty,
    };

    private bool FilterFunc(LeaderboardEntry element) => Filter(element, SearchString);

    private bool Filter(LeaderboardEntry entry, string searchString)
    {
        if (string.IsNullOrWhiteSpace(searchString))
            return true;
        if (entry.TeamName.Contains(searchString, StringComparison.OrdinalIgnoreCase))
            return true;
        return false;
    }
}
