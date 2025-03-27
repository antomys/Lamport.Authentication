namespace Atomic.Swap;

/// <summary>
/// Stores the result of a swap operation
/// </summary>
public sealed class SwapResult
{
    public bool Success { get; set; }
    
    public string Message { get; set; }
    
    public SwapStatus SwapStatus { get; set; }
}