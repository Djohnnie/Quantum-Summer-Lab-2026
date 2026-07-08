using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuantumSummerLab.Data;

namespace QuantumSummerLab.Application.Scores.Commands;

public class ResetChallengeCommand : IRequest<ResetChallengeResponse>
{
    public Guid RequestingTeamId { get; set; }
    public Guid TeamId { get; set; }
    public string ChallengeName { get; set; } = string.Empty;
}

public class ResetChallengeResponse
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}

public class ResetChallengeCommandHandler : IRequestHandler<ResetChallengeCommand, ResetChallengeResponse>
{
    private readonly IServiceScopeFactory _scopeFactory;

    public ResetChallengeCommandHandler(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<ResetChallengeResponse> Handle(ResetChallengeCommand request, CancellationToken cancellationToken)
    {
        using var dbContext = _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<QuantumSummerLabDbContext>();

        var requestingTeam = await dbContext.Teams.SingleOrDefaultAsync(x => x.Id == request.RequestingTeamId, cancellationToken);
        if (requestingTeam == null || requestingTeam.IsArchived || !requestingTeam.IsApproved || !requestingTeam.IsAdmin)
        {
            return new ResetChallengeResponse
            {
                Success = false,
                ErrorMessage = "You are not authorized to reset challenges."
            };
        }

        await dbContext.Scores
            .Where(x => x.Team.Id == request.TeamId && x.Challenge.Name == request.ChallengeName)
            .ExecuteDeleteAsync(cancellationToken);

        return new ResetChallengeResponse
        {
            Success = true
        };
    }
}
