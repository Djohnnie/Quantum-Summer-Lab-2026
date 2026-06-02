using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuantumSummerLab.Data;

namespace QuantumSummerLab.Application.Teams.Commands;

public class SetTeamAdminStatusCommand : IRequest<SetTeamAdminStatusResponse>
{
    public Guid RequestingTeamId { get; set; }
    public Guid TeamId { get; set; }
    public bool IsAdmin { get; set; }
}

public class SetTeamAdminStatusResponse
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}

public class SetTeamAdminStatusCommandHandler : IRequestHandler<SetTeamAdminStatusCommand, SetTeamAdminStatusResponse>
{
    private readonly IServiceScopeFactory _scopeFactory;

    public SetTeamAdminStatusCommandHandler(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<SetTeamAdminStatusResponse> Handle(SetTeamAdminStatusCommand request, CancellationToken cancellationToken)
    {
        using var dbContext = _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<QuantumSummerLabDbContext>();

        var requestingTeam = await dbContext.Teams.SingleOrDefaultAsync(x => x.Id == request.RequestingTeamId, cancellationToken);
        if (requestingTeam == null || requestingTeam.IsArchived || !requestingTeam.IsApproved || !requestingTeam.IsAdmin)
        {
            return new SetTeamAdminStatusResponse
            {
                Success = false,
                ErrorMessage = "You are not authorized to manage admin access."
            };
        }

        var targetTeam = await dbContext.Teams.SingleOrDefaultAsync(x => x.Id == request.TeamId, cancellationToken);
        if (targetTeam == null || targetTeam.IsArchived)
        {
            return new SetTeamAdminStatusResponse
            {
                Success = false,
                ErrorMessage = "The selected team could not be updated."
            };
        }

        if (targetTeam.IsAdmin == request.IsAdmin)
        {
            return new SetTeamAdminStatusResponse
            {
                Success = true
            };
        }

        if (!request.IsAdmin)
        {
            var activeAdmins = await dbContext.Teams.CountAsync(
                x => x.IsAdmin && x.IsApproved && !x.IsArchived,
                cancellationToken);

            if (activeAdmins <= 1)
            {
                return new SetTeamAdminStatusResponse
                {
                    Success = false,
                    ErrorMessage = "At least one approved admin team is required."
                };
            }
        }

        targetTeam.IsAdmin = request.IsAdmin;

        if (request.IsAdmin && !targetTeam.IsApproved)
        {
            targetTeam.IsApproved = true;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new SetTeamAdminStatusResponse
        {
            Success = true
        };
    }
}
