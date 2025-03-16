using System.Text;
using Spectre.Console;

namespace Bloom.Filter;

class Program
{
    static void Main(string[] args)
    {
        AnsiConsole.Write(
            new FigletText("Bloom Filter")
                .LeftJustified()
                .Color(Color.Green));

        // Create a rule with a title
        var rule = new Rule("[green]Bloom Filter Demo[/]")
        {
            Justification = Justify.Left,
        };
        AnsiConsole.Write(rule);

        // Display explanation
        AnsiConsole.MarkupLine("[yellow]A Bloom filter is a space-efficient probabilistic data structure used to test whether an element is a member of a set.[/]");
        AnsiConsole.MarkupLine("[yellow]It may produce false positives (indicating an element is in the set when it's not), but never false negatives.[/]");

        // Get user input for Bloom filter parameters
        int capacity = AnsiConsole.Prompt(
            new TextPrompt<int>("[green]Enter the expected number of elements (capacity):[/]")
                .PromptStyle("green")
                .ValidationErrorMessage("[red]Please enter a valid number[/]")
                .Validate(capacity => capacity > 0 ? ValidationResult.Success() : ValidationResult.Error("Capacity must be greater than 0")));

        double falsePositiveRate = AnsiConsole.Prompt(
            new TextPrompt<double>("[green]Enter the desired false positive rate (between 0 and 1):[/]")
                .PromptStyle("green")
                .ValidationErrorMessage("[red]Please enter a valid number[/]")
                .Validate(rate => rate > 0 && rate < 1 ? ValidationResult.Success() : ValidationResult.Error("Rate must be between 0 and 1")));

        // Create Bloom filter with user parameters
        var bloomFilter = new BloomFilter(capacity, falsePositiveRate);

        // Display Bloom filter configuration
        AnsiConsole.MarkupLine($"[green]Bloom Filter created with:[/]");
        AnsiConsole.MarkupLine($"[green]- Bit array size: {bloomFilter.Size} bits[/]");
        AnsiConsole.MarkupLine($"[green]- Number of hash functions: {bloomFilter.HashFunctionCount}[/]");

        bool running = true;
        while (running)
        {
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[green]What would you like to do?[/]")
                    .PageSize(10)
                    .AddChoices(new[] {
                        "Add an element",
                        "Check if element exists",
                        "Add multiple elements",
                        "View filter information",
                        "Exit"
                    }));

            switch (choice)
            {
                case "Add an element":
                    var element = AnsiConsole.Prompt(
                        new TextPrompt<string>("[green]Enter element to add:[/]")
                            .PromptStyle("green"));
                    bloomFilter.Add(element);
                    AnsiConsole.MarkupLine($"[green]Added '{element}' to the Bloom filter[/]");
                    break;

                case "Check if element exists":
                    var checkElement = AnsiConsole.Prompt(
                        new TextPrompt<string>("[green]Enter element to check:[/]")
                            .PromptStyle("green"));
                    bool result = bloomFilter.MightContain(checkElement);
                    if (result)
                        AnsiConsole.MarkupLine($"[yellow]'{checkElement}' might be in the Bloom filter (could be a false positive)[/]");
                    else
                        AnsiConsole.MarkupLine($"[red]'{checkElement}' is definitely not in the Bloom filter[/]");
                    break;

                case "Add multiple elements":
                    var elementsToAdd = AnsiConsole.Prompt(
                        new TextPrompt<string>("[green]Enter elements to add (comma-separated):[/]")
                            .PromptStyle("green"));
                    var elements = elementsToAdd.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var e in elements)
                    {
                        bloomFilter.Add(e.Trim());
                    }
                    AnsiConsole.MarkupLine($"[green]Added {elements.Length} elements to the Bloom filter[/]");
                    break;

                case "View filter information":
                    AnsiConsole.MarkupLine($"[green]Bloom Filter Information:[/]");
                    AnsiConsole.MarkupLine($"[green]- Bit array size: {bloomFilter.Size} bits[/]");
                    AnsiConsole.MarkupLine($"[green]- Number of hash functions: {bloomFilter.HashFunctionCount}[/]");
                    AnsiConsole.MarkupLine($"[green]- Estimated element count: {bloomFilter.EstimatedElementCount}[/]");
                    AnsiConsole.MarkupLine($"[green]- Current estimated false positive rate: {bloomFilter.CurrentFalsePositiveRate:P6}[/]");
                    break;

                case "Exit":
                    running = false;
                    break;
            }
        }

        AnsiConsole.MarkupLine("[green]Thank you for using the Bloom Filter Demo![/]");
    }
}

/// <summary>
/// Implementation of a Bloom filter, a probabilistic data structure for membership testing.
/// </summary>
public class BloomFilter
{
    // Bit array to store bloom filter data
    private readonly bool[] _bits;
        
    // Number of hash functions to use
    private readonly int _hashFunctionCount;
        
    // Count of elements added (estimated)
    private int _elementsAdded;

    /// <summary>
    /// Gets the size of the bit array.
    /// </summary>
    public int Size => _bits.Length;

    /// <summary>
    /// Gets the number of hash functions used.
    /// </summary>
    public int HashFunctionCount => _hashFunctionCount;

    /// <summary>
    /// Gets the estimated number of elements added to the filter.
    /// </summary>
    public int EstimatedElementCount => _elementsAdded;

    /// <summary>
    /// Gets the current estimated false positive rate based on the number of elements added.
    /// </summary>
    public double CurrentFalsePositiveRate
    {
        get
        {
            // Formula: (1 - e^(-k * n / m))^k
            // k = number of hash functions
            // n = number of elements added
            // m = size of bit array
            double exponent = -((double)_hashFunctionCount * _elementsAdded) / Size;
            return Math.Pow(1 - Math.Exp(exponent), _hashFunctionCount);
        }
    }

    /// <summary>
    /// Initializes a new instance of the BloomFilter class.
    /// </summary>
    /// <param name="capacity">Expected number of elements to be added.</param>
    /// <param name="falsePositiveRate">Desired false positive rate (between 0 and 1).</param>
    public BloomFilter(int capacity, double falsePositiveRate)
    {
        // Calculate optimal size for the bit array
        // Formula: m = -n * ln(p) / (ln(2)^2)
        // m = size of bit array
        // n = expected number of items
        // p = false positive probability
        int size = (int)Math.Ceiling(-capacity * Math.Log(falsePositiveRate) / (Math.Log(2) * Math.Log(2)));
            
        // Calculate optimal number of hash functions
        // Formula: k = (m/n) * ln(2)
        // k = number of hash functions
        // m = size of bit array
        // n = expected number of items
        int k = (int)Math.Ceiling((size / (double)capacity) * Math.Log(2));
            
        // Ensure minimum values for size and hash functions
        _bits = new bool[Math.Max(size, 1)];
        _hashFunctionCount = Math.Max(k, 1);
        _elementsAdded = 0;
    }

    /// <summary>
    /// Adds an element to the Bloom filter.
    /// </summary>
    /// <param name="element">The element to add.</param>
    public void Add(string element)
    {
        if (element == null) throw new ArgumentNullException(nameof(element));

        // Apply each hash function and set the corresponding bit
        foreach (int index in GetHashIndexes(element))
        {
            _bits[index] = true;
        }
            
        _elementsAdded++;
    }

    /// <summary>
    /// Checks if an element might be in the Bloom filter.
    /// </summary>
    /// <param name="element">The element to check.</param>
    /// <returns>
    /// True if the element might be in the set (could be a false positive).
    /// False if the element is definitely not in the set.
    /// </returns>
    public bool MightContain(string element)
    {
        if (element == null) throw new ArgumentNullException(nameof(element));

        // Check if all corresponding bits are set
        foreach (int index in GetHashIndexes(element))
        {
            if (!_bits[index]) return false;
        }
            
        return true;
    }

    /// <summary>
    /// Calculates hash indexes for the element using multiple hash functions.
    /// </summary>
    /// <param name="element">The element to hash.</param>
    /// <returns>An enumerable of indexes in the bit array.</returns>
    private IEnumerable<int> GetHashIndexes(string element)
    {
        // Use two hash functions to create k different hash functions
        // This is known as the Kirsch-Mitzenmacher technique
        byte[] data = Encoding.UTF8.GetBytes(element);
            
        // Calculate two independent hash values
        var hash1 = MurmurHash3(data, 0);
        var hash2 = MurmurHash3(data, hash1);
            
        // Generate k hash functions using the formula: h_i(x) = (hash1 + i * hash2) % m
        for (int i = 0; i < _hashFunctionCount; i++)
        {
            // Ensure the hash is positive before taking modulo
            uint combinedHash = (uint)((hash1 + ((long)i * hash2)) % (uint)int.MaxValue);
            yield return (int)(combinedHash % (uint)Size);
        }
    }

    /// <summary>
    /// Implementation of MurmurHash3 algorithm for string hashing.
    /// </summary>
    /// <param name="data">The data to hash.</param>
    /// <param name="seed">A seed value for the hash.</param>
    /// <returns>A hash value.</returns>
    private static int MurmurHash3(byte[] data, int seed)
    {
        const uint c1 = 0xcc9e2d51;
        const uint c2 = 0x1b873593;
        const int r1 = 15;
        const int r2 = 13;
        const uint m = 5;
        const uint n = 0xe6546b64;

        uint hash = (uint)seed;

        // Process 4 bytes at a time
        int i = 0;
        int length = data.Length;
        int remaining = length & 3; // length % 4
        int blocks = length / 4;

        // Main loop
        while (i < blocks)
        {
            uint k = BitConverter.ToUInt32(data, i * 4);
            i++;

            k *= c1;
            k = (k << r1) | (k >> (32 - r1));
            k *= c2;

            hash ^= k;
            hash = (hash << r2) | (hash >> (32 - r2));
            hash = hash * m + n;
        }

        // Handle remaining bytes
        uint tail = 0;
        if (remaining > 0)
        {
            int offset = blocks * 4;
            switch (remaining)
            {
                case 3:
                    tail = (uint)(data[offset + 2] << 16 | data[offset + 1] << 8 | data[offset]);
                    break;
                case 2:
                    tail = (uint)(data[offset + 1] << 8 | data[offset]);
                    break;
                case 1:
                    tail = data[offset];
                    break;
            }

            tail *= c1;
            tail = (tail << r1) | (tail >> (32 - r1));
            tail *= c2;
            hash ^= tail;
        }

        // Finalization
        hash ^= (uint)length;
        hash ^= hash >> 16;
        hash *= 0x85ebca6b;
        hash ^= hash >> 13;
        hash *= 0xc2b2ae35;
        hash ^= hash >> 16;

        return (int)hash;
    }
}