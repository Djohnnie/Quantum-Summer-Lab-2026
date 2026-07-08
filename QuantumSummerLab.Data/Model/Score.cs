namespace QuantumSummerLab.Data.Model;

public class Score
{
    public Guid Id { get; set; }
    public int SysId { get; set; }
    public Challenge Challenge { get; set; } = null!;
    public Team Team { get; set; } = null!;
    public string ProposedSolution { get; set; } = null!;
    public bool IsSuccessful { get; set; }
    public string Feedback { get; set; } = null!;
    public string? Tip { get; set; }
    public DateTime SubmissionTimestamp { get; set; }
}