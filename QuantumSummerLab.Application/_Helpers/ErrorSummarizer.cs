namespace QuantumSummerLab.Application.Helpers;

public interface IErrorSummarizer
{
    Task<string> SummarizeError(string error, string submission);
}
