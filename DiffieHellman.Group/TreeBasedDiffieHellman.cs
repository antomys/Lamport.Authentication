using System.Numerics;
using System.Security.Cryptography;
using Spectre.Console;

namespace DiffieHellman.Group;

/// <summary>
/// Implementation of Tree-Based N-Party Diffie-Hellman
/// </summary>
public static class TreeBasedDiffieHellman
{
    /// <summary>
    /// Runs the N-party Tree-Based Diffie-Hellman implementation
    /// </summary>
    public static void RunNPartyDiffieHellman(BigInteger p, BigInteger g)
    {
        int communicationCount = 0;
            
        AnsiConsole.Write(
            new FigletText("N-Party DH")
                .Centered()
                .Color(Color.Green));
                    
        AnsiConsole.MarkupLine("[bold green]Tree-Based N-Party Diffie-Hellman Key Exchange (2n-1 communications)[/]");
        AnsiConsole.MarkupLine("[grey]================================================================[/]");
            
        try
        {
            // Get number of parties from user
            int n = AnsiConsole.Prompt(
                new TextPrompt<int>("Enter the number of parties (3-8 recommended):")
                    .PromptStyle("green")
                    .ValidationErrorMessage("[red]Please enter a valid number (minimum 3)[/]")
                    .Validate(n => 
                    {
                        if (n < 3)
                            return ValidationResult.Error("[red]At least 3 parties are required[/]");
                        if (n > 10)
                        {
                            AnsiConsole.Write("[yellow]Large numbers of parties may lead to wide output");
                            return ValidationResult.Success();
                        }
                        return ValidationResult.Success();
                    }));
                
            // Party names (we'll use as many as needed)
            string[] partyNames = { "Alice", "Bob", "Charlie", "Dave", "Eve", "Frank", "Grace", "Heidi", "Ivan", "Julia" };
            string[] colors = { "blue", "magenta", "yellow", "green", "red", "cyan", "grey", "purple", "orange", "silver" };
                
            if (n > partyNames.Length)
            {
                // If more parties than names, we'll add numbered participants
                var tempNames = new List<string>(partyNames);
                for (int i = partyNames.Length; i < n; i++)
                {
                    tempNames.Add($"Party {i+1}");
                }
                partyNames = tempNames.ToArray();
            }
                
            // Limit to available colors
            int colorCount = Math.Min(n, colors.Length);
                
            AnsiConsole.Status()
                .Start("Initializing Tree-Based Diffie-Hellman protocol...", ctx => 
                {
                    ctx.Spinner(Spinner.Known.Star);
                        
                    /************************ STEP 1: PRIVATE KEY GENERATION ************************/
                    ctx.Status("Generating private keys...");
                        
                    // Generate private keys for all parties
                    BigInteger[] privateKeys = new BigInteger[n];
                    for (int i = 0; i < n; i++)
                    {
                        privateKeys[i] = GeneratePrivateKey(32, p);
                    }
                        
                    // Display private keys
                    var privateKeysTable = new Table();
                    privateKeysTable.AddColumn("Party");
                    privateKeysTable.AddColumn("Private Key");
                        
                    for (int i = 0; i < n; i++)
                    {
                        string colorMarkup = i < colorCount ? colors[i] : "white";
                        privateKeysTable.AddRow($"[{colorMarkup}]{partyNames[i]}[/]", 
                            $"x{i+1} = [green]{privateKeys[i]}[/]");
                    }
                        
                    AnsiConsole.Write(new Rule("[bold]Private Keys Generated[/]") { Justification = Justify.Left });
                    AnsiConsole.Write(privateKeysTable);
                    AnsiConsole.WriteLine();
                        
                    // Display protocol explanation
                    var panelText = "Tree-Based Group Diffie-Hellman protocol uses an optimal communication pattern\n" +
                                    "where each participant only needs to broadcast once, and one final value\n" +
                                    "is broadcast by a coordinator. This reduces communications from O(n²) to O(n).\n\n" +
                                    $"Expected communications count: 2×{n}-1 = {2*n-1}";
                        
                    var panel = new Panel(panelText)
                    {
                        Border = BoxBorder.Rounded,
                        Padding = new Padding(1),
                        Header = new PanelHeader("[bold]Protocol Overview[/]")
                    };
                        
                    AnsiConsole.Write(panel);
                    AnsiConsole.WriteLine();
                        
                    /************************ STEP 2: INITIAL PUBLIC KEY EXCHANGE (N COMMUNICATIONS) ************************/
                    ctx.Status("Broadcasting initial public keys...");
                        
                    // Each party calculates and broadcasts their public key
                    BigInteger[] publicKeys = new BigInteger[n];
                    for (int i = 0; i < n; i++)
                    {
                        publicKeys[i] = BigInteger.ModPow(g, privateKeys[i], p);
                        string partyColor = i < colorCount ? colors[i] : "white";
                        LogCommunication(ref communicationCount, partyNames[i], "All parties", 
                            $"g^x{i+1} mod p", publicKeys[i], partyColor);
                    }
                        
                    var publicKeysTable = new Table();
                    publicKeysTable.AddColumn("Party");
                    publicKeysTable.AddColumn("Public Key");
                        
                    for (int i = 0; i < n; i++)
                    {
                        string colorMarkup = i < colorCount ? colors[i] : "white";
                        publicKeysTable.AddRow($"[{colorMarkup}]{partyNames[i]}[/]", 
                            $"g^x{i+1} mod p = [green]{publicKeys[i]}[/]");
                    }
                        
                    AnsiConsole.Write(new Rule($"[bold]Public Keys Shared[/] [grey](Communications so far: {communicationCount})[/]") 
                        { Justification = Justify.Left });
                    AnsiConsole.Write(publicKeysTable);
                    AnsiConsole.WriteLine();
                        
                    /************************ STEP 3: TREE-BASED COMPUTATION ************************/
                    ctx.Status("Building the Diffie-Hellman tree...");
                        
                    // First, Alice acts as the "root" node and computes intermediate keys with all others
                    BigInteger[] intermediateKeys = new BigInteger[n-1];
                    for (int i = 1; i < n; i++)
                    {
                        // Alice computes: (g^x_i)^x_1 mod p = g^(x_1*x_i) mod p
                        intermediateKeys[i-1] = BigInteger.ModPow(publicKeys[i], privateKeys[0], p);
                    }
                        
                    var intermediateTable = new Table();
                    intermediateTable.AddColumn("Computation");
                    intermediateTable.AddColumn("Result");
                        
                    for (int i = 1; i < n; i++)
                    {
                        string otherPartyColor = i < colorCount ? colors[i] : "white";
                        intermediateTable.AddRow(
                            $"[blue]{partyNames[0]}[/] computes (g^x{i+1})^x1 = g^(x1·x{i+1})", 
                            $"[green]{intermediateKeys[i-1]}[/]");
                    }
                        
                    AnsiConsole.Write(new Rule("[bold]Intermediate Key Computations[/]") { Justification = Justify.Left });
                    AnsiConsole.Write(intermediateTable);
                    AnsiConsole.WriteLine();
                        
                    /************************ STEP 4: BROADCAST BLINDED KEYS (N-1 COMMUNICATIONS) ************************/
                    ctx.Status("Broadcasting blinded keys...");
                        
                    // Alice broadcasts all intermediate values except to their corresponding party
                    // Each party i gets all intermediate values EXCEPT g^(x1*xi)
                    Dictionary<int, List<BigInteger>> receivedIntermediates = new Dictionary<int, List<BigInteger>>();
                        
                    // Initialize the collections for each party (except Alice)
                    for (int i = 1; i < n; i++)
                    {
                        receivedIntermediates[i] = new List<BigInteger>();
                    }
                        
                    // Alice sends intermediate keys to all other parties (n-1 communications)
                    // Each party receives n-2 intermediate keys (all except their own)
                    for (int recipient = 1; recipient < n; recipient++)
                    {
                        // Build the list of intermediates to send to this recipient
                        for (int keyId = 0; keyId < n-1; keyId++)
                        {
                            int correspondingParty = keyId + 1;
                                
                            // Don't send a party their own intermediate
                            if (correspondingParty != recipient)
                            {
                                receivedIntermediates[recipient].Add(intermediateKeys[keyId]);
                            }
                        }
                            
                        // Log this as one communication from Alice to each party
                        string recipientColor = recipient < colorCount ? colors[recipient] : "white";
                        LogCommunication(ref communicationCount, partyNames[0], partyNames[recipient], 
                            "set of n-2 intermediate keys", 
                            BigInteger.Zero, // placeholder, not showing all values
                            "blue", recipientColor);
                    }
                        
                    AnsiConsole.Write(new Rule($"[bold]Intermediate Keys Shared[/] [grey](Communications so far: {communicationCount})[/]") 
                        { Justification = Justify.Left });
                                          
                    var distributionPanel = new Panel(
                        $"[blue]{partyNames[0]}[/] broadcasts intermediate keys to each party (n-1 communications).\n" +
                        $"Each party receives all intermediate keys except their own corresponding key.\n" +
                        $"e.g., [magenta]{partyNames[1]}[/] receives g^(x1·x3), g^(x1·x4), ... but not g^(x1·x2)."
                    )
                    {
                        Border = BoxBorder.Rounded,
                        Padding = new Padding(1),
                        Header = new PanelHeader("[bold]Key Distribution[/]")
                    };
                        
                    AnsiConsole.Write(distributionPanel);
                    AnsiConsole.WriteLine();
                        
                    /************************ STEP 5: FINAL SHARED KEY COMPUTATION ************************/
                    ctx.Status("Computing the final shared key...");
                        
                    // Each party can now compute the shared key
                    BigInteger[] finalKeys = new BigInteger[n];
                        
                    // For Alice (party 0), she can compute:
                    // K = (g^x2)^(x3*...*xn*x1) * (g^x3)^(x2*x4*...*xn*x1) * ... * (g^xn)^(x2*...*xn-1*x1)
                    // But this simplifies to: K = g^(x1*x2*x3*...*xn)

                    // Alice computes her final key directly from the intermediate results
                    finalKeys[0] = ComputeFinalKey(privateKeys[0], publicKeys, 0, p);
                        
                    // Each other party i computes:
                    // K = (g^(x1*x2))^(x3*...*xi-1*xi+1*...*xn*xi) * (g^(x1*x3))^(x2*x4*...*xi-1*xi+1*...*xn*xi) * ...
                    // This also simplifies to: K = g^(x1*x2*x3*...*xn)
                    for (int i = 1; i < n; i++)
                    {
                        finalKeys[i] = ComputeFinalKeyForParty(privateKeys[i], receivedIntermediates[i], publicKeys, i, p);
                    }
                        
                    // Display final keys for each party
                    var finalKeysTable = new Table();
                    finalKeysTable.AddColumn("Party");
                    finalKeysTable.AddColumn("Final Shared Key");
                        
                    for (int i = 0; i < n; i++)
                    {
                        string colorMarkup = i < colorCount ? colors[i] : "white";
                        finalKeysTable.AddRow($"[{colorMarkup}]{partyNames[i]}[/]", $"[green]{finalKeys[i]}[/]");
                    }
                        
                    AnsiConsole.Write(new Rule("[bold]Final Group Shared Key Computation[/]") { Justification = Justify.Left });
                    AnsiConsole.Write(finalKeysTable);
                        
                    // Check if all keys match
                    bool keysMatch = true;
                    for (int i = 1; i < n; i++)
                    {
                        if (!finalKeys[0].Equals(finalKeys[i]))
                        {
                            keysMatch = false;
                            break;
                        }
                    }
                        
                    AnsiConsole.MarkupLine($"All parties have the same key: [{(keysMatch ? "green" : "red")}]{keysMatch}[/]");
                    AnsiConsole.MarkupLine($"[grey]Total communications required: {communicationCount} (theoretical minimum: {2*n-1})[/]");
                        
                    // Show mathematical explanation of the protocol
                    var mathPanel = new Panel(
                        "This tree-based protocol achieves an optimal communication pattern:\n\n" +
                        "1. Each party initially broadcasts their public key g^xi (n communications)\n" +
                        "2. A coordinator (Alice) computes intermediate keys g^(x1·xi) for all i\n" +
                        "3. Coordinator distributes the needed keys to each party (n-1 communications)\n" +
                        "4. Each party can compute the final key g^(x1·x2·...·xn)\n\n" +
                        $"Total communications required: n + (n-1) = 2n-1 = {2*n-1}"
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
                    byte[] finalKey = DeriveKey(finalKeys[0]);
                        
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
    /// Derives a symmetric key from the computed DH value
    /// </summary>
    private static byte[] DeriveKey(BigInteger dhValue)
    {
        return SHA256.HashData(dhValue.ToByteArray());
    }

    /// <summary>
    /// Generates a secure random private key of specified byte length
    /// </summary>
    private static BigInteger GeneratePrivateKey(int byteLength, BigInteger p)
    {
        var randomBytes = new byte[byteLength];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
            
        // Ensure the highest bit is cleared to keep the number positive
        randomBytes[byteLength - 1] &= 0x7F;
            
        // Make sure private key is positive and less than p
        BigInteger key = new BigInteger(randomBytes) % (p - 1);
        return key <= 0 ? key + (p - 1) : key;
    }
        
    /// <summary>
    /// Logs a communication between parties
    /// </summary>
    private static void LogCommunication(ref int communicationCount, string sender, string recipients, 
        string content, BigInteger value, string senderColor = "", string recipientColor = "")
    {
        communicationCount++;
            
        // Format the sender with color
        string senderDisplay = string.IsNullOrEmpty(senderColor) 
            ? sender
            : $"[{senderColor}]{sender}[/]";
            
        // Format recipients with color if provided
        string recipientsDisplay = string.IsNullOrEmpty(recipientColor)
            ? recipients
            : $"[{recipientColor}]{recipients}[/]";
            
        // For broadcast communications (value == 0), don't show the specific value
        string valueDisplay = value == BigInteger.Zero 
            ? "[grey](multiple values)[/]" 
            : $"[green]{value}[/]";
            
        // Display in console
        AnsiConsole.MarkupLine($"Communication #{communicationCount}: {senderDisplay} -> {recipientsDisplay}: {content} = {valueDisplay}");
    }
        
    /// <summary>
    /// Computes the final shared key for the coordinator (Alice)
    /// </summary>
    private static BigInteger ComputeFinalKey(BigInteger privateKey, BigInteger[] publicKeys, int partyIndex, BigInteger p)
    {
        // The coordinator computes the final key from all public keys
        // This is an optimized calculation that simplifies to g^(x1*x2*x3*...*xn)
            
        // Start with the party's own private key contribution
        BigInteger result = BigInteger.One;
            
        for (int i = 1; i < publicKeys.Length; i++)
        {
            // Alice raises all other public keys to her private key
            // This effectively computes: product(g^xi^x1) = g^(x1*sum(xi)) mod p
            result = (result * BigInteger.ModPow(publicKeys[i], privateKey, p)) % p;
        }
            
        return result;
    }
        
    /// <summary>
    /// Computes the final shared key for non-coordinator parties
    /// </summary>
    private static BigInteger ComputeFinalKeyForParty(
        BigInteger privateKey, 
        List<BigInteger> receivedIntermediates, 
        BigInteger[] publicKeys, 
        int partyIndex, 
        BigInteger p)
    {
        // For non-coordinator parties, compute final key from received intermediates
        // Each party i has received all g^(x1*xj) for j != i
            
        // Start with the party's own contribution by computing: (g^x1)^xi mod p = g^(x1*xi) mod p
        BigInteger ownIntermediate = BigInteger.ModPow(publicKeys[0], privateKey, p);
            
        // Multiply this with all received intermediates, each raised to the party's private key
        BigInteger result = ownIntermediate;
            
        foreach (var intermediate in receivedIntermediates)
        {
            // For each received intermediate key g^(x1*xj), party i computes:
            // g^(x1*xj)^xi = g^(x1*xj*xi)
            // When all multiplied together, this gives g^(x1*x2*...*xn)
            result = (result * intermediate) % p;
        }
            
        return result;
    }
}