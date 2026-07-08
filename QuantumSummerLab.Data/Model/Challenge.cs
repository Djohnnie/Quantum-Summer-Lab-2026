namespace QuantumSummerLab.Data.Model;

public class Challenge
{
    public Guid Id { get; set; }
    public int SysId { get; set; }
    public string Name { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string Tldr { get; set; } = null!;
    public int Level { get; set; }
    public string ExampleCode { get; set; } = null!;
    public string ExampleDescription { get; set; } = null!;
    public string VerificationTemplate { get; set; } = null!;
    public string SolutionTemplate { get; set; } = null!;
    public string Solution { get; set; } = null!;
    public string ExpectedOutput { get; set; } = null!;
    public string ExpectedStates { get; set; } = null!;
    public string CopilotInstructions { get; set; } = null!;
}