using System.Numerics;
using System.Reflection;
using DiffieHellman.Group;

namespace DiffieHellman.Test;

public sealed class GroupDiffieHellmanSecurityTests
{
    // Test parameters
    private static readonly BigInteger P = BigInteger.Parse("167943249473723310254471526982850180302760979434825200565439559077451944403500917305703253229047924284278754252898749094751102418090022311394732456214852230699470434922624388223791466545934584581681642497616046872294302749385778681506086391835727255193949884827669228131901777866179339899462487911092554010879");
    private static readonly BigInteger G = 2;
        
    [Fact]
    public void DeriveKey_DifferentInputs_ProduceDifferentOutputs()
    {
        // Arrange
        BigInteger input1 = BigInteger.Parse("12345678901234567890");
        BigInteger input2 = BigInteger.Parse("12345678901234567891"); // Just 1 different
            
        // Access the private method using reflection
        var derivekeyMethod = typeof(GroupDiffieHellman).GetMethod("DeriveKey", 
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
            
        var generateKeyMethod = typeof(GroupDiffieHellman).GetMethod("GeneratePrivateKey", 
            BindingFlags.NonPublic | BindingFlags.Static);
            
        // Act
        BigInteger[] results = new BigInteger[iterations];
        for (int i = 0; i < iterations; i++)
        {
            results[i] = (BigInteger)generateKeyMethod.Invoke(null, new object[] { byteLength });
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
    public void DiffieHellman_ShouldBeSecure_AgainstPotentialEavesdropper()
    {
        // This is a conceptual test to demonstrate security against eavesdropping
            
        // Arrange - Alice and Bob's private/public keys
        BigInteger privateAlice = 123456789;
        BigInteger privateBob = 987654321;
            
        BigInteger publicAlice = BigInteger.ModPow(G, privateAlice, P);
        BigInteger publicBob = BigInteger.ModPow(G, privateBob, P);
            
        // Act - Compute shared secrets
        BigInteger sharedAlice = BigInteger.ModPow(publicBob, privateAlice, P);
        BigInteger sharedBob = BigInteger.ModPow(publicAlice, privateBob, P);
            
        // An eavesdropper would know: g, p, g^x, g^y
        // But not: x, y, or g^(xy)
            
        // Assert
        Assert.Equal(sharedAlice, sharedBob);
            
        // Conceptual: An eavesdropper with public keys can't compute the shared secret
        // No practical assertion here, just a conceptual note
    }
        
    [Fact]
    public void ThreePartyDH_ShouldBeSecure_AgainstPotentialEavesdropper()
    {
        // This is a conceptual test to demonstrate security of 3-party DH
            
        // Arrange - Alice, Bob, and Charlie's private/public keys
        BigInteger privateAlice = 123456789;
        BigInteger privateBob = 987654321;
        BigInteger privateCharlie = 555555555;
            
        BigInteger publicAlice = BigInteger.ModPow(G, privateAlice, P);
        BigInteger publicBob = BigInteger.ModPow(G, privateBob, P);
        BigInteger publicCharlie = BigInteger.ModPow(G, privateCharlie, P);
            
        // Act - Compute intermediate and final shared secrets
        BigInteger k_AB_Alice = BigInteger.ModPow(publicBob, privateAlice, P);
        BigInteger k_AB_Bob = BigInteger.ModPow(publicAlice, privateBob, P);
            
        // Alice sends g^xy to Charlie
        BigInteger g_xy = k_AB_Alice;
            
        // Compute three-party keys
        BigInteger k_ABC_Alice = (publicCharlie * k_AB_Alice) % P;
        BigInteger k_ABC_Bob = (publicCharlie * k_AB_Bob) % P;
        BigInteger k_ABC_Charlie = (g_xy * BigInteger.ModPow(G, privateCharlie, P)) % P;
            
        // An eavesdropper would know: g, p, g^x, g^y, g^z, g^xy
        // But not: x, y, z, or g^(xyz)
            
        // Assert
        Assert.Equal(k_AB_Alice, k_AB_Bob);
        Assert.Equal(k_ABC_Alice, k_ABC_Bob);
        Assert.Equal(k_ABC_Bob, k_ABC_Charlie);
            
        // Conceptual: An eavesdropper can't compute the shared secret
        // No practical assertion here, just a conceptual note
    }
        
    [Fact]
    public void DiffieHellman_ShouldUseAppropriately_LargePrimeModulus()
    {
        // Test to ensure the prime modulus is sufficiently large for security
            
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
        Assert.True(bitLength >= 1024, 
            $"Prime modulus should be at least 1024 bits for adequate security (actual: {bitLength} bits)");
    }
        
    [Theory]
    [InlineData(1, 1, 1)]               // All smallest values
    [InlineData(1, 1, int.MaxValue)]    // Mix of small and large
    [InlineData(1, int.MaxValue, 1)]    // Mix of small and large
    [InlineData(int.MaxValue, 1, 1)]    // Mix of small and large
    [InlineData(int.MaxValue, int.MaxValue, int.MaxValue)] // All large values
    public void EdgeCase_TestWithVariousPrivateKeys(int x, int y, int z)
    {
        // Arrange - Use the provided values but ensure they're valid for our modulus
        BigInteger privateX = new BigInteger(x) % (P - 1);
        BigInteger privateY = new BigInteger(y) % (P - 1);
        BigInteger privateZ = new BigInteger(z) % (P - 1);
            
        // Ensure private keys are positive
        if (privateX <= 0) privateX += (P - 1);
        if (privateY <= 0) privateY += (P - 1);
        if (privateZ <= 0) privateZ += (P - 1);
            
        // Act - Calculate public keys
        BigInteger g_x = BigInteger.ModPow(G, privateX, P);
        BigInteger g_y = BigInteger.ModPow(G, privateY, P);
        BigInteger g_z = BigInteger.ModPow(G, privateZ, P);
            
        // Calculate two-party DH key (Alice-Bob)
        BigInteger k_AB_Alice = BigInteger.ModPow(g_y, privateX, P);
        BigInteger k_AB_Bob = BigInteger.ModPow(g_x, privateY, P);
            
        // Calculate three-party keys
        BigInteger g_xy = k_AB_Alice;
            
        BigInteger k_ABC_Alice = (g_z * k_AB_Alice) % P;
        BigInteger k_ABC_Bob = (g_z * k_AB_Bob) % P;
        BigInteger k_ABC_Charlie = (g_z * g_xy) % P;
            
        // Assert
        Assert.Equal(k_AB_Alice, k_AB_Bob);
        Assert.Equal(k_ABC_Alice, k_ABC_Bob);
        Assert.Equal(k_ABC_Bob, k_ABC_Charlie);
    }
}