using System.Security.Cryptography;

namespace Atomic.Swap;

/// <summary>
/// Represents a party in the atomic swap (Alice or Bob)
/// </summary>
internal sealed class SwapParty(string name, string currency)
{
    public string Name { get; } = name;
    public string Currency { get; } = currency;

    /// <summary>
    /// Creates a Hashed Timelock Contract (HTLC) on the blockchain
    /// </summary>
    /// <param name="secretHash">The hash of the secret that unlocks the funds</param>
    /// <param name="recipientName">The name of the recipient</param>
    /// <param name="timelock">The timelock duration</param>
    /// <returns>Details of the created contract</returns>
    public ContractDetails CreateHtlc(byte[] secretHash, string recipientName, TimeSpan timelock)
    {
        // In a real implementation, this would interact with the blockchain
        // Here we just simulate the contract creation

        // Generate a random contract ID
        string contractId = Guid.NewGuid().ToString("N")[..16];
            
        return new ContractDetails(
            contractId,
            Currency,
            secretHash,
            recipientName,
            DateTime.UtcNow.Add(timelock)
        );
    }

    /// <summary>
    /// Claims funds from a contract using the secret
    /// </summary>
    /// <param name="contractDetails">The contract details</param>
    /// <param name="secret">The secret that unlocks the funds</param>
    public void ClaimFunds(ContractDetails contractDetails, byte[] secret)
    {
        // In a real implementation, this would interact with the blockchain
        // Here we just simulate the claim process

        // Verify that the hash of the provided secret matches the hash in the contract
        byte[] computedHash = SHA256.HashData(secret);

        // Check if the hashes match
        if (!ByteArraysEqual(computedHash, contractDetails.SecretHash))
        {
            throw new InvalidOperationException("The provided secret does not match the hash in the contract.");
        }

        // Check if the contract has expired
        if (DateTime.UtcNow > contractDetails.Expiration)
        {
            throw new InvalidOperationException("The contract has expired.");
        }

        // In a real implementation, we would submit a transaction to the blockchain
        // to claim the funds
    }

    /// <summary>
    /// Compares two byte arrays for equality
    /// </summary>
    private static bool ByteArraysEqual(byte[] a, byte[] b)
    {
        if (a.Length != b.Length)
        {
            return false;
        }

        for (int i = 0; i < a.Length; i++)
        {
            if (a[i] != b[i])
            {
                return false;
            }
        }

        return true;
    }
}