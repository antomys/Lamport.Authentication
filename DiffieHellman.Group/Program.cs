﻿using System.Numerics;
using System.Security.Cryptography;
using Spectre.Console;

namespace DiffieHellman.Group;

/// <summary>
/// Main program with menu system for different Diffie-Hellman implementations
/// </summary>
public static class Program
{
    // Shared group parameters - must be known by all parties
    private static readonly BigInteger P = BigInteger.Parse("23492587423094857029384572093485702983457029384752");
    private static readonly BigInteger G = 2;

    public static void Main(string[] args)
    {
        DisplayMainMenu();
    }

    /// <summary>
    /// Displays the main menu and handles user selection
    /// </summary>
    private static void DisplayMainMenu()
    {
        bool exit = false;

        while (!exit)
        {
            // Clear console between runs
            Console.Clear();
            
            AnsiConsole.Write(
                new FigletText("Diffie-Hellman")
                    .Centered()
                    .Color(Color.Yellow));
                    
            AnsiConsole.WriteLine();
            
            var selection = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select a Diffie-Hellman implementation:")
                    .PageSize(10)
                    .AddChoices(new[] {
                        "1. Optimized 3-Party Diffie-Hellman (4 communications)",
                        "2. N-Party Diffie-Hellman (2n-1 communications)",
                        "3. About Diffie-Hellman Key Exchange",
                        "4. Exit"
                    }));

            switch (selection)
            {
                case "1. Optimized 3-Party Diffie-Hellman (4 communications)":
                    Run3PartyDiffieHellman();
                    WaitForKeyPress();
                    break;
                case "2. N-Party Diffie-Hellman (2n-1 communications)":
                    TreeBasedDiffieHellman.RunNPartyDiffieHellman(P, G);
                    WaitForKeyPress();
                    break;
                case "3. About Diffie-Hellman Key Exchange":
                    DisplayAboutInfo();
                    WaitForKeyPress();
                    break;
                case "4. Exit":
                    exit = true;
                    break;
            }
        }
        
        // Say goodbye
        AnsiConsole.Write(new Markup("[green]Thank you for using the Diffie-Hellman demonstration![/]\n"));
    }
    
    /// <summary>
    /// Waits for a key press before continuing
    /// </summary>
    private static void WaitForKeyPress()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Markup("[grey]Press any key to return to the main menu...[/]"));
        Console.ReadKey(true);
    }
    
    /// <summary>
    /// Displays information about Diffie-Hellman key exchange
    /// </summary>
    private static void DisplayAboutInfo()
    {
        AnsiConsole.Write(new Rule("[bold yellow]About Diffie-Hellman Key Exchange[/]"));
        
        var panel = new Panel(
            "The Diffie-Hellman key exchange is a method of securely exchanging cryptographic keys over a public channel without requiring a pre-shared secret.\n\n" +
            "Traditional 2-party Diffie-Hellman allows two parties to establish a shared secret by exchanging public values. " +
            "This demonstration extends the protocol to support multiple parties (3 or more) while minimizing the number of exchanges required.\n\n" +
            "[bold]3-Party Version:[/] Requires only 4 total communications (instead of 6 with a naive approach).\n" +
            "[bold]N-Party Version:[/] Requires only 2n-1 total communications (instead of n(n-1)/2 with a naive approach).\n\n" +
            "Both implementations are optimized for minimal exchanges while maintaining the security properties of the original Diffie-Hellman protocol."
        )
        {
            Border = BoxBorder.Rounded,
            Padding = new Padding(1),
            Header = new PanelHeader("Overview")
        };
        
        AnsiConsole.Write(panel);
        
        // Mathematical explanation
        AnsiConsole.Write(new Rule("[bold]Mathematical Foundation[/]"));
        
        var math = new Panel(
            "The security of Diffie-Hellman is based on the discrete logarithm problem:\n\n" +
            "1. We have shared public parameters: a large prime p and a generator g.\n" +
            "2. Each party chooses a private key (x, y, z, etc.) and computes their public key: g^x mod p.\n" +
            "3. In traditional DH, two parties can compute a shared secret: g^(xy) mod p.\n\n" +
            "For multi-party DH:\n" +
            "- 3-Party: The shared key is calculated as k_ABC = g^z * g^xy mod p.\n" +
            "- N-Party: Uses a star topology with partial keys to efficiently compute g^(x₁*x₂*...*xₙ) mod p."
        )
        {
            Border = BoxBorder.Rounded,
            Padding = new Padding(1),
            Header = new PanelHeader("Mathematics")
        };
        
        AnsiConsole.Write(math);
    }

    /// <summary>
    /// Runs the 3-party Diffie-Hellman implementation with exactly 4 communications
    /// as per the formula: A,B,C (x,y,z-private keys, g^x, g^y, g^z-public keys)
    /// k_AB = g^xy (2 interactions), k_ABC = k_ABC' = g^z*g^xy = g^(z+xy)
    /// </summary>
    private static void Run3PartyDiffieHellman()
    {
        int communicationCount = 0;
        
        AnsiConsole.Write(
            new FigletText("3-Party DH")
                .Centered()
                .Color(Color.Yellow));
                
        AnsiConsole.MarkupLine("[bold yellow]Three-Party Diffie-Hellman Key Exchange (4 communications)[/]");
        AnsiConsole.MarkupLine("[grey]================================================================[/]");
        
        try
        {
            AnsiConsole.Status()
                .Start("Initializing Diffie-Hellman protocol...", ctx => 
                {
                    ctx.Spinner(Spinner.Known.Star);
                    
                    /************************ STEP 1: PRIVATE KEY GENERATION ************************/
                    // Each party generates their own private key
                    ctx.Status("Generating private keys...");
                    BigInteger x = GeneratePrivateKey(32);  // Alice's private key
                    BigInteger y = GeneratePrivateKey(32);  // Bob's private key
                    BigInteger z = GeneratePrivateKey(32);  // Charlie's private key
                    
                    // Make sure private keys are positive and less than p
                    x = x % (P - 1);
                    y = y % (P - 1); 
                    z = z % (P - 1);
                    
                    // Ensure private keys are positive
                    if (x <= 0) x += (P - 1);
                    if (y <= 0) y += (P - 1);
                    if (z <= 0) z += (P - 1);
                    
                    var privateKeysTable = new Table();
                    privateKeysTable.AddColumn("Party");
                    privateKeysTable.AddColumn("Private Key");
                    
                    privateKeysTable.AddRow("[blue]Alice[/]", $"x = [green]{x}[/]");
                    privateKeysTable.AddRow("[magenta]Bob[/]", $"y = [green]{y}[/]");
                    privateKeysTable.AddRow("[yellow]Charlie[/]", $"z = [green]{z}[/]");
                    
                    AnsiConsole.Write(new Rule("[bold]Private Keys Generated[/]") { Justification = Justify.Left });
                    AnsiConsole.Write(privateKeysTable);
                    AnsiConsole.WriteLine();
                    
                    // Display formula explanation
                    var panel = new Panel(
                        "A, B, C (x, y, z-private keys, g^x, g^y, g^z-public keys)\n\n" +
                        "k_AB = g^xy (2 interactions, DH)\n" +
                        "k_ABC = k'_ABC = g^z * g^xy = g^(z+xy) (2 interactions, AC or BC)"
                    )
                    {
                        Border = BoxBorder.Rounded,
                        Padding = new Padding(1),
                        Header = new PanelHeader("[bold]Protocol Formula[/]")
                    };
                    
                    AnsiConsole.Write(panel);
                    AnsiConsole.WriteLine();
                    
                    /************************ STEP 2: PUBLIC KEY EXCHANGE (3 COMMUNICATIONS) ************************/
                    // Each party calculates and shares their public key
                    ctx.Status("Generating and exchanging public keys...");
                    
                    // Alice computes g^x mod p and shares with Bob and Charlie
                    BigInteger g_x = BigInteger.ModPow(G, x, P);
                    LogCommunication(ref communicationCount, "Alice", "Bob and Charlie", "g^x mod p", g_x);
                    
                    // Bob computes g^y mod p and shares with Alice and Charlie
                    BigInteger g_y = BigInteger.ModPow(G, y, P);
                    LogCommunication(ref communicationCount, "Bob", "Alice and Charlie", "g^y mod p", g_y);
                    
                    // Charlie computes g^z mod p and shares with Alice and Bob
                    BigInteger g_z = BigInteger.ModPow(G, z, P);
                    LogCommunication(ref communicationCount, "Charlie", "Alice and Bob", "g^z mod p", g_z);
                    
                    var publicKeysTable = new Table();
                    publicKeysTable.AddColumn("Party");
                    publicKeysTable.AddColumn("Public Key");
                    
                    publicKeysTable.AddRow("[blue]Alice[/]", $"g^x mod p = [green]{g_x}[/]");
                    publicKeysTable.AddRow("[magenta]Bob[/]", $"g^y mod p = [green]{g_y}[/]");
                    publicKeysTable.AddRow("[yellow]Charlie[/]", $"g^z mod p = [green]{g_z}[/]");
                    
                    AnsiConsole.Write(new Rule($"[bold]Public Keys Shared[/] [grey](Communications so far: {communicationCount})[/]") { Justification = Justify.Left });
                    AnsiConsole.Write(publicKeysTable);
                    AnsiConsole.WriteLine();
                    
                    /************************ STEP 3: TWO-PARTY DH KEY (ALICE-BOB) ************************/
                    // Alice and Bob can establish a standard DH key between them
                    ctx.Status("Computing two-party key...");
                    
                    // Alice computes k_AB = (g^y)^x mod p = g^(xy) mod p
                    BigInteger k_AB_Alice = BigInteger.ModPow(g_y, x, P);
                    
                    // Bob computes k_AB = (g^x)^y mod p = g^(xy) mod p
                    BigInteger k_AB_Bob = BigInteger.ModPow(g_x, y, P);
                    
                    var twoPartyTable = new Table();
                    twoPartyTable.AddColumn("Party");
                    twoPartyTable.AddColumn("Computation");
                    twoPartyTable.AddColumn("Result");
                    
                    twoPartyTable.AddRow("[blue]Alice[/]", "k_AB = (g^y)^x mod p", $"[green]{k_AB_Alice}[/]");
                    twoPartyTable.AddRow("[magenta]Bob[/]", "k_AB = (g^x)^y mod p", $"[green]{k_AB_Bob}[/]");
                    
                    AnsiConsole.Write(new Rule("[bold]Two-Party Shared Key (Alice-Bob)[/]") { Justification = Justify.Left });
                    AnsiConsole.Write(twoPartyTable);
                    AnsiConsole.MarkupLine($"Keys match: [{(k_AB_Alice.Equals(k_AB_Bob) ? "green" : "red")}]{k_AB_Alice.Equals(k_AB_Bob)}[/]");
                    AnsiConsole.WriteLine();
                    
                    /************************ STEP 4: ADDITIONAL COMMUNICATION (1) ************************/
                    // We need one more communication to establish a 3-party key
                    ctx.Status("Sending additional key material...");
                    
                    // Alice computes g^xy mod p (which she already has as k_AB_Alice)
                    // and sends to Charlie (fourth and final communication)
                    BigInteger g_xy = k_AB_Alice;
                    LogCommunication(ref communicationCount, "Alice", "Charlie", "g^xy mod p", g_xy);
                    
                    AnsiConsole.Write(new Rule($"[bold]Additional Communication[/] [grey](Communications so far: {communicationCount})[/]") { Justification = Justify.Left });
                    AnsiConsole.MarkupLine($"[blue]Alice[/] sent g^xy mod p to [yellow]Charlie[/]: [green]{g_xy}[/]");
                    AnsiConsole.WriteLine();
                    
                    /************************ STEP 5: THREE-PARTY KEY COMPUTATION ************************/
                    // Now all three parties can compute the shared key k_ABC = g^z * g^xy mod p
                    ctx.Status("Computing final three-party shared key...");
                    
                    // Alice computes k_ABC = g^z * g^xy mod p = g^z * k_AB_Alice mod p
                    BigInteger k_ABC_Alice = (g_z * k_AB_Alice) % P;
                    
                    // Bob computes k_ABC = g^z * g^xy mod p = g^z * k_AB_Bob mod p
                    BigInteger k_ABC_Bob = (g_z * k_AB_Bob) % P;
                    
                    // Charlie computes k_ABC = g^z * g^xy mod p
                    BigInteger k_ABC_Charlie = (g_z * g_xy) % P;
                    
                    var threePartyTable = new Table();
                    threePartyTable.AddColumn("Party");
                    threePartyTable.AddColumn("Computation");
                    threePartyTable.AddColumn("Result");
                    
                    threePartyTable.AddRow("[blue]Alice[/]", "k_ABC = g^z * g^xy mod p", $"[green]{k_ABC_Alice}[/]");
                    threePartyTable.AddRow("[magenta]Bob[/]", "k_ABC = g^z * g^xy mod p", $"[green]{k_ABC_Bob}[/]");
                    threePartyTable.AddRow("[yellow]Charlie[/]", "k_ABC = g^z * g^xy mod p", $"[green]{k_ABC_Charlie}[/]");
                    
                    AnsiConsole.Write(new Rule("[bold]Three-Party Shared Key Computation[/]") { Justification = Justify.Left });
                    AnsiConsole.Write(threePartyTable);
                    
                    // Check if all keys match
                    bool keysMatch = k_ABC_Alice.Equals(k_ABC_Bob) && 
                                     k_ABC_Bob.Equals(k_ABC_Charlie);
                    
                    AnsiConsole.MarkupLine($"All three-party keys match: [{(keysMatch ? "green" : "red")}]{keysMatch}[/]");
                    AnsiConsole.MarkupLine($"[grey]Total communications required: {communicationCount}[/]");
                    
                    // Show mathematical explanation of the protocol
                    var mathPanel = new Panel(
                        "This protocol works because:\n\n" +
                        "1. Alice & Bob establish k_AB = g^xy\n" +
                        "2. Alice shares g^xy with Charlie\n" +
                        "3. Everyone computes k_ABC = g^z * g^xy = g^z * g^(xy) = g^(z+xy)\n\n" +
                        "This requires exactly 4 communications instead of the 6 needed in\n" +
                        "standard 3-party Diffie-Hellman."
                    )
                    {
                        Border = BoxBorder.Rounded,
                        Padding = new Padding(1),
                        Header = new PanelHeader("[bold]Mathematical Explanation[/]")
                    };
                    
                    AnsiConsole.WriteLine();
                    AnsiConsole.Write(mathPanel);
                    AnsiConsole.WriteLine();
                    
                    // Finally, derive a symmetric encryption key using a KDF
                    ctx.Status("Deriving final symmetric key...");
                    byte[] finalKey = DeriveKey(k_ABC_Alice);
                    
                    AnsiConsole.Write(new Rule("[bold]Final 256-bit Symmetric Key[/]") { Justification = Justify.Left });
                    AnsiConsole.MarkupLine($"[green]{Convert.ToHexString(finalKey)}[/]");
                });
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[bold red]Error occurred:[/] {ex.Message}");
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
        }
    }
    
    /// <summary>
    /// Logs a communication and increments the counter
    /// </summary>
    private static void LogCommunication(ref int communicationCount, string sender, string recipients, string content, BigInteger value)
    {
        communicationCount++;
        
        // Format the sender with color if it's one of the named participants
        string senderDisplay = GetColoredName(sender);
        
        // Display in console
        AnsiConsole.MarkupLine($"Communication #{communicationCount}: {senderDisplay} -> {recipients}: {content} = [green]{value}[/]");
    }
    
    /// <summary>
    /// Returns a colored name for standard participants
    /// </summary>
    private static string GetColoredName(string name)
    {
        return name switch
        {
            "Alice" => "[blue]Alice[/]",
            "Bob" => "[magenta]Bob[/]",
            "Charlie" => "[yellow]Charlie[/]",
            "Dave" => "[green]Dave[/]",
            "Eve" => "[red]Eve[/]",
            "Frank" => "[cyan]Frank[/]",
            "Grace" => "[grey]Grace[/]",
            _ => name
        };
    }
    
    /// <summary>
    /// Derives a symmetric key from the computed DH value
    /// </summary>
    private static byte[] DeriveKey(BigInteger dhValue)
    {
        return SHA256.HashData(dhValue.ToByteArray());
    }

    /// <summary>
    /// Generates a secure random private key of specified byte length
    /// </summary>
    private static BigInteger GeneratePrivateKey(int byteLength)
    {
        var randomBytes = new byte[byteLength];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        
        // Ensure the highest bit is cleared to keep the number positive
        randomBytes[byteLength - 1] &= 0x7F;
        
        return new BigInteger(randomBytes);
    }
}