using WebDataCollector.LicenseGenerator;

var service = new LicenseGeneratorService();

Console.WriteLine("=== INALCODE License Generator ===");
Console.WriteLine();

while (true)
{
    Console.Write("Machine Code gir: ");
    var machineCode = Console.ReadLine()?.Trim();

    if (string.IsNullOrWhiteSpace(machineCode))
    {
        Console.WriteLine("Machine Code boş olamaz.");
        Console.WriteLine();
        continue;
    }

    Console.WriteLine();
    Console.WriteLine("Lisans tipi seç:");
    Console.WriteLine("1 - Free");
    Console.WriteLine("2 - Pro");
    Console.Write("Seçim: ");
    var choice = Console.ReadLine()?.Trim();

    LicenseTier tier;

    switch (choice)
    {
        case "1":
            tier = LicenseTier.Free;
            break;
        case "2":
            tier = LicenseTier.Pro;
            break;
        default:
            Console.WriteLine("Geçersiz seçim.");
            Console.WriteLine();
            continue;
    }

    try
    {
        var key = service.GenerateLicenseKey(machineCode, tier);

        Console.WriteLine();
        Console.WriteLine("Oluşturulan lisans anahtarı:");
        Console.WriteLine(key);
        Console.WriteLine();

        Console.Write("Yeni key üretmek ister misin? (E/H): ");
        var again = Console.ReadLine()?.Trim().ToUpperInvariant();

        if (again != "E")
            break;

        Console.WriteLine();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Hata: {ex.Message}");
        Console.WriteLine();
    }
}