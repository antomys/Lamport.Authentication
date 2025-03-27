namespace Atomic.Swap;

/// <summary>
/// Represents a contract details for a Hashed Timelock Contract (HTLC)
/// </summary>
internal sealed class ContractDetails(
    string contractId,
    string currency,
    byte[] secretHash,
    string recipientName,
    DateTime expiration)
{
    public string ContractId { get; } = contractId;
    
    public string Currency { get; } = currency;
    
    public byte[] SecretHash { get; } = secretHash;
    
    public string RecipientName { get; } = recipientName;
    
    public DateTime Expiration { get; } = expiration;
}