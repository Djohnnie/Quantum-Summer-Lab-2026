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
            .OrderBy(x => x.Name)
            .Select(x => new ManagedTeamDto
            {
                TeamId = x.Id,
                TeamName = x.Name,
                IsApproved = x.IsApproved,
                IsAdmin = x.IsAdmin,
                IsArchived = x.IsArchived
            })
            .ToListAsync(cancellationToken);

        return new GetTeamManagementOverviewResponse
        {
            IsAuthorized = true,
            Teams = teams
        };
    }
}
