using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuantumSummerLab.Application.Extensions;
using QuantumSummerLab.Data;

namespace QuantumSummerLab.Application.Challenges.Queries;

public class GetChallengeByNameQuery : IRequest<GetChallengeByNameResponse>
{
    public string ChallengeName { get; set; } = string.Empty;
}

public class GetChallengeByNameResponse
{
    public bool IsAvailable { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Tldr { get; set; } = string.Empty;
    public string SolutionTemplate { get; set; } = string.Empty;
    public string ExampleDescription { get; set; } = string.Empty;
    public string ExampleCode { get; set; } = string.Empty;
    public string CopilotInstructions { get; set; } = string.Empty;
}

public class GetChallengeByNameQueryHandler : IRequestHandler<GetChallengeByNameQuery, GetChallengeByNameResponse>
{
    private readonly IServiceScopeFactory _scopeFactory;

    public GetChallengeByNameQueryHandler(
        IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<GetChallengeByNameResponse> Handle(GetChallengeByNameQuery request, CancellationToken cancellationToken)
    {
        using var dbContext = _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<QuantumSummerLabDbContext>();
        var challenge = await dbContext.Challenges
            .FirstOrDefaultAsync(c => c.Name == request.ChallengeName, cancellationToken);

        if (challenge is null)
        {
            return new GetChallengeByNameResponse { IsAvailable = false };
        }

        return new GetChallengeByNameResponse
        {
            IsAvailable = true,
            Name = challenge.Name,
            Level = challenge.Level,
            Title = challenge.Title,
            Description = challenge.Description,
            Tldr = challenge.Tldr,
            SolutionTemplate = challenge.SolutionTemplate.FromBase64String(),
            ExampleDescription = challenge.ExampleDescription,
            ExampleCode = challenge.ExampleCode.FromBase64String(),
            CopilotInstructions = challenge.CopilotInstructions
        };
    }
}