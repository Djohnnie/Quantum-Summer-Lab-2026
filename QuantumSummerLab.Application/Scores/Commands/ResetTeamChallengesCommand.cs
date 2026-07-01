using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuantumSummerLab.Data;

namespace QuantumSummerLab.Application.Scores.Commands;

public class ResetTeamChallengesCommand : IRequest<ResetTeamChallengesResponse>
{
    public Guid RequestingTeamId { get; set; }
    public Guid TeamId { get; set; }
}

public class ResetTeamChallengesResponse
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}

public class ResetTeamChallengesCommandHandler : IRequestHandler<ResetTeamChallengesCommand, ResetTeamChallengesResponse>
{
    private readonly IServiceScopeFactory _scopeFactory;

    public ResetTeamChallengesCommandHandler(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<ResetTeamChallengesResponse> Handle(ResetTeamChallengesCommand request, CancellationToken cancellationToken)
    {
        using var dbContext = _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<QuantumSummerLabDbContext>();

        var requestingTeam = await dbContext.Teams.SingleOrDefaultAsync(x => x.Id == request.RequestingTeamId, cancellationToken);
        if (requestingTeam == null || requestingTeam.IsArchived || !requestingTeam.IsApproved || !requestingTeam.IsAdmin)
        {
            return new ResetTeamChallengesResponse
            {
                Success = false,
                ErrorMessage = "You are not authorized to reset challenges."
            };
        }

        await dbContext.Scores
            .Where(x => x.Team.Id == request.TeamId)
            .ExecuteDeleteAsync(cancellationToken);

        return new ResetTeamChallengesResponse
        {
            Success = true
        };
    }
}
