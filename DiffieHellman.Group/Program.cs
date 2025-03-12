using System.Numerics;
using Spectre.Console;

namespace DiffieHellman.Group;

/// <summary>
/// Main program with menu system for different Diffie-Hellman implementations
/// </summary>
public static class Program
{
    // Shared group parameters - must be known by all parties
    private static readonly BigInteger P = BigInteger.Parse("167943249473723310254471526982850180302760979434825200565439559077451944403500917305703253229047924284278754252898749094751102418090022311394732456214852230699470434922624388223791466545934584581681642497616046872294302749385778681506086391835727255193949884827669228131901777866179339899462487911092554010879");
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
                    GroupDiffieHellman.Run3PartyDiffieHellman(P, G);
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
}