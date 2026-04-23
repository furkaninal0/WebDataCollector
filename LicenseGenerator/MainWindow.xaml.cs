using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace LicenseGenerator;

public partial class MainWindow : Window
{
    private const string SecretSalt = "INALCODE-PRIVATE-SALT-2026";

    public MainWindow()
    {
        InitializeComponent();
    }

    private void BtnGenerate_Click(object sender, RoutedEventArgs e)
    {
        string machineCode = TxtMachineCode.Text.Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(machineCode))
        {
            MessageBox.Show("Machine Code boş olamaz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (CmbTier.SelectedItem is not ComboBoxItem selectedItem)
        {
            MessageBox.Show("Lisans türü seçmelisiniz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string tier = selectedItem.Content?.ToString() ?? "FREE";
        string key = GenerateKey(machineCode, tier);

        TxtResult.Text = key;
    }

    private void BtnCopy_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtResult.Text))
        {
            MessageBox.Show("Önce bir lisans anahtarı oluşturun.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        Clipboard.SetText(TxtResult.Text);
        MessageBox.Show("Lisans anahtarı kopyalandı.", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private string GenerateKey(string machineCode, string tier)
    {
        string raw = $"{machineCode}|{tier}|{SecretSalt}";
        string hash = ComputeSha256(raw);
        return $"INAL-{tier}-{hash[..16]}";
    }

    private string ComputeSha256(string input)
    {
        using SHA256 sha = SHA256.Create();
        byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }
}