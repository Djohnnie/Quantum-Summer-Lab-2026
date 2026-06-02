using System.Security.Cryptography;
using System.Text;

namespace QuantumSummerLab.Application.Helpers;

public class PasswordHash
{
    public string Password { get; set; } = string.Empty;
    public string Salt { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
}

public class PasswordVerificationResult
{
    public bool IsValid { get; set; }
    public bool ShouldUpgrade { get; set; }
}

public interface IPasswordHashHelper
{
    PasswordHash CalculateHash(string password);
    PasswordVerificationResult Verify(string password, string hash, string metadata);
}

public class PasswordHashHelper : IPasswordHashHelper
{
    private const string CurrentVersion = "bcrypt-v1";
    private const int WorkFactor = 12;

    public PasswordHash CalculateHash(string password)
    {
        var salt = BCrypt.Net.BCrypt.GenerateSalt(WorkFactor);
        var hash = BCrypt.Net.BCrypt.HashPassword(password, salt);

        return new PasswordHash
        {
            Password = password,
            Salt = $"{CurrentVersion}:{WorkFactor}",
            Hash = hash
        };
    }

    public PasswordVerificationResult Verify(string password, string hash, string metadata)
    {
        if (IsBcryptHash(hash))
        {
            var isValid = BCrypt.Net.BCrypt.Verify(password, hash);
            var isCurrentVersion = string.Equals(GetVersion(metadata), CurrentVersion, StringComparison.OrdinalIgnoreCase);
            var hasCurrentWorkFactor = !BCrypt.Net.BCrypt.PasswordNeedsRehash(hash, WorkFactor);

            return new PasswordVerificationResult
            {
                IsValid = isValid,
                ShouldUpgrade = isValid && (!isCurrentVersion || !hasCurrentWorkFactor)
            };
        }

        // Legacy salted SHA-256 support for smooth migration of existing teams.
        var saltedPassword = $"{metadata}{password}";
        var legacyHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(saltedPassword)));
        var isLegacyMatch = legacyHash == hash;

        return new PasswordVerificationResult
        {
            IsValid = isLegacyMatch,
            ShouldUpgrade = isLegacyMatch
        };
    }

    private static bool IsBcryptHash(string hash) =>
        hash.StartsWith("$2a$", StringComparison.Ordinal) ||
        hash.StartsWith("$2b$", StringComparison.Ordinal) ||
        hash.StartsWith("$2y$", StringComparison.Ordinal);

    private static string GetVersion(string metadata)
    {
        if (string.IsNullOrWhiteSpace(metadata))
        {
            return string.Empty;
        }

        return metadata.Split(':', StringSplitOptions.RemoveEmptyEntries)[0];
    }
}