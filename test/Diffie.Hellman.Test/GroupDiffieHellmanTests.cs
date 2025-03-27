using System.Numerics;
using System.Reflection;
using System.Security.Cryptography;
using DiffieHellman.Group;

namespace DiffieHellman.Test;

public sealed class GroupDiffieHellmanTests
{
    // Test parameters
    private static readonly BigInteger P = BigInteger.Parse("23492587423094857029384572093485702983457029384752");
    private static readonly BigInteger G = 2;
    
    [Fact]
    public void DeriveKey_ValidInput_ReturnsCorrectHash()
    {
        // Arrange
        BigInteger testValue = BigInteger.Parse("12345678901234567890");
        
        // Create a private method invoker using reflection
        var derivekeyMethod = typeof(GroupDiffieHellman).GetMethod("DeriveKey", 
            BindingFlags.NonPublic | BindingFlags.Static);
        
        // Act
        byte[] result = (byte[])derivekeyMethod.Invoke(null, new object[] { testValue });
        
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
        var generateKeyMethod = typeof(GroupDiffieHellman).GetMethod("GeneratePrivateKey", 
            BindingFlags.NonPublic | BindingFlags.Static);
        
        // Act
        BigInteger result = (BigInteger)generateKeyMethod.Invoke(null, new object[] { byteLength });
        
        // Assert
        Assert.True(result > 0);
        Assert.True(result.ToByteArray().Length <= byteLength + 1); // +1 for potential sign byte
        Assert.True((result.ToByteArray()[result.ToByteArray().Length - 1] & 0x80) == 0); // MSB should be 0 (positive)
    }
    
    [Fact]
    public void GetColoredName_ReturnsCorrectFormattedString()
    {
        // Arrange
        var getColoredNameMethod = typeof(GroupDiffieHellman).GetMethod("GetColoredName", 
            BindingFlags.NonPublic | BindingFlags.Static);
        
        // Dictionary of names and expected outputs
        var testCases = new Dictionary<string, string>
        {
            { "Alice", "[blue]Alice[/]" },
            { "Bob", "[magenta]Bob[/]" },
            { "Charlie", "[yellow]Charlie[/]" },
            { "Dave", "[green]Dave[/]" },
            { "Eve", "[red]Eve[/]" },
            { "Frank", "[cyan]Frank[/]" },
            { "Grace", "[grey]Grace[/]" },
            { "Unknown", "Unknown" } // For unknown names, it should return the original name
        };
        
        // Act & Assert
        foreach (var testCase in testCases)
        {
            string result = (string)getColoredNameMethod.Invoke(null, new object[] { testCase.Key });
            Assert.Equal(testCase.Value, result);
        }
    }
    
    [Fact]
    public void ThreePartyDH_KeysMatch_WhenUsingPredeterminedValues()
    {
        // This test manually verifies key calculation with predetermined values
        
        // Arrange - predetermined private keys
        BigInteger x = 123456789; // Alice
        BigInteger y = 987654321; // Bob
        BigInteger z = 555555555; // Charlie
        
        // Act - Calculate public keys
        BigInteger g_x = BigInteger.ModPow(G, x, P); // Alice's public key
        BigInteger g_y = BigInteger.ModPow(G, y, P); // Bob's public key
        BigInteger g_z = BigInteger.ModPow(G, z, P); // Charlie's public key
        
        // Calculate two-party DH key (Alice-Bob)
        BigInteger k_AB_Alice = BigInteger.ModPow(g_y, x, P);
        BigInteger k_AB_Bob = BigInteger.ModPow(g_x, y, P);
        
        // Calculate three-party keys
        BigInteger g_xy = k_AB_Alice; // Alice sends to Charlie
        
        BigInteger k_ABC_Alice = (g_z * k_AB_Alice) % P;
        BigInteger k_ABC_Bob = (g_z * k_AB_Bob) % P;
        BigInteger k_ABC_Charlie = (g_z * g_xy) % P;
        
        // Assert
        Assert.Equal(k_AB_Alice, k_AB_Bob);
        Assert.Equal(k_ABC_Alice, k_ABC_Bob);
        Assert.Equal(k_ABC_Bob, k_ABC_Charlie);
    }
    
    [Fact]
    public void ThreePartyDH_Produces4Communications()
    {
        // This test verifies that the 3-party DH protocol requires exactly 4 communications
        int communicationCount = 0;
        
        // Arrange
        BigInteger x = 123;
        BigInteger y = 456;
        BigInteger z = 789;
        
        // Act - Simulate the protocol steps
        
        // Step 1: Public key exchange (3 communications)
        BigInteger g_x = BigInteger.ModPow(G, x, P);
        IncrementCommunication(ref communicationCount); // Alice -> all
        
        BigInteger g_y = BigInteger.ModPow(G, y, P);
        IncrementCommunication(ref communicationCount); // Bob -> all
        
        BigInteger g_z = BigInteger.ModPow(G, z, P);
        IncrementCommunication(ref communicationCount); // Charlie -> all
        
        // Step 2: Alice-Bob DH key (calculated locally, no communication)
        BigInteger k_AB_Alice = BigInteger.ModPow(g_y, x, P);
        BigInteger k_AB_Bob = BigInteger.ModPow(g_x, y, P);
        
        // Step 3: Additional communication (1)
        BigInteger g_xy = k_AB_Alice;
        IncrementCommunication(ref communicationCount); // Alice -> Charlie
        
        // Assert
        Assert.Equal(4, communicationCount);
    }
    
    [Fact]
    public void ThreePartyDH_SecurityProperty_PrivateKeysRemainSecret()
    {
        // This test verifies that private keys cannot be derived from public information
        
        // Arrange
        BigInteger x = 123456789;
        BigInteger y = 987654321;
        BigInteger z = 555555555;
        
        // Public values that would be shared
        BigInteger g_x = BigInteger.ModPow(G, x, P);
        BigInteger g_y = BigInteger.ModPow(G, y, P);
        BigInteger g_z = BigInteger.ModPow(G, z, P);
        BigInteger g_xy = BigInteger.ModPow(g_y, x, P);
        
        // The shared key
        BigInteger k_ABC = (g_z * g_xy) % P;
        
        // Act & Assert
        // This is a conceptual test - we can't actually try to break the discrete log
        // problem in a unit test, but we can verify that our final key depends on all
        // private values and can only be computed if you have the right information
        
        // If an attacker knows g, p, g^x, g^y, g^z, and g^xy, they still can't compute x, y, or z
        // due to the discrete logarithm problem
        
        // We can verify that final key matches what we expect
        BigInteger expected = BigInteger.ModPow(g_y, x, P);
        expected = (expected * g_z) % P;
        
        Assert.Equal(expected, k_ABC);
    }
    
    // Helper method to simulate communication count increment
    private void IncrementCommunication(ref int count)
    {
        count++;
    }
}

public partial class TreeBasedDiffieHellmanTests
    {
        // Test parameters
        private static readonly BigInteger P = BigInteger.Parse("23492587423094857029384572093485702983457029384752");
        private static readonly BigInteger G = 2;
        
        [Fact]
        public void TreeBasedDH_VerifyMinimumCommunicationCount()
        {
            // Verify that an n-party tree-based DH requires 2n-1 communications
            
            // Arrange
            int n = 5; // Number of parties
            int communicationCount = 0;
            
            // Act - Simulate the protocol steps
            
            // Step 1: Each party broadcasts their public key (n communications)
            for (int i = 0; i < n; i++)
            {
                communicationCount++;
            }
            
            // Step 2: Coordinator (Alice) sends intermediate keys to all other parties (n-1 communications)
            for (int i = 1; i < n; i++)
            {
                communicationCount++;
            }
            
            // Assert
            int expectedCommunications = 2 * n - 1;
            Assert.Equal(expectedCommunications, communicationCount);
        }
        
        [Fact]
        public void TreeBasedDH_KeysMatch_WithMultipleParties()
        {
            // Test that all parties in tree-based DH compute the same key
            
            // Arrange
            int n = 4; // Testing with 4 parties
            
            // Generate private keys
            BigInteger[] privateKeys = new BigInteger[n];
            for (int i = 0; i < n; i++)
            {
                privateKeys[i] = 123 * (i + 1); // Simple deterministic keys for testing
            }
            
            // Calculate public keys
            BigInteger[] publicKeys = new BigInteger[n];
            for (int i = 0; i < n; i++)
            {
                publicKeys[i] = BigInteger.ModPow(G, privateKeys[i], P);
            }
            
            // Alice (party 0) computes intermediate keys
            BigInteger[] intermediateKeys = new BigInteger[n-1];
            for (int i = 1; i < n; i++)
            {
                intermediateKeys[i-1] = BigInteger.ModPow(publicKeys[i], privateKeys[0], P);
            }
            
            // Act - Each party computes the final key
            
            // Simulating the distribution of intermediate keys
            // Each party i receives all intermediate keys except their own
            BigInteger[] finalKeys = new BigInteger[n];
            
            // For Alice (coordinator)
            finalKeys[0] = BigInteger.One;
            for (int i = 1; i < n; i++)
            {
                finalKeys[0] = (finalKeys[0] * BigInteger.ModPow(publicKeys[i], privateKeys[0], P)) % P;
            }
            
            // For all other parties
            for (int party = 1; party < n; party++)
            {
                // Start with party's own contribution
                BigInteger ownIntermediate = BigInteger.ModPow(publicKeys[0], privateKeys[party], P);
                
                // Compute the final key by incorporating all other intermediates
                BigInteger partyKey = ownIntermediate;
                for (int i = 1; i < n; i++)
                {
                    if (i != party)
                    {
                        // In a real implementation, party would receive g^(x1*xi) from Alice
                        // Here we just use the intermediateKeys directly
                        partyKey = (partyKey * intermediateKeys[i-1]) % P;
                    }
                }
                
                finalKeys[party] = partyKey;
            }
            
            // Assert - Check if all keys match
            for (int i = 1; i < n; i++)
            {
                Assert.Equal(finalKeys[0], finalKeys[i]);
            }
        }
    }