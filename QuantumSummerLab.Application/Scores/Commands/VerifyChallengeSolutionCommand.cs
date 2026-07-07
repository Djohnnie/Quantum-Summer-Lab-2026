using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuantumSummerLab.Application.Extensions;
using QuantumSummerLab.Application.Helpers;
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
    public string Tips { get; set; }
}

public class VerificationFeedback
{
    public bool Valid { get; set; }
    public string Message { get; set; }
    public string Details { get; set; }
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
    public string Details { get; set; }
}

public class VerifyChallengeSolutionCommandHandler : IRequestHandler<VerifyChallengeSolutionCommand, VerifyChallengeSolutionResponse>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IErrorSummarizer _errorSummarizer;
    private readonly IFeedbackTipper _feedbackTipper;

    public VerifyChallengeSolutionCommandHandler(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        IErrorSummarizer errorSummarizer,
        IFeedbackTipper feedbackTipper)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _errorSummarizer = errorSummarizer;
        _feedbackTipper = feedbackTipper;
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

        var rawFeedback = feedback.Messages.Select(message => new VerificationFeedback
        {
            Valid = message.Valid,
            Message = message.Message,
            Details = message.Details
        }).ToList();

        // Only ask for a tip on a failed submission; the tip nudges the participant
        // forward based on the feedback without ever revealing the solution. The
        // challenge description and reference solution are passed along as context so
        // the tip is grounded in what a correct solution should do. Challenges seeded
        // before the Solution column existed fall back to the verification template,
        // which also embeds the expected behavior. The tip works off the raw feedback,
        // so it can run concurrently with the error summarizations below.
        var correctSolution = string.IsNullOrWhiteSpace(challenge.Solution)
            ? verificationTemplate
            : challenge.Solution.FromBase64String();

        var tipTask = feedback.IsValid
            ? Task.FromResult<string>(null)
            : _feedbackTipper.GetTip(
                challenge.Description.Replace("[BR]", "\n"),
                correctSolution,
                rawFeedback,
                request.Solution);

        // Details on a failed message holds the raw Q# compiler/runtime error; rewrite it
        // in plain language so participants aren't shown a wall of technical output.
        var summaryTasks = feedback.Messages.Select(async message => new VerificationFeedback
        {
            Valid = message.Valid,
            Message = message.Message,
            Details = !message.Valid && !string.IsNullOrWhiteSpace(message.Details)
                ? await _errorSummarizer.SummarizeError(message.Details, request.Solution)
                : message.Details
        });

        var verificationFeedback = (await Task.WhenAll(summaryTasks)).ToList();
        var tips = await tipTask;

        // The score is saved only after the LLM calls complete so the tip and the
        // summarized (readable) error details can be stored with the submission and
        // shown again in the submission history. GetTip and SummarizeError swallow
        // their own failures, so this save is not at risk of being skipped because
        // of an LLM error.
        var storedFeedback = new QSharpFeedback
        {
            IsValid = feedback.IsValid,
            Messages = verificationFeedback.Select(x => new QSharpFeedbackMessage
            {
                Valid = x.Valid,
                Message = x.Message,
                Details = x.Details
            }).ToList()
        };

        dbContext.Scores.Add(new Score
        {
            Challenge = challenge,
            Team = team,
            IsSuccessful = feedback.IsValid,
            Feedback = JsonSerializer.Serialize(storedFeedback),
            Tip = string.IsNullOrWhiteSpace(tips) ? null : tips,
            ProposedSolution = request.Solution.ToBase64String(),
            SubmissionTimestamp = request.Timestamp
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        return new VerifyChallengeSolutionResponse
        {
            IsValid = feedback.IsValid,
            FeedbackMessage = $"Your submitted solution {(feedback.IsValid ? "is" : "is not")} correct.",
            Feedback = verificationFeedback,
            Tips = tips
        };
    }

    private static VerifyChallengeSolutionResponse Failure(string message) => new()
    {
        IsValid = false,
        FeedbackMessage = message,
        Feedback = new List<VerificationFeedback>()
    };
}