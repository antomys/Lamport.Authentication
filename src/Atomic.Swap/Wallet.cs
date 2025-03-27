namespace Atomic.Swap;

/// <summary>
/// Represents a wallet with cryptographic keys
/// </summary>
public sealed class Wallet(string name, decimal btcBalance = 0, decimal altBalance = 0)
{
    public string Name { get; private set; } = name;

    public string PublicKey { get; private set; } = $"{name}-public-key";

    private string PrivateKey { get; set; } = $"{name}-private-key";

    public decimal BtcBalance { get; set; } = btcBalance;

    public decimal AltBalance { get; set; } = altBalance;

    // Generate a dummy key pair for simulation

    // Simulate signing a message with the private key
    public string SignMessage(string message)
    {
        return $"Signed({message})-by-{Name}";
    }

    // Verify a signature (simplified for simulation)
    public bool VerifySignature(string originalMessage, string signature, string publicKey)
    {
        return signature == $"Signed({originalMessage})-by-{publicKey.Split('-')[0]}";
    }
}