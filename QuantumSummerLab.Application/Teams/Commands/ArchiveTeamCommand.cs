using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuantumSummerLab.Data;

namespace QuantumSummerLab.Application.Teams.Commands;

public class ArchiveTeamCommand : IRequest<ArchiveTeamResponse>
{
    public Guid RequestingTeamId { get; set; }
    public Guid TeamId { get; set; }
}

public class ArchiveTeamResponse
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}

public class ArchiveTeamCommandHandler : IRequestHandler<ArchiveTeamCommand, ArchiveTeamResponse>
{
    private readonly IServiceScopeFactory _scopeFactory;

    public ArchiveTeamCommandHandler(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<ArchiveTeamResponse> Handle(ArchiveTeamCommand request, CancellationToken cancellationToken)
    {
        using var dbContext = _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<QuantumSummerLabDbContext>();

        if (request.RequestingTeamId != Guid.Empty)
        {
            var requestingTeam = await dbContext.Teams.SingleOrDefaultAsync(x => x.Id == request.RequestingTeamId, cancellationToken);
            if (requestingTeam == null || requestingTeam.IsArchived || !requestingTeam.IsApproved || !requestingTeam.IsAdmin)
            {
                return new ArchiveTeamResponse
                {
                    Success = false,
                    ErrorMessage = "You are not authorized to archive teams."
                };
            }
        }

        var team = await dbContext.Teams.SingleOrDefaultAsync(x => x.Id == request.TeamId, cancellationToken);
        if (team == null)
        {
            return new ArchiveTeamResponse
            {
                Success = false,
                ErrorMessage = "The selected team could not be archived."
            };
        }

        if (team.IsArchived)
        {
            return new ArchiveTeamResponse
            {
                Success = true
            };
        }

        if (team.IsAdmin)
        {
            var activeAdmins = await dbContext.Teams.CountAsync(
                x => x.IsAdmin && x.IsApproved && !x.IsArchived,
                cancellationToken);

            if (activeAdmins <= 1)
            {
                return new ArchiveTeamResponse
                {
                    Success = false,
                    ErrorMessage = "At least one approved admin team is required."
                };
            }
        }

        team.IsArchived = true;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new ArchiveTeamResponse
        {
            Success = true
        };
    }
}