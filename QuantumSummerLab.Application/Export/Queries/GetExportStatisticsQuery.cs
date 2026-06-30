using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuantumSummerLab.Data;

namespace QuantumSummerLab.Application.Export.Queries;

public class GetExportStatisticsQuery : IRequest<GetExportStatisticsResponse>
{
    public Guid RequestingTeamId { get; set; }
}

public class GetExportStatisticsResponse
{
    public bool IsAuthorized { get; set; }
    public int TeamCount { get; set; }
    public int ChallengeCount { get; set; }
    public int SubmissionCount { get; set; }
    public int ChatMessageCount { get; set; }
}

public class GetExportStatisticsQueryHandler : IRequestHandler<GetExportStatisticsQuery, GetExportStatisticsResponse>
{
    private readonly IServiceScopeFactory _scopeFactory;

    public GetExportStatisticsQueryHandler(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<GetExportStatisticsResponse> Handle(GetExportStatisticsQuery request, CancellationToken cancellationToken)
    {
        using var dbContext = _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<QuantumSummerLabDbContext>();

        var requestingTeam = await dbContext.Teams.SingleOrDefaultAsync(x => x.Id == request.RequestingTeamId, cancellationToken);
        if (requestingTeam == null || requestingTeam.IsArchived || !requestingTeam.IsApproved || !requestingTeam.IsAdmin)
        {
            return new GetExportStatisticsResponse { IsAuthorized = false };
        }

        return new GetExportStatisticsResponse
        {
            IsAuthorized = true,
            TeamCount = await dbContext.Teams.CountAsync(cancellationToken),
            ChallengeCount = await dbContext.Challenges.CountAsync(cancellationToken),
            SubmissionCount = await dbContext.Scores.CountAsync(cancellationToken),
            ChatMessageCount = await dbContext.Chats.CountAsync(cancellationToken)
        };
    }
}
