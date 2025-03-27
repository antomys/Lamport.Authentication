using System.Security.Cryptography;
using System.Text;

namespace Atomic.Swap;

/// <summary>
/// Handles the hashing of secret values
/// </summary>
public static class HashingService
{
    public static string GenerateRandomSecret()
    {
        byte[] randomBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        return Convert.ToBase64String(randomBytes);
    }

    public static string ComputeHash(string input)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));

        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < bytes.Length; i++)
        {
            builder.Append(bytes[i].ToString("x2"));
        }
        return builder.ToString();
    }
}