namespace Bloom.Filter.Test;

/// <summary>
/// Unit tests for the BloomFilter class.
/// </summary>
public sealed class BloomFilterTests
{
    /// <summary>
    /// Tests that the constructor correctly initializes the BloomFilter.
    /// </summary>
    [Fact]
    public void Constructor_InitializesCorrectly()
    {
        // Arrange & Act
        var bloomFilter = new BloomFilter(1000, 0.01);

        // Assert
        Assert.True(bloomFilter.Size > 0);
        Assert.True(bloomFilter.HashFunctionCount > 0);
        Assert.Equal(0, bloomFilter.EstimatedElementCount);
    }

    /// <summary>
    /// Tests that constructor handles edge cases for capacity and false positive rate.
    /// </summary>
    [Theory]
    [InlineData(1, 0.1)]
    [InlineData(10000, 0.001)]
    [InlineData(1000000, 0.00001)]
    public void Constructor_HandlesEdgeCases(int capacity, double falsePositiveRate)
    {
        // Arrange & Act
        var bloomFilter = new BloomFilter(capacity, falsePositiveRate);

        // Assert
        Assert.True(bloomFilter.Size > 0);
        Assert.True(bloomFilter.HashFunctionCount > 0);
    }

    /// <summary>
    /// Tests that Add method properly adds elements to the BloomFilter.
    /// </summary>
    [Fact]
    public void Add_AddsElementToFilter()
    {
        // Arrange
        var bloomFilter = new BloomFilter(100, 0.01);
        string element = "test element";

        // Act
        bloomFilter.Add(element);

        // Assert
        Assert.Equal(1, bloomFilter.EstimatedElementCount);
        Assert.True(bloomFilter.MightContain(element));
    }

    /// <summary>
    /// Tests that Add method throws ArgumentNullException for null elements.
    /// </summary>
    [Fact]
    public void Add_ThrowsArgumentNullException_WhenElementIsNull()
    {
        // Arrange
        var bloomFilter = new BloomFilter(100, 0.01);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => bloomFilter.Add(null));
    }

    /// <summary>
    /// Tests that MightContain returns false for elements that haven't been added.
    /// </summary>
    [Fact]
    public void MightContain_ReturnsFalse_ForElementsNotInFilter()
    {
        // Arrange
        var bloomFilter = new BloomFilter(100, 0.01);
        bloomFilter.Add("element1");
        bloomFilter.Add("element2");

        // Act & Assert
        Assert.False(bloomFilter.MightContain("element3"));
    }

    /// <summary>
    /// Tests that MightContain throws ArgumentNullException for null elements.
    /// </summary>
    [Fact]
    public void MightContain_ThrowsArgumentNullException_WhenElementIsNull()
    {
        // Arrange
        var bloomFilter = new BloomFilter(100, 0.01);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => bloomFilter.MightContain(null));
    }

    /// <summary>
    /// Tests that MightContain returns true for all elements that have been added.
    /// </summary>
    [Fact]
    public void MightContain_ReturnsTrue_ForAllAddedElements()
    {
        // Arrange
        var bloomFilter = new BloomFilter(1000, 0.01);
        var elements = new List<string>
        {
            "apple", "banana", "cherry", "date", "elderberry",
            "fig", "grape", "honeydew", "kiwi", "lemon"
        };

        // Act
        foreach (var element in elements)
        {
            bloomFilter.Add(element);
        }

        // Assert
        foreach (var element in elements)
        {
            Assert.True(bloomFilter.MightContain(element));
        }
    }

    /// <summary>
    /// Tests that the false positive rate is close to the expected rate.
    /// This test is probabilistic and may occasionally fail.
    /// </summary>
    [Fact]
    public void FalsePositiveRate_IsReasonablyClose_ToExpectedRate()
    {
        // Arrange
        int capacity = 10000;
        double targetFalsePositiveRate = 0.01;
        var bloomFilter = new BloomFilter(capacity, targetFalsePositiveRate);
            
        // Add capacity number of elements
        for (int i = 0; i < capacity; i++)
        {
            bloomFilter.Add($"element-{i}");
        }

        // Act
        // Check elements that weren't added to measure false positive rate
        int falsePositives = 0;
        int testCount = 10000;
        for (int i = 0; i < testCount; i++)
        {
            if (bloomFilter.MightContain($"not-added-{i}"))
            {
                falsePositives++;
            }
        }

        double actualFalsePositiveRate = (double)falsePositives / testCount;

        // Assert
        // Allow for some margin of error (3x the target rate)
        Assert.True(actualFalsePositiveRate < targetFalsePositiveRate * 3,
            $"False positive rate {actualFalsePositiveRate} exceeds 3x the target rate {targetFalsePositiveRate}");
    }

    /// <summary>
    /// Tests that elements with similar strings are properly distinguished.
    /// </summary>
    [Fact]
    public void SimilarElements_AreProperlyDistinguished()
    {
        // Arrange
        var bloomFilter = new BloomFilter(100, 0.01);
        bloomFilter.Add("test");
        bloomFilter.Add("test1");

        // Act & Assert
        Assert.True(bloomFilter.MightContain("test"));
        Assert.True(bloomFilter.MightContain("test1"));
        // "test2" wasn't added, so it should return false (unless a false positive occurs)
        // This test is probabilistic but should pass with high probability
        if (bloomFilter.MightContain("test2"))
        {
            // If this is a false positive, make sure other non-added elements are correctly identified
            int falsePositives = 0;
            for (int i = 3; i < 100; i++)
            {
                if (bloomFilter.MightContain($"test{i}"))
                {
                    falsePositives++;
                }
            }
            // Allow a small number of false positives
            Assert.True(falsePositives < 5, $"Too many false positives: {falsePositives}");
        }
    }

    /// <summary>
    /// Tests that CurrentFalsePositiveRate increases as more elements are added.
    /// </summary>
    [Fact]
    public void CurrentFalsePositiveRate_Increases_AsElementsAreAdded()
    {
        // Arrange
        var bloomFilter = new BloomFilter(1000, 0.01);
        double initialRate = bloomFilter.CurrentFalsePositiveRate;

        // Act
        for (int i = 0; i < 500; i++)
        {
            bloomFilter.Add($"element-{i}");
        }
        double midRate = bloomFilter.CurrentFalsePositiveRate;

        for (int i = 500; i < 1000; i++)
        {
            bloomFilter.Add($"element-{i}");
        }
        double finalRate = bloomFilter.CurrentFalsePositiveRate;

        // Assert
        Assert.True(initialRate < midRate);
        Assert.True(midRate < finalRate);
    }
}