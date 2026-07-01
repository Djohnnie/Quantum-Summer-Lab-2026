using Microsoft.AspNetCore.Components;
using QuantumSummerLab.Application.Challenges.Queries;
using QuantumSummerLab.Application.Scores.Commands;
using QuantumSummerLab.Application.Scores.Queries;
using QuantumSummerLab.Application.Teams.Commands;
using QuantumSummerLab.Application.Teams.Queries;
using QuantumSummerLab.Copilot.Extensions;

namespace QuantumSummerLab.Web.Components.Pages;

public partial class TeamReport
{
    [Parameter]
    public string TeamId { get; set; }

    private bool IsLoading { get; set; } = true;
    private bool IsLoggedIn { get; set; }
    private bool IsAdmin { get; set; }
    private bool TeamNotFound { get; set; }
    private bool IsErrorMessage { get; set; }
    private string Message { get; set; } = string.Empty;
    private string TeamName { get; set; } = string.Empty;
    private AuthenticationToken? AuthToken { get; set; }

    private List<ChallengeGroup> _challenges =
        [
            new ChallengeGroup
            {
                Title = "Example challenges",
                Challenges = [
                    new ChallengeItem { Name = "0" }
                ]
            },
            new ChallengeGroup
            {
                Title = "Easy challenges",
                Challenges = [
                    new ChallengeItem { Name = "A1" },
                    new ChallengeItem { Name = "A2" },
                    new ChallengeItem { Name = "A3" }
                ]
            },
            new ChallengeGroup
            {
                Title = "Moderate challenges",
                Challenges = [
                    new ChallengeItem { Name = "B1" },
                    new ChallengeItem { Name = "B2" },
                    new ChallengeItem { Name = "B3" }
                ]
            },
            new ChallengeGroup
            {
                Title = "Hard challenges",
                Challenges = [
                    new ChallengeItem { Name = "C1" },
                    new ChallengeItem { Name = "C2" },
                    new ChallengeItem { Name = "C3" }
                ]
            },
            new ChallengeGroup
            {
                Title = "Extra challenges",
                Challenges = [
                    new ChallengeItem { Name = "D1" },
                    new ChallengeItem { Name = "D2" },
                    new ChallengeItem { Name = "D3" }
                ]
            }
    ];

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

        AuthToken = authToken.Value;

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
        IsLoading = false;
        StateHasChanged();

        await LoadChallenges();
    }

    private async Task LoadChallenges()
    {
        foreach (var group in _challenges)
        {
            foreach (var challenge in group.Challenges)
            {
                await LoadChallenge(challenge);
                StateHasChanged();
            }
        }
    }

    private async Task LoadChallenge(ChallengeItem challenge)
    {
        var challengeResponse = await Mediator.Send(new GetChallengeByNameQuery { ChallengeName = challenge.Name });
        if (challengeResponse != null)
        {
            challenge.Title = challengeResponse.Title;
        }

        var scoreResponse = await Mediator.Send(new GetYourScoreQuery { ChallengeName = challenge.Name, TeamName = TeamName });
        if (scoreResponse != null)
        {
            challenge.TeamName = TeamName;
            challenge.IsSuccess = scoreResponse.TotalAttempts == 0 ? null : scoreResponse.IsSuccess;
            challenge.TotalAttempts = scoreResponse.TotalAttempts;
            challenge.Score = scoreResponse.Score;
        }

        var submissionsResponse = await Mediator.Send(new GetYourSubmissionsQuery { ChallengeName = challenge.Name, TeamName = TeamName });
        if (submissionsResponse != null)
        {
            challenge.Submissions = submissionsResponse.YourSubmissions
                .Select(s => new SubmissionItem
                {
                    IsSuccess = s.IsSuccessful,
                    ProposedSolution = s.ProposedSolution,
                    SubmissionTimestamp = s.SubmissionTimestamp,
                    Feedback = s.Feedback.Select(f => new FeedbackItem
                    {
                        IsValid = f.Valid,
                        Message = f.Message,
                        Details = f.Details
                    }).ToList()
                })
                .ToList();
        }
    }

    private async Task ResetChallenge(ChallengeItem challenge)
    {
        if (AuthToken == null || !Guid.TryParse(TeamId, out var teamId))
        {
            return;
        }

        var response = await Mediator.Send(new ResetChallengeCommand
        {
            RequestingTeamId = AuthToken.TeamId,
            TeamId = teamId,
            ChallengeName = challenge.Name
        });

        IsErrorMessage = !response.Success;
        Message = response.Success
            ? $"All submissions for challenge {challenge.Name} have been removed."
            : response.ErrorMessage;

        if (response.Success)
        {
            await LoadChallenge(challenge);
        }

        StateHasChanged();
    }

    class ChallengeGroup
    {
        public string Title { get; set; }

        public List<ChallengeItem> Challenges { get; set; } = new List<ChallengeItem>();
    }

    class ChallengeItem
    {
        public string Name { get; set; }
        public string Title { get; set; } = "...";
        public string TeamName { get; set; }
        public bool? IsSuccess { get; set; }
        public int TotalAttempts { get; set; }
        public int Score { get; set; }

        public List<SubmissionItem> Submissions { get; set; } = new List<SubmissionItem>();
    }

    class SubmissionItem
    {
        public bool IsSuccess { get; set; }
        public string ProposedSolution { get; set; }
        public DateTime SubmissionTimestamp { get; set; }

        public List<FeedbackItem> Feedback { get; set; } = new List<FeedbackItem>();
    }

    class FeedbackItem
    {
        public bool IsValid { get; set; }
        public string Message { get; set; }
        public string Details { get; set; }
    }
}
