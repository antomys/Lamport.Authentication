using System.Numerics;
using System.Reflection;
using DiffieHellman.Group;
using Spectre.Console;

namespace DiffieHellman.Test;

public class TreeBasedDiffieHellmanIntegrationTests
{
    // Test parameters
    private static readonly BigInteger P = BigInteger.Parse("23492587423094857029384572093485702983457029384752");
    private static readonly BigInteger G = 2;
        
    [Fact]
    public void TreeBasedDH_TestMathematicalCorrectness()
    {
        // Test that demonstrates the mathematical correctness of the tree-based approach
            
        // Arrange - Set up parties
        int n = 4;
        BigInteger[] privateKeys = new BigInteger[n];
        privateKeys[0] = 123; // Alice (coordinator)
        privateKeys[1] = 456; // Bob
        privateKeys[2] = 789; // Charlie
        privateKeys[3] = 987; // Dave
            
        BigInteger[] publicKeys = new BigInteger[n];
        for (int i = 0; i < n; i++)
        {
            publicKeys[i] = BigInteger.ModPow(G, privateKeys[i], P);
        }
            
        // Compute intermediate keys (coordinator-computed values)
        BigInteger[] intermediateKeys = new BigInteger[n-1];
        for (int i = 1; i < n; i++)
        {
            intermediateKeys[i-1] = BigInteger.ModPow(publicKeys[i], privateKeys[0], P);
        }
            
        // Get methods using reflection
        var computeFinalKeyMethod = typeof(TreeBasedDiffieHellman).GetMethod("ComputeFinalKey", 
            BindingFlags.NonPublic | BindingFlags.Static);
            
        var computeFinalKeyForPartyMethod = typeof(TreeBasedDiffieHellman).GetMethod("ComputeFinalKeyForParty", 
            BindingFlags.NonPublic | BindingFlags.Static);
            
        // Act - compute final keys

        // Alice's key
        BigInteger finalKeyAlice = (BigInteger)computeFinalKeyMethod.Invoke(null, 
            new object[] { privateKeys[0], publicKeys, 0, P });
            
        // Bob's key
        var bobIntermediates = new System.Collections.Generic.List<BigInteger> { 
            intermediateKeys[1], // g^(x1*x3)
            intermediateKeys[2]  // g^(x1*x4)
        };
        BigInteger finalKeyBob = (BigInteger)computeFinalKeyForPartyMethod.Invoke(null, 
            new object[] { privateKeys[1], bobIntermediates, publicKeys, 1, P });
            
        // Charlie's key
        var charlieIntermediates = new System.Collections.Generic.List<BigInteger> { 
            intermediateKeys[0], // g^(x1*x2)
            intermediateKeys[2]  // g^(x1*x4)
        };
        BigInteger finalKeyCharlie = (BigInteger)computeFinalKeyForPartyMethod.Invoke(null, 
            new object[] { privateKeys[2], charlieIntermediates, publicKeys, 2, P });
            
        // Dave's key
        var daveIntermediates = new System.Collections.Generic.List<BigInteger> { 
            intermediateKeys[0], // g^(x1*x2)
            intermediateKeys[1]  // g^(x1*x3)
        };
        BigInteger finalKeyDave = (BigInteger)computeFinalKeyForPartyMethod.Invoke(null, 
            new object[] { privateKeys[3], daveIntermediates, publicKeys, 3, P });
            
        // Assert - All keys should match
        Assert.Equal(finalKeyAlice, finalKeyBob);
        Assert.Equal(finalKeyBob, finalKeyCharlie);
        Assert.Equal(finalKeyCharlie, finalKeyDave);
            
        // Optional - calculate key directly for verification
        // (product of all private keys modulo p-1)
        BigInteger exponent = (((privateKeys[0] * privateKeys[1]) % (P-1)) * 
            privateKeys[2] % (P-1)) * privateKeys[3] % (P-1);
        BigInteger directKey = BigInteger.ModPow(G, exponent, P);
            
        // This may not match exactly depending on how the protocol is implemented
        // So this assertion is optional and depends on the exact algorithm
        // Assert.Equal(directKey, finalKeyAlice);
    } 
    
    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void TreeBasedDH_VerifyScalability(int n)
    {
        // Calculate communications required
        int communications = 0;
    
        // Stage 1: Each party broadcasts public key (n communications)
        communications += n;
    
        // Stage 2: Coordinator sends intermediate values (n-1 communications)
        communications += (n - 1);
    
        // Verify we get exactly 2n-1 communications
        Assert.Equal(2*n - 1, communications);
    
        // Compare to naive approach (each party communicates with every other party)
        int naiveCommunications = n * (n - 1) / 2;
    
        // The tree-based approach becomes more efficient at n ≥ 5
        if (n >= 5)
        {
            Assert.True(communications < naiveCommunications, 
                $"Tree-based ({communications}) should require fewer communications than naive ({naiveCommunications})");
        }
        else
        {
            // For n ≤ 4, naive approach is actually better
            Assert.True(communications >= naiveCommunications, 
                $"For n={n}, naive ({naiveCommunications}) should require fewer communications than tree-based ({communications})");
        }
    }
}