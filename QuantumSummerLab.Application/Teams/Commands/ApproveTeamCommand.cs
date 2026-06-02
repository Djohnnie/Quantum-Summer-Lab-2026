using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuantumSummerLab.Data;

namespace QuantumSummerLab.Application.Teams.Commands;

public class ApproveTeamCommand : IRequest<ApproveTeamResponse>
{
    public Guid RequestingTeamId { get; set; }
    public Guid TeamId { get; set; }
}

public class ApproveTeamResponse
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}

public class ApproveTeamCommandHandler : IRequestHandler<ApproveTeamCommand, ApproveTeamResponse>
{
    private readonly IServiceScopeFactory _scopeFactory;

    public ApproveTeamCommandHandler(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<ApproveTeamResponse> Handle(ApproveTeamCommand request, CancellationToken cancellationToken)
    {
        using var dbContext = _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<QuantumSummerLabDbContext>();

        var requestingTeam = await dbContext.Teams.SingleOrDefaultAsync(x => x.Id == request.RequestingTeamId, cancellationToken);
        if (requestingTeam == null || requestingTeam.IsArchived || !requestingTeam.IsApproved || !requestingTeam.IsAdmin)
        {
            return new ApproveTeamResponse
            {
                Success = false,
                ErrorMessage = "You are not authorized to approve teams."
            };
        }

        var team = await dbContext.Teams.SingleOrDefaultAsync(x => x.Id == request.TeamId, cancellationToken);
        if (team == null || team.IsArchived)
        {
            return new ApproveTeamResponse
            {
                Success = false,
                ErrorMessage = "The selected team could not be approved."
            };
        }

        if (team.IsApproved)
        {
            return new ApproveTeamResponse
            {
                Success = true
            };
        }

        team.IsApproved = true;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new ApproveTeamResponse
        {
            Success = true
        };
    }
}
