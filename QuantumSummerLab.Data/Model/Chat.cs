namespace QuantumSummerLab.Data.Model;

public class Chat
{
    public Guid Id { get; set; }
    public int SysId { get; set; }
    public Team Team { get; set; } = null!;
    public string Message { get; set; } = null!;
    public string Role { get; set; } = null!;
    public DateTime Timestamp { get; set; }
    public int TokensUsed { get; set; }
    public bool IsReduced { get; set; }
    public bool IsDeleted { get; set; }
    public int ProcessingTime { get; set; }
}