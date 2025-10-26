namespace Ecng.Tests.Collections;

[TestClass]
public class CollectionHelperTests
{
	[TestMethod]
	public void GetHashCodeEx_Distribution_NotCollapsed()
	{
		// Arrange: many different sequences should yield many different hashes
		var sequences = Enumerable.Range(0, 200).Select(i => new[] { i });

		// Act
		var hashes = sequences.Select(s => s.GetHashCodeEx()).ToArray();
		var distinct = new HashSet<int>(hashes).Count;

		// Assert: with a proper hash we expect substantially more distinct values
		distinct.AssertGreater(50);
	}

	[TestMethod]
	public void GetHashCodeEx_ConsidersAllPositions()
	{
		// Arrange: two arrays differing only at index31 must produce different hashes
		var a = Enumerable.Range(0, 40).ToArray();
		var b = a.ToArray();
		b[31] = a[31] + 1; // index31 is currently masked out by (31 ^ index) ==0

		// Act
		var ha = a.GetHashCodeEx();
		var hb = b.GetHashCodeEx();

		// Assert
		ha.AssertNotEqual(hb);
	}

	[TestMethod]
	public void ToComparer_FuncBool_CreatesValidComparer()
	{
		// Arrange
		Func<int, int, bool> equalityFunc = (a, b) => a == b;

		// Act
		var comparer = equalityFunc.ToComparer();

		// Assert
		comparer.AssertNotNull();
		comparer.Equals(5, 5).AssertTrue();
		comparer.Equals(5, 10).AssertFalse();
	}

	[TestMethod]
	public void ToComparer_FuncInt_CreatesValidComparer()
	{
		// Arrange
		Func<int, int, int> compareFunc = (a, b) => a.CompareTo(b);

		// Act
		var comparer = compareFunc.ToComparer();

		// Assert
		comparer.AssertNotNull();
		comparer.Compare(5, 10).AssertLess(0);
		comparer.Compare(10, 5).AssertGreater(0);
		comparer.Compare(5, 5).AssertEqual(0);
	}

	[TestMethod]
	public void ToComparer_Comparison_CreatesValidComparer()
	{
		// Arrange
		Comparison<string> comparison = (a, b) => string.Compare(a, b, StringComparison.Ordinal);

		// Act
		var comparer = comparison.ToComparer();

		// Assert
		comparer.AssertNotNull();
		comparer.Compare("apple", "banana").AssertLess(0);
		comparer.Compare("banana", "apple").AssertGreater(0);
		comparer.Compare("apple", "apple").AssertEqual(0);
	}

	[TestMethod]
	public void ToFunc_Comparison_ConvertsToFunc()
	{
		// Arrange
		Comparison<int> comparison = (a, b) => a.CompareTo(b);

		// Act
		var func = comparison.ToFunc();

		// Assert
		func.AssertNotNull();
		func(5, 10).AssertLess(0);
		func(10, 5).AssertGreater(0);
		func(5, 5).AssertEqual(0);
	}

	[TestMethod]
	public void SequenceEqual_CustomComparer_ComparesCorrectly()
	{
		// Arrange
		var first = new[] { 1, 2, 3 };
		var second = new[] { 1, 2, 3 };
		var third = new[] { 1, 2, 4 };
		static bool comparer(int a, int b) => a == b;

		// Act & Assert
		first.SequenceEqual(second, comparer).AssertTrue();
		first.SequenceEqual(third, comparer).AssertFalse();
	}

	[TestMethod]
	public void OrderBy_Comparison_SortsCorrectly()
	{
		// Arrange
		var collection = new[] { 5, 2, 8, 1, 9 };
		static int comparison(int a, int b) => a.CompareTo(b);

		// Act
		var result = collection.OrderBy(comparison).ToArray();

		// Assert
		result.AssertEqual([1, 2, 5, 8, 9]);
	}

	[TestMethod]
	public void IndexOf_FindsFirstMatch()
	{
		// Arrange
		var collection = new[] { 1, 2, 3, 4, 5, 3, 6 };

		// Act
		var index = collection.IndexOf(x => x == 3);

		// Assert
		index.AssertEqual(2);
	}

	[TestMethod]
	public void IndexOf_NoMatch_ReturnsMinusOne()
	{
		// Arrange
		var collection = new[] { 1, 2, 3, 4, 5 };

		// Act
		var index = collection.IndexOf(x => x == 10);

		// Assert
		index.AssertEqual(-1);
	}

	[TestMethod]
	public void TryAdd_Collection_AddsOnlyNew()
	{
		// Arrange
		var collection = new List<int> { 1, 2, 3 };
		var values = new[] { 3, 4, 5 };

		// Act
		collection.TryAdd(values);

		// Assert
		collection.Count.AssertEqual(5);
		collection.ToArray().AssertEqual([1, 2, 3, 4, 5]);
	}

	[TestMethod]
	public void TryAdd_SingleValue_ReturnsTrueWhenAdded()
	{
		// Arrange
		var collection = new List<int> { 1, 2, 3 };

		// Act
		var result = collection.TryAdd(4);

		// Assert
		result.AssertTrue();
		collection.Count.AssertEqual(4);
		collection.Contains(4).AssertTrue();
	}

	[TestMethod]
	public void TryAdd_SingleValue_ReturnsFalseWhenExists()
	{
		// Arrange
		var collection = new List<int> { 1, 2, 3 };

		// Act
		var result = collection.TryAdd(2);

		// Assert
		result.AssertFalse();
		collection.Count.AssertEqual(3);
	}

	[TestMethod]
	public void TryAdd2_Dictionary_AddsOnlyNew()
	{
		// Arrange
		var dict = new Dictionary<int, string> { { 1, "one" }, { 2, "two" } };

		// Act
		var result1 = dict.TryAdd2(3, "three");
		var result2 = dict.TryAdd2(2, "TWO");

		// Assert
		result1.AssertTrue();
		result2.AssertFalse();
		dict.Count.AssertEqual(3);
		dict[2].AssertEqual("two"); // не изменилось
	}

	[TestMethod]
	public void AddRange_List_AddsAllItems()
	{
		// Arrange
		var list = new List<int> { 1, 2, 3 };
		var items = new[] { 4, 5, 6 };

		// Act
		list.AddRange(items);

		// Assert
		list.Count.AssertEqual(6);
		list.ToArray().AssertEqual([1, 2, 3, 4, 5, 6]);
	}

	[TestMethod]
	public void AddRange_Set_AddsOnlyUnique()
	{
		// Arrange
		var set = new HashSet<int> { 1, 2, 3 };
		var items = new[] { 3, 4, 5 };

		// Act
		set.AddRange(items);

		// Assert
		set.Count.AssertEqual(5);
		set.SetEquals([1, 2, 3, 4, 5]).AssertTrue();
	}

	[TestMethod]
	public void RemoveRange_RemovesSpecifiedItems()
	{
		// Arrange
		var list = new List<int> { 1, 2, 3, 4, 5 };
		var itemsToRemove = new[] { 2, 4 };

		// Act
		list.RemoveRange(itemsToRemove);

		// Assert
		list.Count.AssertEqual(3);
		list.ToArray().AssertEqual([1, 3, 5]);
	}

	[TestMethod]
	public void ConcatEx_CombinesTwoCollections()
	{
		// Arrange
		var first = new List<int> { 1, 2, 3 };
		var second = new List<int> { 4, 5, 6 };

		// Act
		var result = first.ConcatEx<List<int>, int>(second);

		// Assert
		result.Count.AssertEqual(6);
		result.ToArray().AssertEqual([1, 2, 3, 4, 5, 6]);
	}
}