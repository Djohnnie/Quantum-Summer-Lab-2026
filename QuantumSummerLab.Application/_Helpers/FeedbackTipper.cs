using QuantumSummerLab.Application.Scores.Commands;

namespace QuantumSummerLab.Application.Helpers;

public interface IFeedbackTipper
{
    Task<string> GetTip(string challengeDescription, string correctSolution, List<VerificationFeedback> feedback, string submission);
}
