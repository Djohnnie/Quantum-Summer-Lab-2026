using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuantumSummerLab.Application.Scores.Queries;
using QuantumSummerLab.Data;

namespace QuantumSummerLab.Application.Export.Queries;

public class ExportDataQuery : IRequest<ExportDataResponse>
{
    public Guid RequestingTeamId { get; set; }
}

public class ExportDataResponse
{
    public bool IsAuthorized { get; set; }
    public DateTime GeneratedAtUtc { get; set; }
    public List<ExportTeam> Teams { get; set; } = new List<ExportTeam>();
    public List<ExportChallenge> Challenges { get; set; } = new List<ExportChallenge>();
    public List<ExportScore> Scores { get; set; } = new List<ExportScore>();
    public List<ExportChat> Chats { get; set; } = new List<ExportChat>();
    public List<LeaderboardEntry> Leaderboard { get; set; } = new List<LeaderboardEntry>();
}

public class ExportTeam
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public bool IsApproved { get; set; }
    public bool IsArchived { get; set; }
}

public class ExportChallenge
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Tldr { get; set; } = string.Empty;
    public int Level { get; set; }
    public string ExampleCode { get; set; } = string.Empty;
    public string ExampleDescription { get; set; } = string.Empty;
    public string VerificationTemplate { get; set; } = string.Empty;
    public string SolutionTemplate { get; set; } = string.Empty;
    public string ExpectedOutput { get; set; } = string.Empty;
    public string ExpectedStates { get; set; } = string.Empty;
    public string CopilotInstructions { get; set; } = string.Empty;
}

public class ExportScore
{
    public Guid Id { get; set; }
    public string ChallengeName { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
    public string ProposedSolution { get; set; } = string.Empty;
    public bool IsSuccessful { get; set; }
    public string Feedback { get; set; } = string.Empty;
    public DateTime SubmissionTimestamp { get; set; }
}

public class ExportChat
{
    public Guid Id { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public int TokensUsed { get; set; }
    public bool IsReduced { get; set; }
    public bool IsDeleted { get; set; }
    public int ProcessingTime { get; set; }
}

public class ExportDataQueryHandler : IRequestHandler<ExportDataQuery, ExportDataResponse>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISender _sender;

    public ExportDataQueryHandler(IServiceScopeFactory scopeFactory, ISender sender)
    {
        _scopeFactory = scopeFactory;
        _sender = sender;
    }

    public async Task<ExportDataResponse> Handle(ExportDataQuery request, CancellationToken cancellationToken)
    {
        using var dbContext = _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<QuantumSummerLabDbContext>();

        var requestingTeam = await dbContext.Teams.SingleOrDefaultAsync(x => x.Id == request.RequestingTeamId, cancellationToken);
        if (requestingTeam == null || requestingTeam.IsArchived || !requestingTeam.IsApproved || !requestingTeam.IsAdmin)
        {
            return new ExportDataResponse { IsAuthorized = false };
        }

        var teams = await dbContext.Teams
            .OrderBy(x => x.Name)
            .Select(x => new ExportTeam
            {
                Id = x.Id,
                Name = x.Name,
                IsAdmin = x.IsAdmin,
                IsApproved = x.IsApproved,
                IsArchived = x.IsArchived
            })
            .ToListAsync(cancellationToken);

        var challenges = await dbContext.Challenges
            .OrderBy(x => x.Name)
            .Select(x => new ExportChallenge
            {
                Id = x.Id,
                Name = x.Name,
                Title = x.Title,
                Description = x.Description,
                Tldr = x.Tldr,
                Level = x.Level,
                ExampleCode = x.ExampleCode,
                ExampleDescription = x.ExampleDescription,
                VerificationTemplate = x.VerificationTemplate,
                SolutionTemplate = x.SolutionTemplate,
                ExpectedOutput = x.ExpectedOutput,
                ExpectedStates = x.ExpectedStates,
                CopilotInstructions = x.CopilotInstructions
            })
            .ToListAsync(cancellationToken);

        var scores = await dbContext.Scores
            .OrderBy(x => x.SubmissionTimestamp)
            .Select(x => new ExportScore
            {
                Id = x.Id,
                ChallengeName = x.Challenge.Name,
                TeamName = x.Team.Name,
                ProposedSolution = x.ProposedSolution,
                IsSuccessful = x.IsSuccessful,
                Feedback = x.Feedback,
                SubmissionTimestamp = x.SubmissionTimestamp
            })
            .ToListAsync(cancellationToken);

        var chats = await dbContext.Chats
            .OrderBy(x => x.Timestamp)
            .Select(x => new ExportChat
            {
                Id = x.Id,
                TeamName = x.Team.Name,
                Message = x.Message,
                Role = x.Role,
                Timestamp = x.Timestamp,
                TokensUsed = x.TokensUsed,
                IsReduced = x.IsReduced,
                IsDeleted = x.IsDeleted,
                ProcessingTime = x.ProcessingTime
            })
            .ToListAsync(cancellationToken);

        var leaderboard = await _sender.Send(new GetLeaderboardQuery(), cancellationToken);

        return new ExportDataResponse
        {
            IsAuthorized = true,
            GeneratedAtUtc = DateTime.UtcNow,
            Teams = teams,
            Challenges = challenges,
            Scores = scores,
            Chats = chats,
            Leaderboard = leaderboard.Entries
        };
    }
}
