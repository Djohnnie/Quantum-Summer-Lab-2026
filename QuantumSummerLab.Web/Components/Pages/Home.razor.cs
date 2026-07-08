using QuantumSummerLab.Application.Scores.Queries;
using QuantumSummerLab.Application.Teams.Commands;

namespace QuantumSummerLab.Web.Components.Pages;

public partial class Home
{
    private bool IsLoggedIn { get; set; }
    private bool IsChallenge0Completed { get; set; }
    private bool IsChallengeA1Completed { get; set; }
    private bool IsChallengeA2Completed { get; set; }
    private bool IsChallengeA3Completed { get; set; }
    private bool IsChallengeB1Completed { get; set; }
    private bool IsChallengeB2Completed { get; set; }
    private bool IsChallengeB3Completed { get; set; }
    private bool IsChallengeC1Completed { get; set; }
    private bool IsChallengeC2Completed { get; set; }
    private bool IsChallengeC3Completed { get; set; }
    private bool IsChallengeD1Completed { get; set; }
    private bool IsChallengeD2Completed { get; set; }
    private bool IsChallengeD3Completed { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var authToken = await ProtectedLocalStore.GetAsync<AuthenticationToken>("authToken");

            IsLoggedIn = authToken.Success;

            if (IsLoggedIn)
            {
                var teamName = authToken.Value!.TeamName;
                var result = await Mediator.Send(new GetYourScoresQuery { TeamName = teamName });
                IsChallenge0Completed = result.Scores.SingleOrDefault(x => x.ChallengeName == "0")?.IsSuccess ?? false;
                IsChallengeA1Completed = result.Scores.SingleOrDefault(x => x.ChallengeName == "A1")?.IsSuccess ?? false;
                IsChallengeA2Completed = result.Scores.SingleOrDefault(x => x.ChallengeName == "A2")?.IsSuccess ?? false;
                IsChallengeA3Completed = result.Scores.SingleOrDefault(x => x.ChallengeName == "A3")?.IsSuccess ?? false;
                IsChallengeB1Completed = result.Scores.SingleOrDefault(x => x.ChallengeName == "B1")?.IsSuccess ?? false;
                IsChallengeB2Completed = result.Scores.SingleOrDefault(x => x.ChallengeName == "B2")?.IsSuccess ?? false;
                IsChallengeB3Completed = result.Scores.SingleOrDefault(x => x.ChallengeName == "B3")?.IsSuccess ?? false;
                IsChallengeC1Completed = result.Scores.SingleOrDefault(x => x.ChallengeName == "C1")?.IsSuccess ?? false;
                IsChallengeC2Completed = result.Scores.SingleOrDefault(x => x.ChallengeName == "C2")?.IsSuccess ?? false;
                IsChallengeC3Completed = result.Scores.SingleOrDefault(x => x.ChallengeName == "C3")?.IsSuccess ?? false;
                IsChallengeD1Completed = result.Scores.SingleOrDefault(x => x.ChallengeName == "D1")?.IsSuccess ?? false;
                IsChallengeD2Completed = result.Scores.SingleOrDefault(x => x.ChallengeName == "D2")?.IsSuccess ?? false;
                IsChallengeD3Completed = result.Scores.SingleOrDefault(x => x.ChallengeName == "D3")?.IsSuccess ?? false;
            }

            StateHasChanged();
        }
    }
}
