using System.Security.Cryptography;
using System.Text.Json;

namespace HomeMcp.Server.Admin;

public sealed class SetupState
{
    private readonly string _filePath;
    private readonly TimeProvider _timeProvider;
    private SetupData _data;

    // In-memory only: single outstanding pairing PIN. Wiped after use or expiry.
    private string? _pairingPin;
    private DateTimeOffset _pairingPinExpiry;

    public SetupState(IConfiguration configuration, TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
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
        _data = new SetupData(true, serverName, defaultLocale, CryptoHelper.Hash(password));
        Save();
    }

    public bool VerifyPassword(string password)
    {
        if (string.IsNullOrEmpty(_data.AdminPasswordHash))
        {
            return false;
        }

        return CryptoHelper.Verify(password, _data.AdminPasswordHash);
    }

    // Generates a 6-character alphanumeric PIN, valid for 10 minutes.
    // Replaces any existing outstanding PIN.
    public string GeneratePairingPin()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        Span<byte> buf = stackalloc byte[6];
        RandomNumberGenerator.Fill(buf);
        _pairingPin = new string([.. buf.ToArray().Select(b => chars[b % chars.Length])]);
        _pairingPinExpiry = _timeProvider.GetUtcNow().AddMinutes(10);
        return _pairingPin;
    }

    // Returns true and clears the PIN if it matches and hasn't expired.
    public bool VerifyAndConsumePairingPin(string? pin)
    {
        if (string.IsNullOrWhiteSpace(pin) || _pairingPin is null)
        {
            return false;
        }

        if (_timeProvider.GetUtcNow() > _pairingPinExpiry)
        {
            _pairingPin = null;
            return false;
        }

        var valid = string.Equals(_pairingPin, pin.Trim().ToUpperInvariant(), StringComparison.Ordinal);
        if (valid)
        {
            _pairingPin = null;
        }

        return valid;
    }

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
