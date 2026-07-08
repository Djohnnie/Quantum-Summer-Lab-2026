using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuantumSummerLab.Data;

namespace QuantumSummerLab.Application.Scores.Queries;

public class GetChallengeStatisticsQuery : IRequest<GetChallengeStatisticsResponse>
{
    public string ChallengeName { get; set; } = string.Empty;
    public Guid RequestingTeamId { get; set; }
}

public class GetChallengeStatisticsResponse
{
    public int TeamsCompleted { get; set; }
}

public class GetChallengeStatisticsQueryHandler : IRequestHandler<GetChallengeStatisticsQuery, GetChallengeStatisticsResponse>
{
    private readonly IServiceScopeFactory _scopeFactory;

    public GetChallengeStatisticsQueryHandler(
        IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<GetChallengeStatisticsResponse> Handle(GetChallengeStatisticsQuery request, CancellationToken cancellationToken)
    {
        using var dbContext = _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<QuantumSummerLabDbContext>();

        var teamsCompleted = await dbContext.Scores
            .Where(x => x.Challenge.Name == request.ChallengeName
                && x.IsSuccessful
                && !x.Team.IsArchived
                && !x.Team.IsAdmin
                && x.Team.Id != request.RequestingTeamId)
            .Select(x => x.Team.Id)
            .Distinct()
            .CountAsync(cancellationToken);

        return new GetChallengeStatisticsResponse
        {
            TeamsCompleted = teamsCompleted
        };
    }
}
