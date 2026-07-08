using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuantumSummerLab.Application.Helpers;
using QuantumSummerLab.Data;

namespace QuantumSummerLab.Application.Teams.Commands;

public class LoginCommand : IRequest<LoginResponse>
{
    public string TeamName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public bool Success { get; set; }
    public AuthenticationToken? Token { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}

public class AuthenticationToken
{
    public Guid TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
}

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IPasswordHashHelper _passwordHashHelper;

    public LoginCommandHandler(
        IServiceScopeFactory scopeFactory,
        IPasswordHashHelper passwordHashHelper)
    {
        _scopeFactory = scopeFactory;
        _passwordHashHelper = passwordHashHelper;
    }

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.TeamName) || string.IsNullOrWhiteSpace(request.Password))
        {
            return new LoginResponse
            {
                Success = false,
                ErrorMessage = "Team name and password must be provided!"
            };
        }

        using var dbContext = _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<QuantumSummerLabDbContext>();
        var existingTeam = await dbContext.Teams
            .FirstOrDefaultAsync(t => !t.IsArchived && t.Name == request.TeamName, cancellationToken);

        if (existingTeam == null)
        {
            return new LoginResponse
            {
                Success = false,
                ErrorMessage = "The team name and password provided are not valid!"
            };
        }

        var verificationResult = _passwordHashHelper.Verify(
            request.Password,
            existingTeam.PasswordHash,
            existingTeam.PasswordSalt);

        if (!verificationResult.IsValid)
        {
            return new LoginResponse
            {
                Success = false,
                ErrorMessage = "The team name and password provided are not valid!"
            };
        }

        if (!existingTeam.IsApproved)
        {
            return new LoginResponse
            {
                Success = false,
                ErrorMessage = "Your team is waiting for admin approval before you can login."
            };
        }

        if (verificationResult.ShouldUpgrade)
        {
            var upgradedHash = _passwordHashHelper.CalculateHash(request.Password);
            existingTeam.PasswordHash = upgradedHash.Hash;
            existingTeam.PasswordSalt = upgradedHash.Salt;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return new LoginResponse
        {
            Success = true,
            Token = new AuthenticationToken
            {
                TeamId = existingTeam.Id,
                TeamName = existingTeam.Name,
                IsAdmin = existingTeam.IsAdmin
            }
        };
    }
}