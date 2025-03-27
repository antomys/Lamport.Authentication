namespace Atomic.Swap;

/// <summary>
/// Represents a transaction on the blockchain
/// </summary>
public sealed class Transaction(string from, string to, decimal amount)
{
    public string Id { get; set; }
    
    public string From { get; set; } = from;

    public string To { get; set; } = to;

    public decimal Amount { get; set; } = amount;

    public Dictionary<string, object> Conditions { get; init; } = new Dictionary<string, object>();
    
    public bool IsSpent { get; set; } = false;
    
    public DateTime Timestamp { get; init; } = DateTime.Now;
    
    public DateTime? TimeLock { get; set; }

    // Helper to clone a transaction
    public Transaction Clone()
    {
        return new Transaction(From, To, Amount)
        {
            Id = Id,
            Conditions = new Dictionary<string, object>(Conditions),
            IsSpent = IsSpent,
            Timestamp = Timestamp,
            TimeLock = TimeLock
        };
    }
}