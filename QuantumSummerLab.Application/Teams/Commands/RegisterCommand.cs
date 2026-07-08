using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuantumSummerLab.Application.Helpers;
using QuantumSummerLab.Data;
using QuantumSummerLab.Data.Model;

namespace QuantumSummerLab.Application.Teams.Commands;

public class RegisterCommand : IRequest<RegisterResponse>
{
    public string TeamName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RegisterResponse
{
    public bool Success { get; set; }
    public bool RequiresApproval { get; set; }
    public AuthenticationToken? Token { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, RegisterResponse>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IPasswordHashHelper _passwordHashHelper;

    public RegisterCommandHandler(
        IServiceScopeFactory scopeFactory,
        IPasswordHashHelper passwordHashHelper)
    {
        _scopeFactory = scopeFactory;
        _passwordHashHelper = passwordHashHelper;
    }

    public async Task<RegisterResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.TeamName) || string.IsNullOrWhiteSpace(request.Password))
        {
            return new RegisterResponse
            {
                Success = false,
                ErrorMessage = "Team name and password must be provided!"
            };
        }

        using var dbContext = _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<QuantumSummerLabDbContext>();
        var isFirstTeam = !await dbContext.Teams.AnyAsync(cancellationToken);
        var passwordHash = _passwordHashHelper.CalculateHash(request.Password);

        var newTeam = new Team
        {
            Name = request.TeamName,
            PasswordSalt = passwordHash.Salt,
            PasswordHash = passwordHash.Hash,
            IsAdmin = isFirstTeam,
            IsApproved = isFirstTeam
        };

        dbContext.Teams.Add(newTeam);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return new RegisterResponse
            {
                Success = false,
                ErrorMessage = $"We are not able to register a team using the name '{request.TeamName}'"
            };
        }

        if (isFirstTeam)
        {
            return new RegisterResponse
            {
                Success = true,
                RequiresApproval = false,
                Token = new AuthenticationToken
                {
                    TeamId = newTeam.Id,
                    TeamName = newTeam.Name,
                    IsAdmin = true
                }
            };
        }

        return new RegisterResponse
        {
            Success = true,
            RequiresApproval = true,
            ErrorMessage = "Your team was registered successfully and is now pending admin approval before login."
        };
    }
}