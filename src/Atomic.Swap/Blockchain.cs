namespace Atomic.Swap;

/// <summary>
/// Simulates a blockchain with basic transaction capabilities
/// </summary>
public sealed class Blockchain(string name)
{
    public string Name { get; private set; } = name;
    public Dictionary<string, Transaction> Transactions { get; private set; } = new();

    public string AddTransaction(Transaction transaction)
    {
        // Generate a simple transaction ID
        string txId = Guid.NewGuid().ToString()[..8];
        transaction.Id = txId;
        Transactions[txId] = transaction;
            
        return txId;
    }

    public override string ToString()
    {
        return Name;
    }
}