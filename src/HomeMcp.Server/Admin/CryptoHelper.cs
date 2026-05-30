using System.Security.Cryptography;
using System.Text;

namespace HomeMcp.Server.Admin;

internal static class CryptoHelper
{
    private const int SaltBytes = 16;
    private const int HashBytes = 32;
    private const int Iterations = 200_000;

    public static string Hash(string plaintext)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltBytes);
        var hash = Pbkdf2(plaintext, salt);
        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string plaintext, string storedHash)
    {
        var parts = storedHash.Split(':');
        if (parts.Length != 2)
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(parts[0]);
            var expected = Convert.FromBase64String(parts[1]);
            var actual = Pbkdf2(plaintext, salt);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public static string GenerateToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    private static byte[] Pbkdf2(string plaintext, byte[] salt) =>
        Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(plaintext),
            salt,
            iterations: Iterations,
            HashAlgorithmName.SHA256,
            outputLength: HashBytes);
}
