using Spectre.Console;

namespace Atomic.Swap;

/// <summary>
/// The main class implementing the atomic swap protocol using Hashed TimeLock Contracts (HTLC)
/// </summary>
public sealed class AtomicSwap(Blockchain btcBlockchain, Blockchain altBlockchain)
{
    private Blockchain BtcBlockchain { get; set; } = btcBlockchain;
    private Blockchain AltBlockchain { get; set; } = altBlockchain;

    /// <summary>
    /// Initiates an atomic swap between two parties
    /// </summary>
    /// <param name="initiator">The party initiating the swap (Party A)</param>
    /// <param name="counterParty">The counterparty (Party B)</param>
    /// <param name="btcAmount">Amount of BTC to swap</param>
    /// <param name="altAmount">Amount of ALT coins to swap</param>
    /// <param name="btcTimelock">Timelock duration for BTC in hours</param>
    /// <param name="altTimelock">Timelock duration for ALT in hours</param>
    /// <returns>Result of the swap process</returns>
    public async Task<SwapResult> PerformSwap(
        Wallet initiator, 
        Wallet counterParty, 
        decimal btcAmount, 
        decimal altAmount, 
        int btcTimelock = 48, 
        int altTimelock = 24)
    {
        AnsiConsole.MarkupLine($"[bold green]Starting atomic swap between {initiator.Name} and {counterParty.Name}[/]");
        AnsiConsole.MarkupLine($"{initiator.Name} wants to trade {btcAmount} BTC for {altAmount} ALT coins from {counterParty.Name}");
            
        // Check balances
        if (initiator.BtcBalance < btcAmount)
        {
            return new SwapResult { Success = false, Message = $"{initiator.Name} doesn't have enough BTC" };
        }
            
        if (counterParty.AltBalance < altAmount)
        {
            return new SwapResult { Success = false, Message = $"{counterParty.Name} doesn't have enough ALT coins" };
        }

        var result = new SwapResult();
        var swapStatus = new SwapStatus();
            
        // Step 1: Initiator (A) generates secret x and its hash H(x)
        AnsiConsole.MarkupLine($"\n[bold blue]Step 1:[/] {initiator.Name} generates secret x and its hash H(x)");
        string secretX = HashingService.GenerateRandomSecret();
        string secretXHash = HashingService.ComputeHash(secretX);
            
        AnsiConsole.MarkupLine($"Secret x (only known to {initiator.Name}): {secretX.Substring(0, 10)}...");
        AnsiConsole.MarkupLine($"Hash H(x) (shared with {counterParty.Name}): {secretXHash.Substring(0, 10)}...");
            
        swapStatus.SecretX = secretX;
        swapStatus.SecretXHash = secretXHash;
            
        // Step 2: Initiator (A) creates TX1 and TX2 (refund)
        AnsiConsole.MarkupLine($"\n[bold blue]Step 2:[/] {initiator.Name} creates transactions TX1 and TX2 (refund)");
            
        // TX1: "Pay btcAmount to counterParty if (x for H(x) is revealed and signed by counterParty) or (signed by both)"
        var tx1 = new Transaction(initiator.Name, counterParty.Name, btcAmount);
        tx1.Conditions["HashX"] = secretXHash;
        tx1.Conditions["RequiresSignatureFrom"] = counterParty.PublicKey;
            
        // TX2: Refund transaction after timelock expires
        var tx2 = new Transaction(counterParty.Name, initiator.Name, btcAmount);
        tx2.TimeLock = DateTime.Now.AddHours(btcTimelock);
        tx2.Conditions["RefundFor"] = "TX1";
        tx2.Conditions["SignedByInitiator"] = initiator.SignMessage($"Refund-{btcAmount}-BTC");
            
        swapStatus.TX1 = tx1;
        swapStatus.TX2 = tx2;
            
        AnsiConsole.MarkupLine($"TX1 created: {btcAmount} BTC from {initiator.Name} to {counterParty.Name} with hash lock");
        AnsiConsole.MarkupLine($"TX2 created: Refund to {initiator.Name} after {btcTimelock} hours if swap fails");

        // Step 3: Initiator sends TX2 to counterParty for signing
        AnsiConsole.MarkupLine($"\n[bold blue]Step 3:[/] {initiator.Name} sends TX2 to {counterParty.Name} for signing");
        // Counterparty signs the refund transaction
        tx2.Conditions["SignedByCounterParty"] = counterParty.SignMessage($"Approve-Refund-{btcAmount}-BTC");
        AnsiConsole.MarkupLine($"{counterParty.Name} has signed TX2 (refund transaction)");
            
        // Step 4: Initiator submits TX1 to the BTC blockchain
        AnsiConsole.MarkupLine($"\n[bold blue]Step 4:[/] {initiator.Name} submits TX1 to the {BtcBlockchain.Name} blockchain");
        string tx1Id = BtcBlockchain.AddTransaction(tx1);
        swapStatus.TX1Id = tx1Id;
            
        initiator.BtcBalance -= btcAmount; // Deduct balance
        AnsiConsole.MarkupLine($"TX1 confirmed on {BtcBlockchain.Name} with ID: {tx1Id}");
        AnsiConsole.MarkupLine($"{initiator.Name}'s BTC balance is now {initiator.BtcBalance}");
        AnsiConsole.MarkupLine($"Hash H(x) is now publicly visible on the blockchain: {secretXHash.Substring(0, 10)}...");

        // Step 5: CounterParty (B) creates TX3 and TX4 (refund)
        AnsiConsole.MarkupLine($"\n[bold blue]Step 5:[/] {counterParty.Name} creates transactions TX3 and TX4 (refund)");
            
        // TX3: "Pay altAmount to initiator if (x for H(x) is revealed and signed by initiator) or (signed by both)"
        var tx3 = new Transaction(counterParty.Name, initiator.Name, altAmount);
        tx3.Conditions["HashX"] = secretXHash; // Same hash as in TX1
        tx3.Conditions["RequiresSignatureFrom"] = initiator.PublicKey;
            
        // TX4: Refund transaction after timelock expires
        var tx4 = new Transaction(initiator.Name, counterParty.Name, altAmount);
        tx4.TimeLock = DateTime.Now.AddHours(altTimelock);
        tx4.Conditions["RefundFor"] = "TX3";
        tx4.Conditions["SignedByCounterParty"] = counterParty.SignMessage($"Refund-{altAmount}-ALT");
            
        swapStatus.TX3 = tx3;
        swapStatus.TX4 = tx4;
            
        AnsiConsole.MarkupLine($"TX3 created: {altAmount} ALT from {counterParty.Name} to {initiator.Name} with same hash lock");
        AnsiConsole.MarkupLine($"TX4 created: Refund to {counterParty.Name} after {altTimelock} hours if swap fails");

        // Step 6: CounterParty sends TX4 to initiator for signing
        AnsiConsole.MarkupLine($"\n[bold blue]Step 6:[/] {counterParty.Name} sends TX4 to {initiator.Name} for signing");
        tx4.Conditions["SignedByInitiator"] = initiator.SignMessage($"Approve-Refund-{altAmount}-ALT");
        AnsiConsole.MarkupLine($"{initiator.Name} has signed TX4 (refund transaction)");

        // Step 7: CounterParty submits TX3 to the ALT blockchain
        AnsiConsole.MarkupLine($"\n[bold blue]Step 7:[/] {counterParty.Name} submits TX3 to the {AltBlockchain.Name} blockchain");
        string tx3Id = AltBlockchain.AddTransaction(tx3);
        swapStatus.TX3Id = tx3Id;
            
        counterParty.AltBalance -= altAmount; // Deduct balance
        AnsiConsole.MarkupLine($"TX3 confirmed on {AltBlockchain.Name} with ID: {tx3Id}");
        AnsiConsole.MarkupLine($"{counterParty.Name}'s ALT balance is now {counterParty.AltBalance}");

        // Step 8: Initiator (A) spends TX3 by revealing secret x
        AnsiConsole.MarkupLine($"\n[bold blue]Step 8:[/] {initiator.Name} spends TX3 by revealing secret x");
            
        // Create a spending transaction revealing x
        var spendTx3 = new Transaction(counterParty.Name, initiator.Name, altAmount);
        spendTx3.Conditions["RevealedSecret"] = secretX;
        spendTx3.Conditions["SignedByInitiator"] = initiator.SignMessage($"Claim-{altAmount}-ALT-with-secret");
            
        string spendTx3Id = AltBlockchain.AddTransaction(spendTx3);
        AltBlockchain.Transactions[tx3Id].IsSpent = true;
            
        initiator.AltBalance += altAmount; // Add to balance
        AnsiConsole.MarkupLine($"{initiator.Name} has claimed {altAmount} ALT by revealing secret x");
        AnsiConsole.MarkupLine($"{initiator.Name}'s ALT balance is now {initiator.AltBalance}");
        AnsiConsole.MarkupLine($"Secret x is now publicly visible on the {AltBlockchain.Name}: {secretX.Substring(0, 10)}...");

        // Step 9: CounterParty (B) uses revealed x to spend TX1
        AnsiConsole.MarkupLine($"\n[bold blue]Step 9:[/] {counterParty.Name} uses revealed x to spend TX1");
            
        // CounterParty saw x on the ALT blockchain, now uses it to claim BTC
        var spendTx1 = new Transaction(initiator.Name, counterParty.Name, btcAmount);
        spendTx1.Conditions["RevealedSecret"] = secretX;
        spendTx1.Conditions["SignedByCounterParty"] = counterParty.SignMessage($"Claim-{btcAmount}-BTC-with-secret");
            
        string spendTx1Id = BtcBlockchain.AddTransaction(spendTx1);
        BtcBlockchain.Transactions[tx1Id].IsSpent = true;
            
        counterParty.BtcBalance += btcAmount; // Add to balance
        AnsiConsole.MarkupLine($"{counterParty.Name} has claimed {btcAmount} BTC using the revealed secret x");
        AnsiConsole.MarkupLine($"{counterParty.Name}'s BTC balance is now {counterParty.BtcBalance}");

        // Final swap status
        AnsiConsole.MarkupLine($"\n[bold green]Atomic swap completed successfully![/]");
        AnsiConsole.MarkupLine($"{initiator.Name} traded {btcAmount} BTC for {altAmount} ALT from {counterParty.Name}");
            
        // Add a small delay for better visualization in the console
        await Task.Delay(1000);
            
        result.Success = true;
        result.Message = "Swap completed successfully";
        result.SwapStatus = swapStatus;
        return result;
    }
}