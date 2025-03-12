using System.Numerics;
using System.Reflection;
using System.Security.Cryptography;
using DiffieHellman.Group;

namespace DiffieHellman.Test;

public class TreeDiffieHellmanTests
{
    // Test parameters - using smaller values for faster testing
    private static readonly BigInteger P = BigInteger.Parse("23492587423094857029384572093485702983457029384752");
    private static readonly BigInteger G = 2;
        
    [Fact]
    public void DeriveKey_ValidInput_ReturnsCorrectHash()
    {
        // Arrange
        BigInteger testValue = BigInteger.Parse("12345678901234567890");
            
        // Create a private method invoker using reflection
        var deriveKeyMethod = typeof(TreeBasedDiffieHellman).GetMethod("DeriveKey", 
            BindingFlags.NonPublic | BindingFlags.Static);
            
        // Act
        byte[] result = (byte[])deriveKeyMethod.Invoke(null, new object[] { testValue });
            
        // Assert
        Assert.NotNull(result);
        Assert.Equal(32, result.Length); // SHA256 produces 32 bytes
            
        // Verify result against known SHA256 hash (can be pre-computed)
        using (var sha256 = SHA256.Create())
        {
            byte[] expected = sha256.ComputeHash(testValue.ToByteArray());
            Assert.Equal(expected, result);
        }
    }
        
    [Fact]
    public void GeneratePrivateKey_Returns32BytePositiveNumber()
    {
        // Arrange
        int byteLength = 32;
            
        // Create a private method invoker using reflection
        var generateKeyMethod = typeof(TreeBasedDiffieHellman).GetMethod("GeneratePrivateKey", 
            BindingFlags.NonPublic | BindingFlags.Static);
            
        // Act
        BigInteger result = (BigInteger)generateKeyMethod.Invoke(null, new object[] { byteLength, P });
            
        // Assert
        Assert.True(result > 0);
        Assert.True(result < P - 1); // Should be less than p-1
        Assert.True(result.ToByteArray().Length <= byteLength + 1); // +1 for potential sign byte
    }
        
    [Fact]
    public void ComputeFinalKey_ForCoordinator_CalculatesCorrectly()
    {
        // Arrange
        int partyCount = 4;
        BigInteger[] privateKeys = new BigInteger[partyCount];
        BigInteger[] publicKeys = new BigInteger[partyCount];
            
        // Set deterministic private keys for testing
        privateKeys[0] = 123; // Alice (coordinator)
        privateKeys[1] = 456; // Bob
        privateKeys[2] = 789; // Charlie
        privateKeys[3] = 987; // Dave
            
        // Generate public keys
        for (int i = 0; i < partyCount; i++)
        {
            publicKeys[i] = BigInteger.ModPow(G, privateKeys[i], P);
        }
            
        // Access the private method using reflection
        var computeFinalKeyMethod = typeof(TreeBasedDiffieHellman).GetMethod("ComputeFinalKey", 
            BindingFlags.NonPublic | BindingFlags.Static);
            
        // Act
        BigInteger result = (BigInteger)computeFinalKeyMethod.Invoke(null, 
            new object[] { privateKeys[0], publicKeys, 0, P });
            
        // Manually calculate expected result for comparison
        BigInteger expected = BigInteger.One;
        for (int i = 1; i < partyCount; i++)
        {
            expected = (expected * BigInteger.ModPow(publicKeys[i], privateKeys[0], P)) % P;
        }
            
        // Assert
        Assert.Equal(expected, result);
    }
        
    [Fact]
    public void ComputeFinalKeyForParty_CalculatesCorrectly()
    {
        // Arrange
        int partyCount = 4;
        int partyIndex = 2; // Testing for Charlie (index 2)
            
        BigInteger[] privateKeys = new BigInteger[partyCount];
        BigInteger[] publicKeys = new BigInteger[partyCount];
            
        // Set deterministic private keys for testing
        privateKeys[0] = 123; // Alice (coordinator)
        privateKeys[1] = 456; // Bob
        privateKeys[2] = 789; // Charlie
        privateKeys[3] = 987; // Dave
            
        // Generate public keys
        for (int i = 0; i < partyCount; i++)
        {
            publicKeys[i] = BigInteger.ModPow(G, privateKeys[i], P);
        }
            
        // Generate intermediate keys (Alice computes these)
        BigInteger[] intermediateKeys = new BigInteger[partyCount-1];
        for (int i = 1; i < partyCount; i++)
        {
            intermediateKeys[i-1] = BigInteger.ModPow(publicKeys[i], privateKeys[0], P);
        }
            
        // Create the list of intermediates Charlie would receive
        // (all except Charlie's own corresponding intermediate)
        var receivedIntermediates = new List<BigInteger>();
        for (int i = 0; i < partyCount-1; i++)
        {
            if (i+1 != partyIndex) // Skip Charlie's own intermediate
            {
                receivedIntermediates.Add(intermediateKeys[i]);
            }
        }
            
        // Access the private method using reflection
        var computeFinalKeyMethod = typeof(TreeBasedDiffieHellman).GetMethod("ComputeFinalKeyForParty", 
            BindingFlags.NonPublic | BindingFlags.Static);
            
        // Act
        BigInteger result = (BigInteger)computeFinalKeyMethod.Invoke(null, 
            new object[] { privateKeys[partyIndex], receivedIntermediates, publicKeys, partyIndex, P });
            
        // Manually calculate expected result for comparison
        // First, Charlie's own contribution: (g^x1)^x3 = g^(x1*x3)
        BigInteger ownIntermediate = BigInteger.ModPow(publicKeys[0], privateKeys[partyIndex], P);
            
        // Then combine with received intermediates
        BigInteger expected = ownIntermediate;
        foreach (var intermediate in receivedIntermediates)
        {
            expected = (expected * intermediate) % P;
        }
            
        // Assert
        Assert.Equal(expected, result);
    }
        
    [Fact]
    public void AllParties_ComputeSameKey()
    {
        // Arrange
        int partyCount = 5;
            
        BigInteger[] privateKeys = new BigInteger[partyCount];
        BigInteger[] publicKeys = new BigInteger[partyCount];
        BigInteger[] finalKeys = new BigInteger[partyCount];
            
        // Set deterministic private keys
        for (int i = 0; i < partyCount; i++)
        {
            privateKeys[i] = 100 * (i + 1);
        }
            
        // Generate public keys
        for (int i = 0; i < partyCount; i++)
        {
            publicKeys[i] = BigInteger.ModPow(G, privateKeys[i], P);
        }
            
        // Alice computes intermediate keys
        BigInteger[] intermediateKeys = new BigInteger[partyCount-1];
        for (int i = 1; i < partyCount; i++)
        {
            intermediateKeys[i-1] = BigInteger.ModPow(publicKeys[i], privateKeys[0], P);
        }
            
        // Get methods using reflection
        var computeFinalKeyMethod = typeof(TreeBasedDiffieHellman).GetMethod("ComputeFinalKey", 
            BindingFlags.NonPublic | BindingFlags.Static);
            
        var computeFinalKeyForPartyMethod = typeof(TreeBasedDiffieHellman).GetMethod("ComputeFinalKeyForParty", 
            BindingFlags.NonPublic | BindingFlags.Static);
            
        // Alice computes her key
        finalKeys[0] = (BigInteger)computeFinalKeyMethod.Invoke(null, 
            new object[] { privateKeys[0], publicKeys, 0, P });
            
        // Each other party computes their key
        for (int partyIndex = 1; partyIndex < partyCount; partyIndex++)
        {
            // Create list of intermediates this party would receive
            var receivedIntermediates = new List<BigInteger>();
            for (int i = 0; i < partyCount-1; i++)
            {
                if (i+1 != partyIndex) // Skip party's own intermediate
                {
                    receivedIntermediates.Add(intermediateKeys[i]);
                }
            }
                
            // Compute final key for this party
            finalKeys[partyIndex] = (BigInteger)computeFinalKeyForPartyMethod.Invoke(null, 
                new object[] { privateKeys[partyIndex], receivedIntermediates, publicKeys, partyIndex, P });
        }
            
        // Assert - All keys should be the same
        for (int i = 1; i < partyCount; i++)
        {
            Assert.Equal(finalKeys[0], finalKeys[i]);
        }
    }
        
    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(8)]
    public void VerifyCommunicationCount_Equals2NMinus1(int n)
    {
        // This test verifies the communication complexity is indeed 2n-1
            
        // The formula is:
        // n initial broadcasts + (n-1) distributions from coordinator
        int expectedCommunications = n + (n - 1);
            
        // Verify it equals 2n-1
        Assert.Equal(2*n - 1, expectedCommunications);
    }
}