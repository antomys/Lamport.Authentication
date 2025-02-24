using Lamport.Authentication.Client;

namespace Lamport.Authentication.Test;

public class LamportAuthenticatorTests
{
    // Test that a correctly computed OTP (xₙ₋₁) is accepted.
    [Fact]
    public void ValidOTPVerification_ShouldReturnTrue()
    {
        // Arrange: Define a test secret and number of iterations.
        var secret = "testSecret";
        var iterations = 100;
        var auth = new LamportAuthenticator(secret, iterations);
        // Compute the OTP: xₙ₋₁ = H^(n-1)(secret)
        var validOtp = Program.ComputeClientOtp(secret, iterations - 1);

        // Act: Verify the OTP.
        var result = auth.VerifyOtp(validOtp);

        // Assert: The OTP should be valid.
        Assert.True(result, "OTP verification should succeed with a valid OTP.");
    }

    // Test that an OTP computed with the wrong iteration count (e.g., xₙ instead of xₙ₋₁) is rejected.
    [Fact]
    public void InvalidOTPVerification_ShouldReturnFalse()
    {
        // Arrange: Define a test secret and number of iterations.
        var secret = "testSecret";
        var iterations = 100;
        var auth = new LamportAuthenticator(secret, iterations);
        // Generate an invalid OTP: using xₙ (i.e. applying H exactly n times) instead of xₙ₋₁.
        var invalidOtp = Program.ComputeClientOtp(secret, iterations);

        // Act: Verify the invalid OTP.
        var result = auth.VerifyOtp(invalidOtp);

        // Assert: The OTP should be rejected.
        Assert.False(result, "OTP verification should fail with an invalid OTP.");
    }

    // Test that reusing the same OTP is not allowed.
    [Fact]
    public void ReuseOTP_ShouldFailAfterFirstSuccessfulVerification()
    {
        // Arrange: Define a test secret and number of iterations.
        var secret = "testSecret";
        var iterations = 100;
        var auth = new LamportAuthenticator(secret, iterations);
        // Compute the valid OTP (xₙ₋₁).
        string otp = Program.ComputeClientOtp(secret, iterations - 1);

        // Act: First verification attempt.
        var firstAttempt = auth.VerifyOtp(otp);
        // Attempt to reuse the same OTP.
        var secondAttempt = auth.VerifyOtp(otp);

        // Assert: First attempt should succeed; reusing the OTP should fail.
        Assert.True(firstAttempt, "The first OTP verification should succeed.");
        Assert.False(secondAttempt, "Reusing the same OTP should fail.");
    }

    // Test sequential authentication across the entire hash chain.
    [Fact]
    public void SequentialOTPVerification_ShouldSucceedForEntireChain()
    {
        // Arrange: Use a smaller iteration count for brevity.
        var secret = "testSecret";
        var iterations = 10; // The chain: x₀, x₁, ..., x₁₀ (server holds x₁₀ initially)
        var auth = new LamportAuthenticator(secret, iterations);

        // Act & Assert: Verify each step in the chain.
        // The client OTP should be computed as: xₙ₋₁, then xₙ₋₂, ... until x₀.
        for (var i = iterations - 1; i >= 0; i--)
        {
            var otp = Program.ComputeClientOtp(secret, i);
            var result = auth.VerifyOtp(otp);
            Assert.True(result, $"OTP verification failed at chain step x_{i}.");
        }

        // The hash chain is now exhausted. Reusing the final OTP should now fail.
        var finalOtp = Program.ComputeClientOtp(secret, 0);
        var finalResult = auth.VerifyOtp(finalOtp);
        Assert.False(finalResult, "OTP verification should fail when the chain is exhausted.");
    }
}