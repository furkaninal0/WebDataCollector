using System.Security.Cryptography;
using System.Text;

namespace WebDataCollector.LicenseGenerator;

public class LicenseGeneratorService
{
    private const string SecretSalt = "INALCODE-PRIVATE-SALT-2026";

    public string GenerateLicenseKey(string machineCode, LicenseTier tier)
    {
        if (string.IsNullOrWhiteSpace(machineCode))
            throw new ArgumentException("Machine code boş olamaz.");

        machineCode = machineCode.Trim().ToUpperInvariant();

        var tierText = tier == LicenseTier.Pro ? "PRO" : "FREE";
        var raw = $"{machineCode}|{tierText}|{SecretSalt}";
        var hash = ComputeSha256(raw);

        return $"INAL-{tierText}-{hash[..16]}";
    }

    private static string ComputeSha256(string input)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }
}