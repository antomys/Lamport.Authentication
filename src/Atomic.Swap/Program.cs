using Spectre.Console;

namespace Atomic.Swap;

public static class Program
{
    public static async Task Main(string[] args)
    {
        // Create an ASCII art title
        AnsiConsole.Write(
            new FigletText("Atomic Swap")
                .LeftJustified()
                .Color(Color.Green));
            
        // Display an intro note
        AnsiConsole.MarkupLine("[italic]Atomic swap simulation for trustless cross-chain cryptocurrency exchange[/]");
        AnsiConsole.WriteLine();

        // Create blockchains
        var btcBlockchain = new Blockchain("Bitcoin");
        var altBlockchain = new Blockchain("Altcoin");
            
        // Create wallets with initial balances
        var alice = new Wallet("Alice", btcBalance: 10, altBalance: 0);
        var bob = new Wallet("Bob", btcBalance: 0, altBalance: 500);
            
        // Display initial balances
        AnsiConsole.MarkupLine("[bold]Initial Balances:[/]");
        DisplayBalances(alice, bob);
            
        // Ask user for swap amounts
        decimal btcAmount = AnsiConsole.Prompt(
            new TextPrompt<decimal>("How much [green]BTC[/] should Alice trade?")
                .DefaultValue(1.0m)
                .Validate(amount => 
                    amount <= 0 ? ValidationResult.Error("Amount must be positive") :
                    amount > alice.BtcBalance ? ValidationResult.Error($"Alice only has {alice.BtcBalance} BTC") :
                    ValidationResult.Success()));
            
        decimal altAmount = AnsiConsole.Prompt(
            new TextPrompt<decimal>("How much [blue]ALT[/] should Bob trade?")
                .DefaultValue(100.0m)
                .Validate(amount => 
                    amount <= 0 ? ValidationResult.Error("Amount must be positive") :
                    amount > bob.AltBalance ? ValidationResult.Error($"Bob only has {bob.AltBalance} ALT") :
                    ValidationResult.Success()));
            
        // Create and perform the swap
        var atomicSwap = new AtomicSwap(btcBlockchain, altBlockchain);
            
        await AnsiConsole.Status()
            .Start("Processing swap...", async ctx => 
            {
                ctx.Spinner(Spinner.Known.Star);
                ctx.SpinnerStyle(Style.Parse("green"));
                    
                await atomicSwap.PerformSwap(alice, bob, btcAmount, altAmount);
            });
            
        // Display final balances
        AnsiConsole.MarkupLine("\n[bold]Final Balances:[/]");
        DisplayBalances(alice, bob);
            
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim italic]Press any key to exit...[/]");
        Console.ReadKey();
    }
        
    private static void DisplayBalances(Wallet alice, Wallet bob)
    {
        var table = new Table();
            
        table.AddColumn(new TableColumn("Wallet").Centered());
        table.AddColumn(new TableColumn("BTC").Centered());
        table.AddColumn(new TableColumn("ALT").Centered());
            
        table.AddRow($"[green]{alice.Name}[/]", alice.BtcBalance.ToString(), alice.AltBalance.ToString());
        table.AddRow($"[blue]{bob.Name}[/]", bob.BtcBalance.ToString(), bob.AltBalance.ToString());
            
        AnsiConsole.Write(table);
    }
}