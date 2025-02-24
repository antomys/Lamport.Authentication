using System.Security.Cryptography;
using System.Text;
using Spectre.Console;

namespace Lamport.Authentication.Client;

public static class Program
{
    public static void Main()
    {
        while (true)
        {
            // Step 1: Define the secret and the number of hash iterations (n).
            AnsiConsole.MarkupLine("[bold] Step 1: Define the secret and the number of hash iterations (n).[/]\n");
            
            // Ask for the secret using Spectre.Console's Ask method.
            string secret = AnsiConsole.Ask<string>("[yellow]secret:[/]");
            int iterations = AnsiConsole.Ask<int>("[yellow]number of hash iterations (n):[/]");

            // Step 2: Server generates the hash chain.
            AnsiConsole.MarkupLine("\n[bold] Step 2: Server generates the hash chain.[/]");
            // Escape the caret (^) as ^
            AnsiConsole.MarkupLine("[bold] Computes: xn = H^n(secret) and stores it.[/]\n");
            
            // The server computes xn = Hⁿ(secret) and stores it.
            LamportAuthenticator serverAuth = new LamportAuthenticator(secret, iterations);
            string storedHash = serverAuth.GetCurrentHash();
            AnsiConsole.MarkupLine($"[green]Server stored hash (xn):[/] [blue]{storedHash}[/]");

            // Step 3: Client prepares the one-time password (OTP).
            AnsiConsole.MarkupLine("\n[bold] Step 3: Client prepares the one-time password (OTP).[/]");
            // Escape the caret in the client formula as well.
            AnsiConsole.MarkupLine("[bold] The client computes xn-1 = H^(n-1)(secret) using one fewer iteration.[/]");
            AnsiConsole.MarkupLine("[bold] This value will be used as the OTP.[/]\n");
            
            string clientOtp = ComputeClientOtp(secret, iterations - 1);
            AnsiConsole.MarkupLine($"[green]Client generated OTP (xn-1):[/] [blue]{clientOtp}[/]");
            
            // Step 4: Client sends the OTP to the server.
            AnsiConsole.MarkupLine("\n[bold] Step 4: Client sends the OTP to the server.[/]");
            AnsiConsole.MarkupLine("[bold] Server verifies by computing H(OTP) and checking: H(xn-1) ?= xn[/]");
            
            bool isVerified = serverAuth.VerifyOtp(clientOtp);
            AnsiConsole.MarkupLine($"[green]OTP verified:[/] [blue]{isVerified}[/]\n");
        }
    }

    /*
     * ComputeClientOTP: Computes the OTP for the client.
     *
     * Formula:
     *   x₀ = secret
     *   xᵢ = H(xᵢ₋₁) for i = 1, 2, …, n-1
     * The result is xn-1, which is sent as the OTP.
     */
    public static string ComputeClientOtp(string secret, int iterations)
    {
        // Start with the secret (x₀).
        byte[] hash = Encoding.UTF8.GetBytes(secret);
        // Compute the OTP by iterating H for (n-1) times.
        for (int i = 0; i < iterations; i++)
        {
            hash = SHA256.HashData(hash);
        }
        
        // Return the OTP in hexadecimal string format.
        return Convert.ToHexStringLower(hash);
    }
}