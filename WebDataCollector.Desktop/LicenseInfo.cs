namespace WebDataCollector.Models;

public class LicenseInfo
{
    public bool IsValid { get; set; }
    public string LicenseKey { get; set; } = string.Empty;
    public LicenseTier Tier { get; set; } = LicenseTier.Free;
    public string MachineCode { get; set; } = string.Empty;
    public int MaxUrlLimit { get; set; } = 20;
    public int UsedUrlCount { get; set; } = 0;

    public int RemainingUrlCount => Tier == LicenseTier.Pro
        ? int.MaxValue
        : Math.Max(0, MaxUrlLimit - UsedUrlCount);

}