using QuantumSummerLab.Application.Scores.Queries;

namespace QuantumSummerLab.Web.Components.Pages;

public partial class Leaderboard
{
    private List<LeaderboardEntry> Entries { get; set; }
    private string SearchString = string.Empty;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var result = await Mediator.Send(new GetLeaderboardQuery());
            Entries = result.Entries;

            StateHasChanged();
        }
    }

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
