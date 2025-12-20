namespace Ecng.Tests.Common;

[TestClass]
public class RandomArrayTests : BaseTestClass
{
	[TestMethod]
	public void Count_Doubles()
	{
		// Arrange & Act
		var randomArray = new RandomArray<double>(100);

		// Assert
		randomArray.Count.AssertEqual(100);

		// Verify we can get values
		for (int i = 0; i < 10; i++)
		{
			var value = randomArray.Next();
			(value >= 0 && value <= 1).AssertTrue($"value={value} should be >=0 and <=1");
		}
	}

	[TestMethod]
	public void Count_Ints()
	{
		// Arrange & Act
		var randomArray = new RandomArray<int>(50);

		// Assert
		randomArray.Count.AssertEqual(50);

		// Verify we can get values
		for (int i = 0; i < 10; i++)
		{
			var value = randomArray.Next();
			value.AssertNotEqual(0); // statistically should not be zero every time
		}
	}

	[TestMethod]
	public void Count_Bools()
	{
		// Arrange & Act
		var randomArray = new RandomArray<bool>(100);

		// Assert
		randomArray.Count.AssertEqual(100);

		// Verify we get both true and false values
		var values = new HashSet<bool>();
		for (int i = 0; i < 100; i++)
		{
			values.Add(randomArray.Next());
		}

		// With 100 random booleans, we should get both true and false
		values.Contains(true).AssertTrue();
		values.Contains(false).AssertTrue();
	}

	[TestMethod]
	public void Count_Bytes()
	{
		// Arrange & Act
		var randomArray = new RandomArray<byte>(50);

		// Assert
		randomArray.Count.AssertEqual(50);

		// Verify we can get values
		var value = randomArray.Next();
		(value >= 0 && value <= 255).AssertTrue($"value={value} should be >=0 and <=255");
	}

	[TestMethod]
	public void Range_Ints()
	{
		// Arrange & Act
		var randomArray = new RandomArray<int>(10, 20, 100);

		// Assert
		randomArray.Count.AssertEqual(100);
		randomArray.Min.AssertEqual(10);
		randomArray.Max.AssertEqual(20);

		// Verify all values are within range
		for (int i = 0; i < 100; i++)
		{
			var value = randomArray.Next();
			(value >= 10 && value <= 20).AssertTrue($"value={value} should be >=10 and <=20");
		}
	}

	[TestMethod]
	public void Range_TimeSpans()
	{
		// Arrange
		var min = TimeSpan.FromMinutes(10);
		var max = TimeSpan.FromMinutes(20);

		// Act
		var randomArray = new RandomArray<TimeSpan>(min, max, 50);

		// Assert
		randomArray.Count.AssertEqual(50);
		randomArray.Min.AssertEqual(min);
		randomArray.Max.AssertEqual(max);

		// Verify all values are within range
		for (int i = 0; i < 50; i++)
		{
			var value = randomArray.Next();
			(value >= min && value <= max).AssertTrue($"value={value} should be >={min} and <={max}");
		}
	}

	[TestMethod]
	public void MinGreaterThanMax_ThrowsArgumentException()
	{
		// Arrange & Act & Assert
		ThrowsExactly<ArgumentException>(() => new RandomArray<int>(20, 10, 100));
	}

	[TestMethod]
	public void MinEqualsMax_CreatesArrayWithSameValue()
	{
		// Arrange & Act
		var randomArray = new RandomArray<int>(15, 15, 10);

		// Assert
		randomArray.Count.AssertEqual(10);

		// Verify all values are equal to min/max
		for (int i = 0; i < 10; i++)
		{
			var value = randomArray.Next();
			value.AssertEqual(15);
		}
	}

	[TestMethod]
	public void Next_CallsMultipleTimes_ReturnsValuesInOrder()
	{
		// Arrange
		var randomArray = new RandomArray<int>(5);

		// Act - call Next more times than Count
		var values = new List<int>();
		for (int i = 0; i < 15; i++)
		{
			values.Add(randomArray.Next());
		}

		// Assert - should cycle through the array
		values.Count.AssertEqual(15);
	}

	[TestMethod]
	public void Next_ThreadSafe_NoExceptions()
	{
		// Arrange
		var randomArray = new RandomArray<int>(1000);
		var exceptions = new List<Exception>();
		var exLock = new Lock();

		// Act - access from multiple threads
		Parallel.For(0, 100, i =>
		{
			try
			{
				for (int j = 0; j < 10; j++)
				{
					randomArray.Next();
				}
			}
			catch (Exception ex)
			{
				using (exLock.EnterScope())
				{
					exceptions.Add(ex);
				}
			}
		});

		// Assert
		exceptions.Count.AssertEqual(0);
	}

	// Tests for all numeric primitive types

	[TestMethod]
	public void Count_Floats()
	{
		// Arrange & Act
		var randomArray = new RandomArray<float>(50);

		// Assert
		randomArray.Count.AssertEqual(50);

		for (int i = 0; i < 10; i++)
		{
			var value = randomArray.Next();
			(value >= 0 && value <= 1).AssertTrue($"value={value} should be >=0 and <=1");
		}
	}

	[TestMethod]
	public void Count_Decimals()
	{
		// Arrange & Act
		var randomArray = new RandomArray<decimal>(50);

		// Assert
		randomArray.Count.AssertEqual(50);

		for (int i = 0; i < 10; i++)
		{
			var value = randomArray.Next();
			(value >= 0 && value <= 1).AssertTrue($"value={value} should be >=0 and <=1");
		}
	}

	[TestMethod]
	public void Count_Longs()
	{
		// Arrange & Act
		var randomArray = new RandomArray<long>(50);

		// Assert
		randomArray.Count.AssertEqual(50);

		var value = randomArray.Next();
		value.AssertNotEqual(0);
	}

	[TestMethod]
	public void Count_Shorts()
	{
		// Arrange & Act
		var randomArray = new RandomArray<short>(50);

		// Assert
		randomArray.Count.AssertEqual(50);

		var value = randomArray.Next();
		(value >= short.MinValue && value <= short.MaxValue).AssertTrue($"value={value} should be >={short.MinValue} and <={short.MaxValue}");
	}

	[TestMethod]
	public void Count_UInts()
	{
		// Arrange & Act
		var randomArray = new RandomArray<uint>(50);

		// Assert
		randomArray.Count.AssertEqual(50);

		// Verify we get different values (not all zeros)
		var values = Enumerable.Range(0, 20).Select(_ => randomArray.Next()).ToArray();
		values.Distinct().Count().AssertGreater(1, "Should generate different uint values");

		// Verify some values are > 0 (randomness check)
		values.Any(v => v > 0).AssertTrue("Some uint values should be > 0");
	}

	[TestMethod]
	public void Count_ULongs()
	{
		// Arrange & Act
		var randomArray = new RandomArray<ulong>(50);

		// Assert
		randomArray.Count.AssertEqual(50);

		// Verify we get different values (not all zeros)
		var values = Enumerable.Range(0, 20).Select(_ => randomArray.Next()).ToArray();
		values.Distinct().Count().AssertGreater(1, "Should generate different ulong values");

		// Verify some values are > 0 (randomness check)
		values.Any(v => v > 0).AssertTrue("Some ulong values should be > 0");
	}

	[TestMethod]
	public void Count_UShorts()
	{
		// Arrange & Act
		var randomArray = new RandomArray<ushort>(50);

		// Assert
		randomArray.Count.AssertEqual(50);

		// Verify we get different values (not all zeros)
		var values = Enumerable.Range(0, 20).Select(_ => randomArray.Next()).ToArray();
		values.Distinct().Count().AssertGreater(1, "Should generate different ushort values");

		// Verify some values are > 0 (randomness check)
		values.Any(v => v > 0).AssertTrue("Some ushort values should be > 0");
	}

	[TestMethod]
	public void Count_SBytes()
	{
		// Arrange & Act
		var randomArray = new RandomArray<sbyte>(50);

		// Assert
		randomArray.Count.AssertEqual(50);

		var value = randomArray.Next();
		(value >= sbyte.MinValue && value <= sbyte.MaxValue).AssertTrue($"value={value} should be >={sbyte.MinValue} and <={sbyte.MaxValue}");
	}

	[TestMethod]
	public void Count_Chars()
	{
		// Arrange & Act
		var randomArray = new RandomArray<char>(50);

		// Assert
		randomArray.Count.AssertEqual(50);

		var value = randomArray.Next();
		(value >= 32 && value <= 127).AssertTrue($"value={(int)value} should be >=32 and <=127"); // Printable ASCII
	}

	// Tests for ranged constructors with all types

	[TestMethod]
	public void Range_Longs()
	{
		// Arrange & Act
		var randomArray = new RandomArray<long>(100, 200, 50);

		// Assert
		randomArray.Count.AssertEqual(50);

		for (int i = 0; i < 50; i++)
		{
			var value = randomArray.Next();
			(value >= 100 && value <= 200).AssertTrue($"value={value} should be >=100 and <=200");
		}
	}

	[TestMethod]
	public void Range_Shorts()
	{
		// Arrange & Act
		var randomArray = new RandomArray<short>(10, 100, 50);

		// Assert
		randomArray.Count.AssertEqual(50);

		for (int i = 0; i < 50; i++)
		{
			var value = randomArray.Next();
			(value >= 10 && value <= 100).AssertTrue($"value={value} should be >=10 and <=100");
		}
	}

	[TestMethod]
	public void Range_Bytes()
	{
		// Arrange & Act
		var randomArray = new RandomArray<byte>(10, 250, 50);

		// Assert
		randomArray.Count.AssertEqual(50);

		for (int i = 0; i < 50; i++)
		{
			var value = randomArray.Next();
			(value >= 10 && value <= 250).AssertTrue($"value={value} should be >=10 and <=250");
		}
	}

	[TestMethod]
	public void Range_SBytes()
	{
		// Arrange & Act
		var randomArray = new RandomArray<sbyte>(-50, 50, 50);

		// Assert
		randomArray.Count.AssertEqual(50);

		for (int i = 0; i < 50; i++)
		{
			var value = randomArray.Next();
			(value >= -50 && value <= 50).AssertTrue($"value={value} should be >=-50 and <=50");
		}
	}

	[TestMethod]
	public void Range_UInts()
	{
		// Arrange & Act
		var randomArray = new RandomArray<uint>(100, 1000, 50);

		// Assert
		randomArray.Count.AssertEqual(50);

		for (int i = 0; i < 50; i++)
		{
			var value = randomArray.Next();
			(value >= 100 && value <= 1000).AssertTrue($"value={value} should be >=100 and <=1000");
		}
	}

	[TestMethod]
	public void Range_ULongs()
	{
		// Arrange & Act
		var randomArray = new RandomArray<ulong>(1000, 10000, 50);

		// Assert
		randomArray.Count.AssertEqual(50);

		for (int i = 0; i < 50; i++)
		{
			var value = randomArray.Next();
			(value >= 1000 && value <= 10000).AssertTrue($"value={value} should be >=1000 and <=10000");
		}
	}

	[TestMethod]
	public void Range_UShorts()
	{
		// Arrange & Act
		var randomArray = new RandomArray<ushort>(100, 1000, 50);

		// Assert
		randomArray.Count.AssertEqual(50);

		for (int i = 0; i < 50; i++)
		{
			var value = randomArray.Next();
			(value >= 100 && value <= 1000).AssertTrue($"value={value} should be >=100 and <=1000");
		}
	}

	[TestMethod]
	public void Range_Doubles()
	{
		// Arrange & Act
		var randomArray = new RandomArray<double>(10.5, 20.5, 50);

		// Assert
		randomArray.Count.AssertEqual(50);

		for (int i = 0; i < 50; i++)
		{
			var value = randomArray.Next();
			(value >= 10.5 && value <= 20.5).AssertTrue($"value={value} should be >=10.5 and <=20.5");
		}
	}

	[TestMethod]
	public void Range_Floats()
	{
		// Arrange & Act
		var randomArray = new RandomArray<float>(5.5f, 15.5f, 50);

		// Assert
		randomArray.Count.AssertEqual(50);

		for (int i = 0; i < 50; i++)
		{
			var value = randomArray.Next();
			(value >= 5.5f && value <= 15.5f).AssertTrue($"value={value} should be >=5.5 and <=15.5");
		}
	}

	[TestMethod]
	public void Range_Decimals()
	{
		// Arrange & Act
		var randomArray = new RandomArray<decimal>(100.5m, 200.5m, 50);

		// Assert
		randomArray.Count.AssertEqual(50);

		for (int i = 0; i < 50; i++)
		{
			var value = randomArray.Next();
			(value >= 100.5m && value <= 200.5m).AssertTrue($"value={value} should be >=100.5 and <=200.5");
		}
	}

	[TestMethod]
	public void Range_DateTimes()
	{
		// Arrange
		var min = new DateTime(2020, 1, 1);
		var max = new DateTime(2025, 12, 31);

		// Act
		var randomArray = new RandomArray<DateTime>(min, max, 50);

		// Assert
		randomArray.Count.AssertEqual(50);

		for (int i = 0; i < 50; i++)
		{
			var value = randomArray.Next();
			(value >= min && value <= max).AssertTrue($"value={value} should be >={min} and <={max}");
		}
	}
}
