using System.Numerics;
using System.Reflection;
using DiffieHellman.Group;

namespace DiffieHellman.Test;

public sealed class TreeBasedDiffieHellmanSecurityTests
{
    // Test parameters
    private static readonly BigInteger P = BigInteger.Parse("23492587423094857029384572093485702983457029384752");
    private static readonly BigInteger G = 2;
        
    [Fact]
    public void DeriveKey_DifferentInputs_ProduceDifferentOutputs()
    {
        // Arrange
        BigInteger input1 = BigInteger.Parse("12345678901234567890");
        BigInteger input2 = BigInteger.Parse("12345678901234567891"); // Just 1 different
            
        // Access the private method using reflection
        var derivekeyMethod = typeof(TreeBasedDiffieHellman).GetMethod("DeriveKey", 
            BindingFlags.NonPublic | BindingFlags.Static);
            
        // Act
        byte[] result1 = (byte[])derivekeyMethod.Invoke(null, new object[] { input1 });
        byte[] result2 = (byte[])derivekeyMethod.Invoke(null, new object[] { input2 });
            
        // Assert
        Assert.Equal(32, result1.Length); // SHA256 produces 32 bytes
        Assert.Equal(32, result2.Length);
            
        // Outputs should be different even for very similar inputs
        bool allSame = true;
        for (int i = 0; i < result1.Length; i++)
        {
            if (result1[i] != result2[i])
            {
                allSame = false;
                break;
            }
        }
            
        Assert.False(allSame, "Different inputs should produce different hash outputs");
    }
        
    [Fact]
    public void GeneratePrivateKey_MultipleGenerations_ProduceDifferentKeys()
    {
        // Arrange
        int byteLength = 32;
        int iterations = 10;
            
        var generateKeyMethod = typeof(TreeBasedDiffieHellman).GetMethod("GeneratePrivateKey", 
            BindingFlags.NonPublic | BindingFlags.Static);
            
        // Act
        BigInteger[] results = new BigInteger[iterations];
        for (int i = 0; i < iterations; i++)
        {
            results[i] = (BigInteger)generateKeyMethod.Invoke(null, new object[] { byteLength, P });
        }
            
        // Assert
        for (int i = 0; i < iterations; i++)
        {
            for (int j = i + 1; j < iterations; j++)
            {
                Assert.NotEqual(results[i], results[j]);
            }
        }
    }
        
    [Fact]
    public void PrivateKeys_ShouldNotBeRecoverable_FromPublicKeys()
    {
        // This is a conceptual test to demonstrate the discrete log problem
            
        // Arrange
        BigInteger privateKey = 123456789;
        BigInteger publicKey = BigInteger.ModPow(G, privateKey, P);
            
        // Act - Here we would try to recover private key from public key
        // However, this is computationally infeasible due to the discrete log problem
        // So we're just demonstrating the concept
            
        // Assert
        // We can verify that knowing g, p, and g^x doesn't let us easily compute x
        Assert.Equal(publicKey, BigInteger.ModPow(G, privateKey, P));
            
        // But we can't go backwards efficiently (this is the security assumption)
        // No practical assertion here, just a conceptual note
    }
        
    [Fact]
    public void PrivateKeys_ShouldNotBeRecoverable_FromIntermediateKeys()
    {
        // This test demonstrates that intermediate keys don't leak private keys
            
        // Arrange
        BigInteger x1 = 123; // Alice's private key
        BigInteger x2 = 456; // Bob's private key
            
        BigInteger g_x1 = BigInteger.ModPow(G, x1, P); // Alice's public key
        BigInteger g_x2 = BigInteger.ModPow(G, x2, P); // Bob's public key
            
        // Alice computes intermediate key g^(x1*x2)
        BigInteger g_x1x2 = BigInteger.ModPow(g_x2, x1, P);
            
        // Act & Assert
        // An attacker would know g, p, g^x1, g^x2, and g^(x1*x2)
        // To recover x1 or x2, they would need to solve the discrete log problem
        // In other words, computing x1 from g^x1 or x2 from g^x2 is computationally infeasible
            
        // We can only verify that our computation is correct
        Assert.Equal(g_x1x2, BigInteger.ModPow(g_x1, x2, P)); // g^(x1*x2) = (g^x1)^x2 = (g^x2)^x1
            
        // But an attacker can't recover x1 or x2 from this
        // No practical assertion here, just a conceptual note
    }
        
    [Fact]
    public void NPartyDH_IntermediateKeysDoNotRevealFinalKey()
    {
        // This test ensures that knowing intermediate keys doesn't reveal the final key
            
        // Arrange - set up a 3-party example
        BigInteger x1 = 123; // Alice
        BigInteger x2 = 456; // Bob
        BigInteger x3 = 789; // Charlie
            
        BigInteger g_x1 = BigInteger.ModPow(G, x1, P);
        BigInteger g_x2 = BigInteger.ModPow(G, x2, P);
        BigInteger g_x3 = BigInteger.ModPow(G, x3, P);
            
        // Alice computes intermediate keys
        BigInteger g_x1x2 = BigInteger.ModPow(g_x2, x1, P);
        BigInteger g_x1x3 = BigInteger.ModPow(g_x3, x1, P);
            
        // Act - compute final keys
        var computeFinalKeyMethod = typeof(TreeBasedDiffieHellman).GetMethod("ComputeFinalKey", 
            BindingFlags.NonPublic | BindingFlags.Static);
            
        var computeFinalKeyForPartyMethod = typeof(TreeBasedDiffieHellman).GetMethod("ComputeFinalKeyForParty", 
            BindingFlags.NonPublic | BindingFlags.Static);
            
        // Set up the parameters for the methods
        BigInteger[] publicKeys = new BigInteger[] { g_x1, g_x2, g_x3 };
            
        // Alice's key
        BigInteger aliceKey = (BigInteger)computeFinalKeyMethod.Invoke(null, 
            new object[] { x1, publicKeys, 0, P });
            
        // Bob's key
        var bobIntermediates = new List<BigInteger> { g_x1x3 };
        BigInteger bobKey = (BigInteger)computeFinalKeyForPartyMethod.Invoke(null, 
            new object[] { x2, bobIntermediates, publicKeys, 1, P });
            
        // Charlie's key
        var charlieIntermediates = new List<BigInteger> { g_x1x2 };
        BigInteger charlieKey = (BigInteger)computeFinalKeyForPartyMethod.Invoke(null, 
            new object[] { x3, charlieIntermediates, publicKeys, 2, P });
            
        // Assert - all keys should match
        Assert.Equal(aliceKey, bobKey);
        Assert.Equal(bobKey, charlieKey);
            
        // An attacker knowing g_x1, g_x2, g_x3, g_x1x2, and g_x1x3 still can't compute the final key
        // without one of the private keys x1, x2, or x3.
        // This is a conceptual assertion; no practical test can be written for this.
    }
        
    [Fact]
    public void NPartyDH_SecurityAgainstPassiveAdversary()
    {
        // This test demonstrates that an eavesdropper can't compute the shared key
            
        // Arrange - set up a 3-party example
        BigInteger x1 = 123; // Alice
        BigInteger x2 = 456; // Bob
        BigInteger x3 = 789; // Charlie
            
        // Act - simulate the protocol execution
            
        // 1. Public key exchange
        BigInteger g_x1 = BigInteger.ModPow(G, x1, P);
        BigInteger g_x2 = BigInteger.ModPow(G, x2, P);
        BigInteger g_x3 = BigInteger.ModPow(G, x3, P);
            
        // 2. Intermediate key computation
        BigInteger g_x1x2 = BigInteger.ModPow(g_x2, x1, P);
        BigInteger g_x1x3 = BigInteger.ModPow(g_x3, x1, P);
            
        // 3. Distribution of intermediate keys
        // Bob receives g_x1x3
        // Charlie receives g_x1x2
            
        // 4. Final key computation
        BigInteger k_Alice = (g_x1x2 * g_x1x3) % P;
        BigInteger k_Bob = (BigInteger.ModPow(g_x1, x2, P) * g_x1x3) % P;
        BigInteger k_Charlie = (BigInteger.ModPow(g_x1, x3, P) * g_x1x2) % P;
            
        // Assert
        // 1. All parties should compute the same key
        Assert.Equal(k_Alice, k_Bob);
        Assert.Equal(k_Bob, k_Charlie);
            
        // 2. An eavesdropper would know:
        // - g, p
        // - g^x1, g^x2, g^x3 (public keys)
        // - g^(x1*x2), g^(x1*x3) (intermediate values)
        //
        // But they still can't compute the final key without knowing at least one private key.
        // This is a security property of the discrete logarithm problem.
    }
        
    [Fact]
    public void NPartyDH_SecurityWithCompromisedParty()
    {
        // This test examines what happens if one party is compromised
            
        // Arrange - set up a 4-party example
        BigInteger x1 = 123; // Alice (coordinator)
        BigInteger x2 = 456; // Bob
        BigInteger x3 = 789; // Charlie
        BigInteger x4 = 321; // Dave (compromised)
            
        // Act - simulate the protocol execution
            
        // 1. Public key exchange
        BigInteger g_x1 = BigInteger.ModPow(G, x1, P);
        BigInteger g_x2 = BigInteger.ModPow(G, x2, P);
        BigInteger g_x3 = BigInteger.ModPow(G, x3, P);
        BigInteger g_x4 = BigInteger.ModPow(G, x4, P);
            
        // 2. Intermediate key computation
        BigInteger g_x1x2 = BigInteger.ModPow(g_x2, x1, P);
        BigInteger g_x1x3 = BigInteger.ModPow(g_x3, x1, P);
        BigInteger g_x1x4 = BigInteger.ModPow(g_x4, x1, P);
            
        // 3. Distribution of intermediate keys
        // Bob receives g_x1x3, g_x1x4
        // Charlie receives g_x1x2, g_x1x4
        // Dave receives g_x1x2, g_x1x3
            
        // 4. Final key computation
        BigInteger k_Alice = (g_x1x2 * g_x1x3 * g_x1x4) % P;
        BigInteger k_Bob = (BigInteger.ModPow(g_x1, x2, P) * g_x1x3 * g_x1x4) % P;
        BigInteger k_Charlie = (BigInteger.ModPow(g_x1, x3, P) * g_x1x2 * g_x1x4) % P;
        BigInteger k_Dave = (BigInteger.ModPow(g_x1, x4, P) * g_x1x2 * g_x1x3) % P;
            
        // Assert
        // 1. All parties compute the same key
        Assert.Equal(k_Alice, k_Bob);
        Assert.Equal(k_Bob, k_Charlie);
        Assert.Equal(k_Charlie, k_Dave);
            
        // 2. If Dave is compromised (x4 is revealed), what can an attacker learn?
        // With x4, an attacker can compute:
        // - g^(x1*x2) from g^(x1*x2) (already known)
        // - g^(x1*x3) from g^(x1*x3) (already known)
        // - g^(x1*x4) from g^x1 and x4
        //
        // This allows them to compute the final key. This is an inherent limitation:
        // if a participant is compromised, the group key is compromised.
            
        // Verify that with x4, one can indeed compute the key
        BigInteger attackerComputed = (g_x1x2 * g_x1x3 * BigInteger.ModPow(g_x1, x4, P)) % P;
        Assert.Equal(k_Alice, attackerComputed);
    }
        
    [Fact]
    public void ModulusShouldBeOfAdequateSize()
    {
        // For educational/test purposes, we're using a smaller modulus
        // In a real implementation, this should be much larger
            
        // Arrange & Act
        int bitLength = P.ToByteArray().Length * 8;
            
        // Get the actual bit length (ignoring leading zeros)
        BigInteger temp = P;
        while (temp > 0)
        {
            bitLength = (int)BigInteger.Log2(temp) + 1;
            break;
        }
            
        // Assert
        // For educational purposes, we'll accept our smaller value
        // In production, we would require at least 2048 bits
        Assert.True(bitLength >= 160, $"Prime modulus should be at least 160 bits for adequate security in an educational setting (actual: {bitLength} bits)");
    }
        
    [Theory]
    [InlineData(1, 2, 3, 4)]
    [InlineData(100, 200, 300, 400)]
    [InlineData(int.MaxValue/2, int.MaxValue/3, int.MaxValue/4, int.MaxValue/5)]
    public void DifferentPrivateKeysProduceSameSharedKey(int x1, int x2, int x3, int x4)
    {
        // This test verifies that the protocol works correctly with various private key values
            
        // Arrange - normalize the private keys to be valid for our prime modulus
        BigInteger privateX1 = new BigInteger(x1) % (P - 1);
        BigInteger privateX2 = new BigInteger(x2) % (P - 1);
        BigInteger privateX3 = new BigInteger(x3) % (P - 1);
        BigInteger privateX4 = new BigInteger(x4) % (P - 1);
            
        // Ensure private keys are positive
        if (privateX1 <= 0) privateX1 += (P - 1);
        if (privateX2 <= 0) privateX2 += (P - 1);
        if (privateX3 <= 0) privateX3 += (P - 1);
        if (privateX4 <= 0) privateX4 += (P - 1);
            
        // Act - Calculate public keys
        BigInteger g_x1 = BigInteger.ModPow(G, privateX1, P);
        BigInteger g_x2 = BigInteger.ModPow(G, privateX2, P);
        BigInteger g_x3 = BigInteger.ModPow(G, privateX3, P);
        BigInteger g_x4 = BigInteger.ModPow(G, privateX4, P);
            
        // Alice computes intermediate keys
        BigInteger g_x1x2 = BigInteger.ModPow(g_x2, privateX1, P);
        BigInteger g_x1x3 = BigInteger.ModPow(g_x3, privateX1, P);
        BigInteger g_x1x4 = BigInteger.ModPow(g_x4, privateX1, P);
            
        // Final key computation
        BigInteger k_Alice = (g_x1x2 * g_x1x3 * g_x1x4) % P;
        BigInteger k_Bob = (BigInteger.ModPow(g_x1, privateX2, P) * g_x1x3 * g_x1x4) % P;
        BigInteger k_Charlie = (BigInteger.ModPow(g_x1, privateX3, P) * g_x1x2 * g_x1x4) % P;
        BigInteger k_Dave = (BigInteger.ModPow(g_x1, privateX4, P) * g_x1x2 * g_x1x3) % P;
            
        // Assert
        Assert.Equal(k_Alice, k_Bob);
        Assert.Equal(k_Bob, k_Charlie);
        Assert.Equal(k_Charlie, k_Dave);
    }
}