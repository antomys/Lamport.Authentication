namespace Lamport.Authentication.Core;

public sealed class Key(int i, string hash)
{
    public int I { get; init; } = i;

    public string Hash { get; init; } = hash;
}