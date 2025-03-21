namespace Atomic.Swap;

/// <summary>
/// Stores detailed status information about a swap process
/// </summary>
public sealed class SwapStatus
{
    public string SecretX { get; set; }
    
    public string SecretXHash { get; set; }
    
    public Transaction TX1 { get; set; }
    
    public Transaction TX2 { get; set; }
    
    public Transaction TX3 { get; set; }
    
    public Transaction TX4 { get; set; }
    
    public string TX1Id { get; set; }
    
    public string TX3Id { get; set; }
}