using System.Security.Cryptography;
using System.Text;
using Spectre.Console;

namespace Lamport.Authentication.Client;

public sealed class LamportAuthenticator
{
    // The current hash stored on the server.
    // This represents xn = Hⁿ(secret), where H is the hash function.
    private byte[] _currentHash;

    /*
     * Constructor: Generates the hash chain.
     * Formula:
     *   Let secret = S and H(x) = SHA256(x)
     *   x0 = S
     *   xi = H(xi-1) for i = 1, 2, …, n
     * The server stores xn (i.e., H applied n times to the secret).
     */
    public LamportAuthenticator(string secret, long iterations)
    {
        // Display the formula and steps using Spectre.Console markup.
        AnsiConsole.MarkupLine("[bold green]* Formula:*[/] " +
                               "[yellow]Let secret = S and H(x) = SHA256(x)[/]");
        AnsiConsole.MarkupLine("[bold green]* Calculation:*[/] " +
                               "[yellow]x0 = S, xi = H(xi-1) for i = 1, 2, …, n[/]");
        AnsiConsole.MarkupLine("[bold green]* Note:*[/] " +
                               "[yellow]The server stores xn (i.e., H applied n times to the secret).[/]");

        // Start with the secret as the initial value (x0).
        byte[] hash = Encoding.UTF8.GetBytes(secret);

        // Compute xn by applying H iteratively n times.
        for (int i = 0; i < iterations; i++)
        {
            hash = SHA256.HashData(hash);
        }
        // currentHash = xn
        _currentHash = hash;
    }

    // Returns the stored hash in hexadecimal form.
    // This value (xn) is registered on the server.
    public string GetCurrentHash()
    {
        return ByteArrayToHexString(_currentHash);
    }

    /*
     * VerifyOtp: Validates the one-time password (OTP) provided by the client.
     *
     * Algorithm steps:
     * 1. The client sends xn-1 (one-time password).
     * 2. The server computes H(xn-1) which should equal the stored xn.
     * 3. If H(xn-1) == xn, then update currentHash to xn-1.
     *
     * Formula:
     *   Provided OTP = xn-1, and the server checks:
     *     H(xn-1) ?= xn
     * If the equality holds, the OTP is valid.
     */
    public bool VerifyOtp(string providedOtp)
    {
        // Display the algorithm steps using Spectre.Console.
        AnsiConsole.MarkupLine("[bold blue]* Algorithm steps:*[/]");
        AnsiConsole.MarkupLine("[blue]1.[/] The client sends [italic]xn-1[/] (one-time password).");
        AnsiConsole.MarkupLine("[blue]2.[/] The server computes [italic]H(xn-1)[/], which should equal the stored [italic]xn[/].");
        AnsiConsole.MarkupLine("[blue]3.[/] If [italic]H(xn-1) == xn[/], update current hash to [italic]xn-1[/].");

        // Convert the provided hexadecimal OTP into a byte array.
        byte[] otpBytes = HexStringToByteArray(providedOtp);
        AnsiConsole.MarkupLine($"[green]* Converting provided OTP to byte array:*[/] [yellow]{ByteArrayToHexString(otpBytes)}[/]");

        // Compute H(providedOTP) i.e., H(xn-1)
        byte[] computedHash = SHA256.HashData(otpBytes);
        AnsiConsole.MarkupLine($"[green]* Computed H(providedOTP):*[/] [yellow]{ByteArrayToHexString(computedHash)}[/]");

        // Check if H(xn-1) equals the stored xn.
        AnsiConsole.MarkupLine("[green]* Verifying:*[/] Checking if computed hash equals the stored hash.");
        if (CompareByteArrays(computedHash, _currentHash))
        {
            // Update stored hash for next authentication round:
            // Now, _currentHash becomes xn-1.
            _currentHash = otpBytes;
            AnsiConsole.MarkupLine("[green]* Verification successful:*[/] OTP is valid.");
            return true;
        }
        AnsiConsole.MarkupLine("[red]* Verification failed:*[/] OTP does not match.");
        return false;
    }

    // Helper method: Compare two byte arrays for equality.
    private static bool CompareByteArrays(byte[] a, byte[] b)
    {
        if (a.Length != b.Length)
        {
            return false;
        }
        for (int i = 0; i < a.Length; i++)
        {
            if (a[i] != b[i])
                return false;
        }
        return true;
    }

    // Helper method: Convert a byte array to a hexadecimal string.
    private static string ByteArrayToHexString(Span<byte> bytes)
    {
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (byte b in bytes)
        {
            sb.Append(b.ToString("x2"));
        }
        return sb.ToString();
    }

    // Overload to accept a byte[] directly.
    private static string ByteArrayToHexString(byte[] bytes)
    {
        return ByteArrayToHexString(bytes.AsSpan());
    }

    // Helper method: Convert a hexadecimal string back to a byte array.
    private static byte[] HexStringToByteArray(string hex)
    {
        int numberChars = hex.Length;
        byte[] bytes = new byte[numberChars / 2];
        var bytesSpan = bytes.AsSpan();
        for (int i = 0; i < numberChars; i += 2)
        {
            bytesSpan[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
        }
        return bytes;
    }
}