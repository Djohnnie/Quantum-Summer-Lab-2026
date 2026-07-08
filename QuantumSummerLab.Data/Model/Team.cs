namespace QuantumSummerLab.Data.Model;

public class Team
{
    public Guid Id { get; set; }
    public int SysId { get; set; }
    public string Name { get; set; } = null!;
    public string PasswordSalt { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public bool IsAdmin { get; set; }
    public bool IsApproved { get; set; }
    public bool IsArchived { get; set; }
}