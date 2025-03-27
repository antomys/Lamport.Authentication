namespace Atomic.Swap.Tests;

public sealed class AtomicSwapTests
{
    [Fact]
    public async Task SuccessfulSwap_ShouldTransferAssets()
    {
        // Arrange
        var btcBlockchain = new Blockchain("Bitcoin-Test");
        var altBlockchain = new Blockchain("Altcoin-Test");
            
        var alice = new Wallet("Alice", btcBalance: 10, altBalance: 0);
        var bob = new Wallet("Bob", btcBalance: 0, altBalance: 500);
            
        decimal btcAmount = 2.5m;
        decimal altAmount = 125m;
            
        var atomicSwap = new AtomicSwap(btcBlockchain, altBlockchain);
            
        // Act
        var result = await atomicSwap.PerformSwap(alice, bob, btcAmount, altAmount);
            
        // Assert
        Assert.True(result.Success);
        Assert.Equal("Swap completed successfully", result.Message);
            
        // Verify balances changed correctly
        Assert.Equal(7.5m, alice.BtcBalance); // 10 - 2.5
        Assert.Equal(125m, alice.AltBalance); // 0 + 125
        Assert.Equal(2.5m, bob.BtcBalance);   // 0 + 2.5
        Assert.Equal(375m, bob.AltBalance);   // 500 - 125
            
        // Verify transactions are in blockchain and marked as spent
        Assert.True(btcBlockchain.Transactions.ContainsKey(result.SwapStatus.TX1Id));
        Assert.True(btcBlockchain.Transactions[result.SwapStatus.TX1Id].IsSpent);
            
        Assert.True(altBlockchain.Transactions.ContainsKey(result.SwapStatus.TX3Id));
        Assert.True(altBlockchain.Transactions[result.SwapStatus.TX3Id].IsSpent);
            
        // Verify secret and hash
        Assert.NotNull(result.SwapStatus.SecretX);
        Assert.NotNull(result.SwapStatus.SecretXHash);
        Assert.Equal(result.SwapStatus.SecretXHash, HashingService.ComputeHash(result.SwapStatus.SecretX));
    }
        
    [Fact]
    public async Task InsufficientBTCBalance_ShouldFailSwap()
    {
        // Arrange
        var btcBlockchain = new Blockchain("Bitcoin-Test");
        var altBlockchain = new Blockchain("Altcoin-Test");
            
        var alice = new Wallet("Alice", btcBalance: 1, altBalance: 0);
        var bob = new Wallet("Bob", btcBalance: 0, altBalance: 500);
            
        decimal btcAmount = 2.5m; // More than Alice has
        decimal altAmount = 125m;
            
        var atomicSwap = new AtomicSwap(btcBlockchain, altBlockchain);
            
        // Act
        var result = await atomicSwap.PerformSwap(alice, bob, btcAmount, altAmount);
            
        // Assert
        Assert.False(result.Success);
        Assert.Equal("Alice doesn't have enough BTC", result.Message);
            
        // Verify balances didn't change
        Assert.Equal(1m, alice.BtcBalance);
        Assert.Equal(0m, alice.AltBalance);
        Assert.Equal(0m, bob.BtcBalance);
        Assert.Equal(500m, bob.AltBalance);
    }
        
    [Fact]
    public async Task InsufficientALTBalance_ShouldFailSwap()
    {
        // Arrange
        var btcBlockchain = new Blockchain("Bitcoin-Test");
        var altBlockchain = new Blockchain("Altcoin-Test");
            
        var alice = new Wallet("Alice", btcBalance: 10, altBalance: 0);
        var bob = new Wallet("Bob", btcBalance: 0, altBalance: 100);
            
        decimal btcAmount = 2.5m;
        decimal altAmount = 125m; // More than Bob has
            
        var atomicSwap = new AtomicSwap(btcBlockchain, altBlockchain);
            
        // Act
        var result = await atomicSwap.PerformSwap(alice, bob, btcAmount, altAmount);
            
        // Assert
        Assert.False(result.Success);
        Assert.Equal("Bob doesn't have enough ALT coins", result.Message);
            
        // Verify balances didn't change
        Assert.Equal(10m, alice.BtcBalance);
        Assert.Equal(0m, alice.AltBalance);
        Assert.Equal(0m, bob.BtcBalance);
        Assert.Equal(100m, bob.AltBalance);
    }
        
    [Fact]
    public void HashingService_ShouldGenerateConsistentHashes()
    {
        // Arrange
        string secret = "test-secret";
            
        // Act
        string hash1 = HashingService.ComputeHash(secret);
        string hash2 = HashingService.ComputeHash(secret);
        string hash3 = HashingService.ComputeHash("different-secret");
            
        // Assert
        Assert.Equal(hash1, hash2); // Same input should produce same hash
        Assert.NotEqual(hash1, hash3); // Different input should produce different hash
    }
        
    [Fact]
    public void HashingService_ShouldGenerateRandomSecrets()
    {
        // Act
        string secret1 = HashingService.GenerateRandomSecret();
        string secret2 = HashingService.GenerateRandomSecret();
            
        // Assert
        Assert.NotEqual(secret1, secret2); // Should generate different secrets
    }
        
    [Fact]
    public void Wallet_ShouldSignAndVerifyMessages()
    {
        // Arrange
        var alice = new Wallet("Alice");
        var bob = new Wallet("Bob");
        string message = "Test message";
            
        // Act
        string signature = alice.SignMessage(message);
            
        // Assert
        Assert.True(alice.VerifySignature(message, signature, alice.PublicKey));
        Assert.False(alice.VerifySignature("Different message", signature, alice.PublicKey));
        Assert.False(alice.VerifySignature(message, signature, bob.PublicKey));
    }
        
    [Fact]
    public void Transaction_ShouldCloneCorrectly()
    {
        // Arrange
        var tx = new Transaction("Alice", "Bob", 1.0m);
        tx.Conditions["TestKey"] = "TestValue";
        tx.TimeLock = DateTime.Now.AddHours(1);
            
        // Act
        var clonedTx = tx.Clone();
            
        // Assert
        Assert.Equal(tx.From, clonedTx.From);
        Assert.Equal(tx.To, clonedTx.To);
        Assert.Equal(tx.Amount, clonedTx.Amount);
        Assert.Equal(tx.TimeLock, clonedTx.TimeLock);
            
        // Change in original shouldn't affect clone
        tx.Conditions["TestKey"] = "NewValue";
        Assert.Equal("TestValue", clonedTx.Conditions["TestKey"]);
    }
}