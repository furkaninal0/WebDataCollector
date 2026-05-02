using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using WebDataCollector.Models;

namespace WebDataCollector.Services;

public class LicenseService
{
    private const string SecretSalt = "INALCODE-PRIVATE-SALT-2026";
    private const string LicenseStateFileName = "license.json";

    public LicenseInfo GetCurrentLicense()
    {
        var machineCode = GenerateMachineCode();
        var savedState = LoadSavedLicenseState();

        if (savedState is null)
        {
            var defaultFree = new LicenseInfo
            {
                IsValid = true,
                Tier = LicenseTier.Free,
                MachineCode = machineCode,
                MaxUrlLimit = 10,
                UsedUrlCount = 0,
                LicenseKey = string.Empty
            };

            SaveLicenseState(defaultFree);
            return defaultFree;
        }

        if (!string.Equals(savedState.MachineCode, machineCode, StringComparison.OrdinalIgnoreCase))
        {
            var resetLicense = new LicenseInfo
            {
                IsValid = true,
                Tier = LicenseTier.Free,
                MachineCode = machineCode,
                MaxUrlLimit = 10,
                UsedUrlCount = 0,
                LicenseKey = string.Empty
            };

            SaveLicenseState(resetLicense);
            return resetLicense;
        }

        if (!string.IsNullOrWhiteSpace(savedState.LicenseKey))
        {
            var validated = ValidateLicense(savedState.LicenseKey, machineCode);

            if (validated.IsValid)
            {
                validated.UsedUrlCount = savedState.UsedUrlCount;
                SaveLicenseState(validated);
                return validated;
            }
        }

        var freeLicense = new LicenseInfo
        {
            IsValid = true,
            Tier = LicenseTier.Free,
            MachineCode = machineCode,
            MaxUrlLimit = 10,
            UsedUrlCount = savedState.UsedUrlCount,
            LicenseKey = string.Empty
        };

        SaveLicenseState(freeLicense);
        return freeLicense;
    }

    public LicenseInfo ValidateLicense(string licenseKey, string? machineCode = null)
    {
        machineCode ??= GenerateMachineCode();

        var normalized = licenseKey.Trim().ToUpperInvariant();

        if (normalized.StartsWith("INAL-PRO-"))
        {
            var expected = GenerateLicenseKey(machineCode, LicenseTier.Pro);

            if (normalized == expected)
            {
                return new LicenseInfo
                {
                    IsValid = true,
                    LicenseKey = normalized,
                    Tier = LicenseTier.Pro,
                    MachineCode = machineCode,
                    MaxUrlLimit = int.MaxValue,
                    UsedUrlCount = 0
                };
            }
        }

        return new LicenseInfo
        {
            IsValid = false,
            LicenseKey = normalized,
            Tier = LicenseTier.Free,
            MachineCode = machineCode,
            MaxUrlLimit = 10,
            UsedUrlCount = 0
        };
    }

    public string GenerateLicenseKey(string machineCode, LicenseTier tier)
    {
        var tierText = tier == LicenseTier.Pro ? "PRO" : "FREE";
        var raw = $"{machineCode}|{tierText}|{SecretSalt}";
        var hash = ComputeSha256(raw);

        return $"INAL-{tierText}-{hash[..16]}";
    }

    public string GenerateMachineCode()
    {
        var raw = $"{Environment.MachineName}|{Environment.UserName}|{Environment.ProcessorCount}|{Environment.OSVersion.VersionString}";
        return ComputeSha256(raw)[..12];
    }

    public void SaveLicenseKey(string licenseKey)
    {
        var machineCode = GenerateMachineCode();
        var current = GetCurrentLicense();
        var validated = ValidateLicense(licenseKey, machineCode);

        if (!validated.IsValid)
            return;

        validated.UsedUrlCount = validated.Tier == LicenseTier.Pro
            ? 0
            : current.UsedUrlCount;

        SaveLicenseState(validated);
    }

    public void SaveLicenseInfo(LicenseInfo licenseInfo)
    {
        SaveLicenseState(licenseInfo);
    }

    private void SaveLicenseState(LicenseInfo licenseInfo)
    {
        var folder = GetAppFolder();
        Directory.CreateDirectory(folder);

        licenseInfo.MachineCode = GenerateMachineCode();

        if (licenseInfo.Tier == LicenseTier.Pro)
        {
            licenseInfo.IsValid = true;
            licenseInfo.MaxUrlLimit = int.MaxValue;
        }
        else
        {
            licenseInfo.IsValid = true;
            licenseInfo.Tier = LicenseTier.Free;
            licenseInfo.MaxUrlLimit = 10;

            if (licenseInfo.UsedUrlCount < 0)
                licenseInfo.UsedUrlCount = 0;
        }

        var path = Path.Combine(folder, LicenseStateFileName);

        var json = JsonSerializer.Serialize(licenseInfo, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(path, json);
    }

    private LicenseInfo? LoadSavedLicenseState()
    {
        var path = Path.Combine(GetAppFolder(), LicenseStateFileName);

        if (!File.Exists(path))
            return null;

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<LicenseInfo>(json);
        }
        catch
        {
            return null;
        }
    }

    private string GetAppFolder()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "INALCODE", "Developer");
    }

    private static string ComputeSha256(string input)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }
}