using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuantumSummerLab.Data;

namespace QuantumSummerLab.Application.Teams.Queries;

public class GetTeamManagementOverviewQuery : IRequest<GetTeamManagementOverviewResponse>
{
    public Guid RequestingTeamId { get; set; }
}

public class GetTeamManagementOverviewResponse
{
    public bool IsAuthorized { get; set; }
    public List<ManagedTeamDto> Teams { get; set; } = new List<ManagedTeamDto>();
}

public class ManagedTeamDto
{
    public Guid TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsArchived { get; set; }
    public int SubmissionCount { get; set; }
    public int TotalScore { get; set; }
    public int ChallengesSucceeded { get; set; }
    public int ChallengesFailed { get; set; }
    public int MessagesSent { get; set; }
}

public class GetTeamManagementOverviewQueryHandler : IRequestHandler<GetTeamManagementOverviewQuery, GetTeamManagementOverviewResponse>
{
    private readonly IServiceScopeFactory _scopeFactory;

    public GetTeamManagementOverviewQueryHandler(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<GetTeamManagementOverviewResponse> Handle(GetTeamManagementOverviewQuery request, CancellationToken cancellationToken)
    {
        using var dbContext = _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<QuantumSummerLabDbContext>();

        var requestingTeam = await dbContext.Teams
            .SingleOrDefaultAsync(x => x.Id == request.RequestingTeamId, cancellationToken);

        if (requestingTeam == null || requestingTeam.IsArchived || !requestingTeam.IsApproved || !requestingTeam.IsAdmin)
        {
            return new GetTeamManagementOverviewResponse
            {
                IsAuthorized = false
            };
        }

        var teams = await dbContext.Teams
            .OrderBy(x => x.IsArchived)
            .ThenBy(x => x.Name)
            .Select(x => new ManagedTeamDto
            {
                TeamId = x.Id,
                TeamName = x.Name,
                IsApproved = x.IsApproved,
                IsAdmin = x.IsAdmin,
                IsArchived = x.IsArchived
            })
            .ToListAsync(cancellationToken);

        var submissions = await dbContext.Scores
            .Select(s => new
            {
                TeamId = s.Team.Id,
                ChallengeId = s.Challenge.Id,
                ChallengeLevel = s.Challenge.Level,
                s.IsSuccessful
            })
            .ToListAsync(cancellationToken);

        var scoreStatsByTeam = submissions
            .GroupBy(s => s.TeamId)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    SubmissionCount = g.Count(),
                    TotalScore = g.Sum(s => s.IsSuccessful ? s.ChallengeLevel * 100 : -1),
                    ChallengesSucceeded = g.Where(s => s.IsSuccessful).Select(s => s.ChallengeId).Distinct().Count(),
                    ChallengesAttempted = g.Select(s => s.ChallengeId).Distinct().Count()
                });

        var messagesSentByTeam = (await dbContext.Chats
            .Where(c => c.Role == "User")
            .GroupBy(c => c.Team.Id)
            .Select(g => new { TeamId = g.Key, MessagesSent = g.Count() })
            .ToListAsync(cancellationToken))
            .ToDictionary(x => x.TeamId, x => x.MessagesSent);

        foreach (var team in teams)
        {
            if (scoreStatsByTeam.TryGetValue(team.TeamId, out var stats))
            {
                team.SubmissionCount = stats.SubmissionCount;
                team.TotalScore = stats.TotalScore;
                team.ChallengesSucceeded = stats.ChallengesSucceeded;
                team.ChallengesFailed = stats.ChallengesAttempted - stats.ChallengesSucceeded;
            }

            if (messagesSentByTeam.TryGetValue(team.TeamId, out var messagesSent))
            {
                team.MessagesSent = messagesSent;
            }
        }

        return new GetTeamManagementOverviewResponse
        {
            IsAuthorized = true,
            Teams = teams
        };
    }
}
