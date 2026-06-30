using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuantumSummerLab.Application.Extensions;
using QuantumSummerLab.Data;
using QuantumSummerLab.Data.Model;
using System.Net.Http.Json;
using System.Text.Json;

namespace QuantumSummerLab.Application.Scores.Commands;

public class VerifyChallengeSolutionCommand : IRequest<VerifyChallengeSolutionResponse>
{
    public string ChallengeName { get; set; }
    public Guid RequestingTeamId { get; set; }
    public string Solution { get; set; }
    public DateTime Timestamp { get; set; }
}

public class VerifyChallengeSolutionResponse
{
    public bool IsValid { get; set; }
    public string FeedbackMessage { get; set; }
    public List<VerificationFeedback> Feedback { get; set; }
}

public class VerificationFeedback
{
    public bool Valid { get; set; }
    public string Message { get; set; }
}

public class QSharpRequest
{
    public string VerificationTemplate { get; set; }
    public string Solution { get; set; }
    public string ExpectedOutput { get; set; }
    public string ExpectedStates { get; set; }
}

public class QSharpFeedback
{
    public bool IsValid { get; set; }
    public List<QSharpFeedbackMessage> Messages { get; set; } = new List<QSharpFeedbackMessage>();
}

public class QSharpFeedbackMessage
{
    public bool Valid { get; set; }
    public string Message { get; set; }
}

public class VerifyChallengeSolutionCommandHandler : IRequestHandler<VerifyChallengeSolutionCommand, VerifyChallengeSolutionResponse>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    public VerifyChallengeSolutionCommandHandler(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<VerifyChallengeSolutionResponse> Handle(VerifyChallengeSolutionCommand request, CancellationToken cancellationToken)
    {
        using var dbContext = _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<QuantumSummerLabDbContext>();

        var team = await dbContext.Teams.SingleOrDefaultAsync(
            x => x.Id == request.RequestingTeamId, cancellationToken);
        if (team == null || team.IsArchived || !team.IsApproved)
        {
            return Failure("You must be signed in with an approved team to submit a solution.");
        }

        var challenge = await dbContext.Challenges.SingleOrDefaultAsync(
                x => x.Name == request.ChallengeName, cancellationToken);
        if (challenge == null)
        {
            return Failure($"The challenge '{request.ChallengeName}' could not be found.");
        }

        var qsharpHelperBaseAddress = _configuration.GetValue<string>("QSHARP_HELPER_BASE_ADDRESS");
        if (string.IsNullOrWhiteSpace(qsharpHelperBaseAddress))
        {
            return Failure("The verification service is not configured. Please try again later.");
        }

        var verificationTemplate = challenge.VerificationTemplate.FromBase64String();

        var requestData = new QSharpRequest
        {
            VerificationTemplate = verificationTemplate.ToBase64String(),
            Solution = request.Solution.ToBase64String(),
            ExpectedOutput = challenge.ExpectedOutput.ToBase64String(),
            ExpectedStates = challenge.ExpectedStates
        };

        QSharpFeedback? feedback;
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri(qsharpHelperBaseAddress);
            var httpResponse = await httpClient.PostAsJsonAsync("api/QSharpVerificationFunction", requestData, cancellationToken);

            if (!httpResponse.IsSuccessStatusCode)
            {
                return Failure("The verification service could not process your solution. Please try again.");
            }

            feedback = await httpResponse.Content.ReadFromJsonAsync<QSharpFeedback>(cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or NotSupportedException)
        {
            return Failure("The verification service is currently unavailable. Please try again later.");
        }

        if (feedback == null)
        {
            return Failure("The verification service returned an invalid response. Please try again.");
        }

        dbContext.Scores.Add(new Score
        {
            Challenge = challenge,
            Team = team,
            IsSuccessful = feedback.IsValid,
            Feedback = JsonSerializer.Serialize(feedback),
            ProposedSolution = request.Solution.ToBase64String(),
            SubmissionTimestamp = request.Timestamp
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        return new VerifyChallengeSolutionResponse
        {
            IsValid = feedback.IsValid,
            FeedbackMessage = $"Your submitted solution {(feedback.IsValid ? "is" : "is not")} correct.",
            Feedback = feedback.Messages.Select(m => new VerificationFeedback
            {
                Valid = m.Valid,
                Message = m.Message
            }).ToList()
        };
    }

    private static VerifyChallengeSolutionResponse Failure(string message) => new()
    {
        IsValid = false,
        FeedbackMessage = message,
        Feedback = new List<VerificationFeedback>()
    };
}