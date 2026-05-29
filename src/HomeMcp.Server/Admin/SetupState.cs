using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace HomeMcp.Server.Admin;

public sealed class SetupState
{
    private readonly string _filePath;
    private SetupData _data;

    public SetupState(IConfiguration configuration)
    {
        var dataDir = Path.GetDirectoryName(
            Path.GetFullPath(configuration["Storage:Sqlite:Path"] ?? "./data/home-mcp.db"))!;
        _filePath = Path.Combine(dataDir, "setup.json");
        _data = Load();
    }

    public bool IsComplete => _data.Complete;
    public string ServerName => _data.ServerName;
    public string DefaultLocale => _data.DefaultLocale;

    public void Complete(string serverName, string defaultLocale, string password)
    {
        _data = new SetupData(true, serverName, defaultLocale, HashPassword(password));
        Save();
    }

    public bool VerifyPassword(string password)
    {
        if (string.IsNullOrEmpty(_data.AdminPasswordHash))
        {
            return false;
        }

        // Format: <base64-salt>:<base64-hash>
        var parts = _data.AdminPasswordHash.Split(':');
        if (parts.Length != 2)
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[0]);
        var expected = Convert.FromBase64String(parts[1]);
        var actual = Pbkdf2(password, salt);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Pbkdf2(password, salt);
        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    private static byte[] Pbkdf2(string password, byte[] salt) =>
        Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            iterations: 200_000,
            HashAlgorithmName.SHA256,
            outputLength: 32);

    private SetupData Load()
    {
        if (!File.Exists(_filePath))
        {
            return new SetupData(false, "Home MCP", "en-US", string.Empty);
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<SetupData>(json)
                   ?? new SetupData(false, "Home MCP", "en-US", string.Empty);
        }
        catch
        {
            return new SetupData(false, "Home MCP", "en-US", string.Empty);
        }
    }

    private void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        File.WriteAllText(_filePath, JsonSerializer.Serialize(_data));
    }

    private sealed record SetupData(
        bool Complete,
        string ServerName,
        string DefaultLocale,
        string AdminPasswordHash);
}
