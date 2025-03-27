using System.Text;

namespace Bloom.Filter;

/// <summary>
/// Implementation of a Bloom filter, a probabilistic data structure for membership testing.
/// </summary>
public sealed class BloomFilter
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