namespace Ecng.Tests.Collections;

using System.Collections.Concurrent;

[TestClass]
public class CollectionHelperTests : BaseTestClass
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

	[TestMethod]
	public void RemoveWhere_RemovesMatchingItems()
	{
		// Arrange
		var collection = new List<int> { 1, 2, 3, 4, 5, 6 };

		// Act
		var removed = collection.RemoveWhere(x => x % 2 == 0).ToArray();

		// Assert
		removed.AssertEqual([2, 4, 6]);
		collection.ToArray().AssertEqual([1, 3, 5]);
	}

	[TestMethod]
	public void RemoveWhere2_RemovesAndCountsItems()
	{
		// Arrange
		var list = new List<int> { 1, 2, 3, 4, 5, 6 };

		// Act
		var count = list.RemoveWhere2(x => x > 3);

		// Assert
		count.AssertEqual(3);
		list.ToArray().AssertEqual([1, 2, 3]);
	}

	[TestMethod]
	public void CopyAndClear_CopiesAndEmptiesCollection()
	{
		// Arrange
		var collection = new List<int> { 1, 2, 3 };

		// Act
		var copy = collection.CopyAndClear();

		// Assert
		copy.AssertEqual([1, 2, 3]);
		collection.Count.AssertEqual(0);
	}

	[TestMethod]
	public void GetAndRemove_GetsValueAndRemovesKey()
	{
		// Arrange
		var dict = new Dictionary<int, string> { { 1, "one" }, { 2, "two" } };

		// Act
		var value = dict.GetAndRemove(1);

		// Assert
		value.AssertEqual("one");
		dict.ContainsKey(1).AssertFalse();
		dict.Count.AssertEqual(1);
	}

	[TestMethod]
	public void TryGetAndRemove_ReturnsValueIfExists()
	{
		// Arrange
		var dict = new Dictionary<int, string> { { 1, "one" }, { 2, "two" } };

		// Act
		var success = dict.TryGetAndRemove(1, out var value);

		// Assert
		success.AssertTrue();
		value.AssertEqual("one");
		dict.ContainsKey(1).AssertFalse();
	}

	[TestMethod]
	public void TryGetAndRemove_ReturnsFalseIfNotExists()
	{
		// Arrange
		var dict = new Dictionary<int, string> { { 1, "one" } };

		// Act
		var success = dict.TryGetAndRemove(5, out var value);

		// Assert
		success.AssertFalse();
		value.AssertNull();
		dict.Count.AssertEqual(1);
	}

	[TestMethod]
	public void SafeAdd_CreatesNewValueIfNotExists()
	{
		// Arrange
		var dict = new Dictionary<int, List<int>>();

		// Act
		var list = dict.SafeAdd(1);

		// Assert
		list.AssertNotNull();
		dict[1].AssertEqual(list);
	}

	[TestMethod]
	public void SafeAdd_ReturnsExistingValueIfExists()
	{
		// Arrange
		var existing = new List<int> { 1, 2, 3 };
		var dict = new Dictionary<int, List<int>> { { 1, existing } };

		// Act
		var list = dict.SafeAdd(1);

		// Assert
		list.AssertEqual(existing);
	}

	[TestMethod]
	public void TryGetValue_ReturnsValueIfExists()
	{
		// Arrange
		var dict = new Dictionary<int, string> { { 1, "one" } };

		// Act
		var value = dict.TryGetValue(1);

		// Assert
		value.AssertEqual("one");
	}

	[TestMethod]
	public void TryGetValue_ReturnsDefaultIfNotExists()
	{
		// Arrange
		var dict = new Dictionary<int, string> { { 1, "one" } };

		// Act
		var value = dict.TryGetValue(5);

		// Assert
		value.AssertNull();
	}

	[TestMethod]
	public void TryDequeue_DequeuesItemIfExists()
	{
		// Arrange
		var queue = new Queue<string>(["one", "two", "three"]);

		// Act
		var value = queue.TryDequeue();

		// Assert
		value.AssertEqual("one");
		queue.Count.AssertEqual(2);
	}

	[TestMethod]
	public void TryDequeue_ReturnsNullIfEmpty()
	{
		// Arrange
		var queue = new Queue<string>();

		// Act
		var value = queue.TryDequeue();

		// Assert
		value.AssertNull();
	}

	[TestMethod]
	public void TryDequeue2_DequeuesValueTypeIfExists()
	{
		// Arrange
		var queue = new Queue<int>([1, 2, 3]);

		// Act
		var value = queue.TryDequeue2();

		// Assert
		value.AssertEqual(1);
		queue.Count.AssertEqual(2);
	}

	[TestMethod]
	public void TryDequeue2_ReturnsNullIfEmpty()
	{
		// Arrange
		var queue = new Queue<int>();

		// Act
		var value = queue.TryDequeue2();

		// Assert
		value.AssertNull();
	}

	[TestMethod]
	public void TryPeek_PeeksItemIfExists()
	{
		// Arrange
		var queue = new Queue<string>(["one", "two", "three"]);

		// Act
		var value = queue.TryPeek();

		// Assert
		value.AssertEqual("one");
		queue.Count.AssertEqual(3);
	}

	[TestMethod]
	public void TryPeek_ReturnsNullIfEmpty()
	{
		// Arrange
		var queue = new Queue<string>();

		// Act
		var value = queue.TryPeek();

		// Assert
		value.AssertNull();
	}

	[TestMethod]
	public void TryPeek2_PeeksValueTypeIfExists()
	{
		// Arrange
		var queue = new Queue<int>([1, 2, 3]);

		// Act
		var value = queue.TryPeek2();

		// Assert
		value.AssertEqual(1);
		queue.Count.AssertEqual(3);
	}

	[TestMethod]
	public void TryPeek2_ReturnsNullIfEmpty()
	{
		// Arrange
		var queue = new Queue<int>();

		// Act
		var value = queue.TryPeek2();

		// Assert
		value.AssertNull();
	}

	[TestMethod]
	public void FirstOr_ReturnsFirstIfExists()
	{
		// Arrange
		var collection = new[] { 1, 2, 3 };

		// Act
		var value = collection.FirstOr(99);

		// Assert
		value.AssertEqual(1);
	}

	[TestMethod]
	public void FirstOr_ReturnsAlternateIfEmpty()
	{
		// Arrange
		var collection = Array.Empty<int>();

		// Act
		var value = collection.FirstOr(99);

		// Assert
		value.AssertEqual(99);
	}

	[TestMethod]
	public void IsEmpty_ReturnsTrueForEmptyCollection()
	{
		// Arrange
		var collection = new List<int>();

		// Act
		var result = collection.IsEmpty();

		// Assert
		result.AssertTrue();
	}

	[TestMethod]
	public void IsEmpty_ReturnsFalseForNonEmptyCollection()
	{
		// Arrange
		var collection = new List<int> { 1 };

		// Act
		var result = collection.IsEmpty();

		// Assert
		result.AssertFalse();
	}

	[TestMethod]
	public void ForEach_ExecutesActionForEachItem()
	{
		// Arrange
		var collection = new[] { 1, 2, 3 };
		var sum = 0;

		// Act
		collection.ForEach(x => sum += x);

		// Assert
		sum.AssertEqual(6);
	}

	[TestMethod]
	public void Batch_SplitsIntoChunks()
	{
		// Arrange
		var collection = new[] { 1, 2, 3, 4, 5, 6, 7 };

		// Act
		var batches = collection.Chunk(3).ToArray();

		// Assert
		batches.Length.AssertEqual(3);
		batches[0].AssertEqual([1, 2, 3]);
		batches[1].AssertEqual([4, 5, 6]);
		batches[2].AssertEqual([7]);
	}

	[TestMethod]
	public void WhereNotNull_FiltersNullValues()
	{
		// Arrange
		var collection = new string[] { "a", null, "b", null, "c" };

		// Act
		var result = collection.WhereNotNull().ToArray();

		// Assert
		result.AssertEqual(["a", "b", "c"]);
	}

	[TestMethod]
	public void ToSet_CreatesHashSet()
	{
		// Arrange
		var collection = new[] { 1, 2, 2, 3, 3, 3 };

		// Act
		var set = collection.ToHashSet();

		// Assert
		set.Count.AssertEqual(3);
		set.SetEquals([1, 2, 3]).AssertTrue();
	}

	[TestMethod]
	public void HasNullItem_ReturnsTrueIfContainsNull()
	{
		// Arrange
		var collection = new string[] { "a", null, "b" };

		// Act
		var result = collection.HasNullItem();

		// Assert
		result.AssertTrue();
	}

	[TestMethod]
	public void HasNullItem_ReturnsFalseIfNoNull()
	{
		// Arrange
		var collection = new[] { "a", "b", "c" };

		// Act
		var result = collection.HasNullItem();

		// Assert
		result.AssertFalse();
	}

	[TestMethod]
	public void OrderByDescending_SortsDescending()
	{
		// Arrange
		var collection = new[] { 5, 2, 8, 1, 9 };

		// Act
		var result = collection.OrderByDescending().ToArray();

		// Assert
		result.AssertEqual([9, 8, 5, 2, 1]);
	}

	[TestMethod]
	public void SelectMany_FlattensNestedCollections()
	{
		// Arrange
		var nested = new[] { [1, 2], [3, 4], new[] { 5 } };

		// Act
		var result = nested.SelectMany().ToArray();

		// Assert
		result.AssertEqual([1, 2, 3, 4, 5]);
	}

	[TestMethod]
	public void LastOr_ReturnsLastIfExists()
	{
		// Arrange
		var collection = new[] { 1, 2, 3 };

		// Act
		var value = collection.LastOr();

		// Assert
		value.AssertEqual(3);
	}

	[TestMethod]
	public void LastOr_ReturnsNullIfEmpty()
	{
		// Arrange
		var collection = Array.Empty<int>();

		// Act
		var value = collection.LastOr();

		// Assert
		value.AssertNull();
	}

	[TestMethod]
	public void ElementAtOr_ReturnsElementIfExists()
	{
		// Arrange
		var collection = new[] { 10, 20, 30 };

		// Act
		var value = collection.ElementAtOr(1);

		// Assert
		value.AssertEqual(20);
	}

	[TestMethod]
	public void ElementAtOr_ReturnsNullIfOutOfRange()
	{
		// Arrange
		var collection = new[] { 10, 20, 30 };

		// Act
		var value = collection.ElementAtOr(10);

		// Assert
		value.AssertNull();
	}

	[TestMethod]
	public void ElementAtFromEnd_ReturnsElementFromEnd()
	{
		// Arrange
		var collection = new[] { 1, 2, 3, 4, 5 };

		// Act
		var value = collection.ElementAtFromEnd(1);

		// Assert
		value.AssertEqual(4);
	}

	[TestMethod]
	public void ElementAtFromEndOrDefault_ReturnsDefaultIfOutOfRange()
	{
		// Arrange
		var collection = new[] { 1, 2, 3 };

		// Act
		var value = collection.ElementAtFromEndOrDefault(10);

		// Assert
		value.AssertEqual(0);
	}

	[TestMethod]
	public void SkipLast_SkipsLastElements()
	{
		// Arrange
		var collection = new[] { 1, 2, 3, 4, 5 };

		// Act
		var result = collection.SkipLast(2).ToArray();

		// Assert
		result.AssertEqual([1, 2, 3]);
	}

	[TestMethod]
	public void SingleWhenOnly_ReturnsSingleElement()
	{
		// Arrange
		var collection = new[] { 42 };

		// Act
		var value = collection.SingleWhenOnly();

		// Assert
		value.AssertEqual(42);
	}

	[TestMethod]
	public void SingleWhenOnly_ReturnsDefaultIfMultiple()
	{
		// Arrange
		var collection = new[] { 1, 2, 3 };

		// Act
		var value = collection.SingleWhenOnly();

		// Assert
		value.AssertEqual(0);
	}

	[TestMethod]
	public void Count2_CountsNonGenericEnumerable()
	{
		// Arrange
		System.Collections.IEnumerable collection = new System.Collections.ArrayList { 1, 2, 3 };

		// Act
		var count = collection.Count2();

		// Assert
		count.AssertEqual(3);
	}

	[TestMethod]
	public void ToTuple_ConvertsPairToTuple()
	{
		// Arrange
		var pair = new KeyValuePair<int, string>(1, "one");

		// Act
		var tuple = pair.ToTuple();

		// Assert
		tuple.Item1.AssertEqual(1);
		tuple.Item2.AssertEqual("one");
	}

	[TestMethod]
	public void ToPair_ConvertsTupleToPair()
	{
		// Arrange
		var tuple = new Tuple<int, string>(1, "one");

		// Act
		var pair = tuple.ToPair();

		// Assert
		pair.Key.AssertEqual(1);
		pair.Value.AssertEqual("one");
	}

	[TestMethod]
	public void ToPair_ConvertsValueTupleToPair()
	{
		// Arrange
		var tuple = (key: 1, value: "one");

		// Act
		var pair = tuple.ToPair();

		// Assert
		pair.Key.AssertEqual(1);
		pair.Value.AssertEqual("one");
	}

	[TestMethod]
	public void ToDictionary_FromTuples()
	{
		// Arrange
		var tuples = new[] { new Tuple<int, string>(1, "one"), new Tuple<int, string>(2, "two") };

		// Act
		var dict = tuples.ToDictionary();

		// Assert
		dict.Count.AssertEqual(2);
		dict[1].AssertEqual("one");
		dict[2].AssertEqual("two");
	}

	[TestMethod]
	public void ToBits_ConvertsIntToBits()
	{
		// Arrange
		var value = 5; // 101 in binary

		// Act
		var bits = value.ToBits(3);

		// Assert
		bits.Length.AssertEqual(3);
		bits[0].AssertTrue();  // bit 0
		bits[1].AssertFalse(); // bit 1
		bits[2].AssertTrue();  // bit 2
	}

	[TestMethod]
	public void FromBits_ConvertsBitsToInt()
	{
		// Arrange
		var bits = new[] { true, false, true }; // 101 in binary = 5

		// Act
		var value = bits.FromBits();

		// Assert
		value.AssertEqual(5);
	}

	[TestMethod]
	public void IsEmpty_WithPredicate_ReturnsTrueIfNoMatch()
	{
		// Arrange
		var collection = new[] { 1, 2, 3 };

		// Act
		var result = collection.IsEmpty(x => x > 10);

		// Assert
		result.AssertTrue();
	}

	[TestMethod]
	public void IsEmpty_WithPredicate_ReturnsFalseIfHasMatch()
	{
		// Arrange
		var collection = new[] { 1, 2, 3 };

		// Act
		var result = collection.IsEmpty(x => x > 2);

		// Assert
		result.AssertFalse();
	}

	[TestMethod]
	public void TryGetValue2_ReturnsValueIfExists()
	{
		// Arrange
		var dict = new Dictionary<int, int> { { 1, 100 } };

		// Act
		var value = dict.TryGetValue2(1);

		// Assert
		value.AssertEqual(100);
	}

	[TestMethod]
	public void TryGetValue2_ReturnsNullIfNotExists()
	{
		// Arrange
		var dict = new Dictionary<int, int> { { 1, 100 } };

		// Act
		var value = dict.TryGetValue2(5);

		// Assert
		value.AssertNull();
	}

	[TestMethod]
	public void TryGetAndRemove2_ReturnsValueIfExists()
	{
		// Arrange
		var dict = new Dictionary<int, int> { { 1, 100 }, { 2, 200 } };

		// Act
		var value = dict.TryGetAndRemove2(1);

		// Assert
		value.AssertEqual(100);
		dict.ContainsKey(1).AssertFalse();
	}

	[TestMethod]
	public void TryGetAndRemove2_ReturnsNullIfNotExists()
	{
		// Arrange
		var dict = new Dictionary<int, int> { { 1, 100 } };

		// Act
		var value = dict.TryGetAndRemove2(5);

		// Assert
		value.AssertNull();
		dict.Count.AssertEqual(1);
	}

	[TestMethod]
	public void SafeAdd_WithHandler_UsesHandler()
	{
		// Arrange
		var dict = new Dictionary<int, string>();

		// Act
		var value = dict.SafeAdd(1, key => $"Value_{key}");

		// Assert
		value.AssertEqual("Value_1");
		dict[1].AssertEqual("Value_1");
	}

	[TestMethod]
	public void SafeAdd_WithHandler_ReturnsExistingValue()
	{
		// Arrange
		var dict = new Dictionary<int, string> { { 1, "existing" } };

		// Act
		var value = dict.SafeAdd(1, key => $"Value_{key}");

		// Assert
		value.AssertEqual("existing");
	}

	[TestMethod]
	public void SafeAdd_WithHandlerAndFlag_SetsIsNewTrue()
	{
		// Arrange
		var dict = new Dictionary<int, string>();

		// Act
		var value = dict.SafeAdd(1, key => $"Value_{key}", out var isNew);

		// Assert
		value.AssertEqual("Value_1");
		isNew.AssertTrue();
	}

	[TestMethod]
	public void SafeAdd_WithHandlerAndFlag_SetsIsNewFalse()
	{
		// Arrange
		var dict = new Dictionary<int, string> { { 1, "existing" } };

		// Act
		var value = dict.SafeAdd(1, key => $"Value_{key}", out var isNew);

		// Assert
		value.AssertEqual("existing");
		isNew.AssertFalse();
	}

	[TestMethod]
	public void GetKeys_FindsKeysForValue()
	{
		// Arrange
		var dict = new Dictionary<int, string> { { 1, "one" }, { 2, "two" }, { 3, "one" } };

		// Act
		var keys = dict.GetKeys("one").ToArray();

		// Assert
		keys.Length.AssertEqual(2);
		keys.Contains(1).AssertTrue();
		keys.Contains(3).AssertTrue();
	}

	[TestMethod]
	public void GetKeys_ReturnsEmptyForNonExistingValue()
	{
		// Arrange
		var dict = new Dictionary<int, string> { { 1, "one" } };

		// Act
		var keys = dict.GetKeys("two").ToArray();

		// Assert
		keys.Length.AssertEqual(0);
	}

	[TestMethod]
	public void ToPairSet_CreatesPairSet()
	{
		// Arrange
		var pairs = new[] { new KeyValuePair<int, string>(1, "one"), new KeyValuePair<int, string>(2, "two") };

		// Act
		var pairSet = pairs.ToPairSet();

		// Assert
		pairSet.AssertNotNull();
		pairSet.Count.AssertEqual(2);
		pairSet[1].AssertEqual("one");
	}

	[TestMethod]
	public void CopyTo_CopiesPairsToDictionary()
	{
		// Arrange
		var source = new[] { new KeyValuePair<int, string>(1, "one"), new KeyValuePair<int, string>(2, "two") };
		var destination = new Dictionary<int, string>();

		// Act
		source.CopyTo(destination);

		// Assert
		destination.Count.AssertEqual(2);
		destination[1].AssertEqual("one");
		destination[2].AssertEqual("two");
	}

	[TestMethod]
	public void SyncGet_AccessesCollectionSafely()
	{
		// Arrange
		var collection = new SynchronizedList<int> { 1, 2, 3 };

		// Act
		var count = collection.SyncGet(c => c.Count);

		// Assert
		count.AssertEqual(3);
	}

	[TestMethod]
	public void SyncDo_ModifiesCollectionSafely()
	{
		// Arrange
		var collection = new SynchronizedList<int> { 1, 2, 3 };

		// Act
		collection.SyncDo(c => c.Add(4));

		// Assert
		collection.Count.AssertEqual(4);
		collection[3].AssertEqual(4);
	}

	[TestMethod]
	public void Sync_CreatesSymchronizedList()
	{
		// Arrange
		IList<int> list = [1, 2, 3];

		// Act
		var syncList = list.Sync();

		// Assert
		syncList.AssertNotNull();
		syncList.Count.AssertEqual(3);
	}

	[TestMethod]
	public void Sync_CreatesSynchronizedDictionary()
	{
		// Arrange
		IDictionary<int, string> dict = new Dictionary<int, string> { { 1, "one" } };

		// Act
		var syncDict = dict.Sync();

		// Assert
		syncDict.AssertNotNull();
		syncDict.Count.AssertEqual(1);
	}

	[TestMethod]
	public void Sync_CreatesSynchronizedSet()
	{
		// Arrange
		var set = new HashSet<int> { 1, 2, 3 };

		// Act
		var syncSet = set.Sync();

		// Assert
		syncSet.AssertNotNull();
		syncSet.Count.AssertEqual(3);
	}

	[TestMethod]
	public void WhereWithPrevious_FiltersWithPreviousElement()
	{
		// Arrange
		var collection = new[] { 1, 3, 2, 5, 4 };

		// Act
		var result = collection.WhereWithPrevious((prev, curr) => curr > prev).ToArray();

		// Assert
		result.AssertEqual([1, 3, 5]);
	}

	[TestMethod]
	public void ToIgnoreCaseSet_CreatesIgnoreCaseSet()
	{
		// Arrange
		var collection = new[] { "Apple", "BANANA", "cherry" };

		// Act
		var set = collection.ToIgnoreCaseSet();

		// Assert
		set.Count.AssertEqual(3);
		set.Contains("apple").AssertTrue();
		set.Contains("APPLE").AssertTrue();
	}

	[TestMethod]
	public void DamerauLevenshteinDistance_CalculatesDistance()
	{
		// Arrange
		var source = new[] { 1, 2, 3, 4 };
		var target = new[] { 1, 3, 2, 4 };

		// Act
		var distance = CollectionHelper.DamerauLevenshteinDistance(source, target, 10);

		// Assert
		distance.AssertGreater(0);
		distance.AssertLess(10);
	}

	[TestMethod]
	public void ToBits_Long_ConvertsToBits()
	{
		// Arrange
		long value = 5L;

		// Act
		var bits = value.ToBits(3);

		// Assert
		bits.Length.AssertEqual(3);
		bits[0].AssertTrue();
		bits[1].AssertFalse();
		bits[2].AssertTrue();
	}

	[TestMethod]
	public void FromBits2_ConvertsBitsToLong()
	{
		// Arrange
		var bits = new[] { true, false, true };

		// Act
		var value = bits.FromBits2();

		// Assert
		value.AssertEqual(5L);
	}

	[TestMethod]
	public void Clear_ClearsConcurrentQueue()
	{
		// Arrange
		var queue = new ConcurrentQueue<int>();
		queue.Enqueue(1);
		queue.Enqueue(2);
		queue.Enqueue(3);

		// Act
		queue.Clear();

		// Assert
		queue.Count.AssertEqual(0);
	}

	[TestMethod]
	public void ToDictionary_WithIndexedSelectors()
	{
		// Arrange
		var source = new[] { "a", "b", "c" };

		// Act
		var dict = source.ToDictionary((s, i) => i, (s, i) => s.ToUpper());

		// Assert
		dict.Count.AssertEqual(3);
		dict[0].AssertEqual("A");
		dict[1].AssertEqual("B");
		dict[2].AssertEqual("C");
	}

	[TestMethod]
	public void AddRange_BitArray_AddsBits()
	{
		// Arrange
		var array = new System.Collections.BitArray(2);
		array[0] = true;
		array[1] = false;

		// Act
		array.AddRange(true, false, true);

		// Assert
		array.Length.AssertEqual(5);
		array[2].AssertTrue();
		array[3].AssertFalse();
		array[4].AssertTrue();
	}

	[TestMethod]
	public void FirstOr_ValueType_ReturnsFirstIfExists()
	{
		// Arrange
		var collection = new[] { 1, 2, 3 };

		// Act
		var value = collection.FirstOr();

		// Assert
		value.AssertEqual(1);
	}

	[TestMethod]
	public void FirstOr_ValueType_ReturnsNullIfEmpty()
	{
		// Arrange
		var collection = Array.Empty<int>();

		// Act
		var value = collection.FirstOr();

		// Assert
		value.AssertNull();
	}

	[TestMethod]
	public void IsEmpty_Array_ReturnsTrueForEmpty()
	{
		// Arrange
		var array = Array.Empty<int>();

		// Act
		var result = array.IsEmpty();

		// Assert
		result.AssertTrue();
	}

	[TestMethod]
	public void IsEmpty_Array_ReturnsFalseForNonEmpty()
	{
		// Arrange
		var array = new[] { 1 };

		// Act
		var result = array.IsEmpty();

		// Assert
		result.AssertFalse();
	}

	[TestMethod]
	public void IsEmpty_String_ReturnsTrueForEmpty()
	{
		// Arrange
		var str = "";

		// Act
		var result = str.IsEmpty();

		// Assert
		result.AssertTrue();
	}

	[TestMethod]
	public void IsEmpty_String_ReturnsFalseForNonEmpty()
	{
		// Arrange
		var str = "hello";

		// Act
		var result = str.IsEmpty();

		// Assert
		result.AssertFalse();
	}

	[TestMethod]
	public async Task SelectManyAsync_FlattensAsync()
	{
		// Arrange
		var source = new[] { 1, 2 };

		var delay = TimeSpan.FromMilliseconds(10);

		// Act
		var result = await source.SelectManyAsync<int, int>(async x =>
		{
			await delay.Delay(CancellationToken);
			return [x, x * 10];
		});

		// Assert
		result.ToArray().AssertEqual([1, 10, 2, 20]);
	}

	[TestMethod]
	public void Permutations_GeneratesPermutations()
	{
		// Arrange
		var keys = new[] { 1, 2 };

		// Act
		var perms = keys.Permutations(k => new[] { k, k * 10 }).ToArray();

		// Assert
		perms.Length.AssertEqual(4);
		perms[0].AssertEqual([1, 2]);
		perms[1].AssertEqual([1, 20]);
		perms[2].AssertEqual([10, 2]);
		perms[3].AssertEqual([10, 20]);
	}

	[TestMethod]
	public void TryDequeue_SynchronizedQueue_DequeuesItemIfExists()
	{
		// Arrange
		var queue = new SynchronizedQueue<string>();
		queue.Enqueue("one");
		queue.Enqueue("two");

		// Act
		var value = queue.TryDequeue();

		// Assert
		value.AssertEqual("one");
		queue.Count.AssertEqual(1);
	}

	[TestMethod]
	public void TryPeek_SynchronizedQueue_PeeksItemIfExists()
	{
		// Arrange
		var queue = new SynchronizedQueue<string>();
		queue.Enqueue("one");
		queue.Enqueue("two");

		// Act
		var value = queue.TryPeek();

		// Assert
		value.AssertEqual("one");
		queue.Count.AssertEqual(2);
	}

	[TestMethod]
	public void TryDequeue2_SynchronizedQueue_DequeuesValueType()
	{
		// Arrange
		var queue = new SynchronizedQueue<int>();
		queue.Enqueue(1);
		queue.Enqueue(2);
		queue.Enqueue(3);

		// Act
		var value = queue.TryDequeue2();

		// Assert
		value.AssertEqual(1);
		queue.Count.AssertEqual(2);
	}

	[TestMethod]
	public void TryPeek2_SynchronizedQueue_PeeksValueType()
	{
		// Arrange
		var queue = new SynchronizedQueue<int>();
		queue.Enqueue(1);
		queue.Enqueue(2);
		queue.Enqueue(3);

		// Act
		var value = queue.TryPeek2();

		// Assert
		value.AssertEqual(1);
		queue.Count.AssertEqual(3);
	}

	[TestMethod]
	public void GetKeys_SynchronizedDictionary_FindsKeys()
	{
		// Arrange
		var dict = new SynchronizedDictionary<int, string> { { 1, "one" }, { 2, "one" } };

		// Act
		var keys = dict.GetKeys("one").ToArray();

		// Assert
		keys.Length.AssertEqual(2);
		keys.Contains(1).AssertTrue();
		keys.Contains(2).AssertTrue();
	}

	[TestMethod]
	public void TryGetKey_PairSet_ReturnsKeyIfExists()
	{
		// Arrange
		var pairSet = new PairSet<int, string> { { 1, "one" }, { 2, "two" } };

		// Act
		var key = pairSet.TryGetKey("one");

		// Assert
		key.AssertEqual(1);
	}

	[TestMethod]
	public void TryGetKey_PairSet_ReturnsDefaultIfNotExists()
	{
		// Arrange
		var pairSet = new PairSet<int, string> { { 1, "one" } };

		// Act
		var key = pairSet.TryGetKey("three");

		// Assert
		key.AssertEqual(0);
	}

	[TestMethod]
	public void TryGetKey2_PairSet_ReturnsNullIfNotExists()
	{
		// Arrange
		var pairSet = new PairSet<int, string> { { 1, "one" } };

		// Act
		var key = pairSet.TryGetKey2("three");

		// Assert
		key.AssertNull();
	}

	[TestMethod]
	public void ToDictionary_FromGrouping_CreatesDict()
	{
		// Arrange
		var items = new[] { 1, 2, 3, 4, 5 };
		var grouping = items.GroupBy(x => x % 2);

		// Act
		var dict = grouping.ToDictionary();

		// Assert
		dict.Count.AssertEqual(2);
		dict[0].Count().AssertEqual(2); // even numbers
		dict[1].Count().AssertEqual(3); // odd numbers
	}

	[TestMethod]
	public void ToBits_Float_ConvertsToBits()
	{
		// Arrange
		float value = 5.0f;

		// Act
		var bits = value.ToBits(8);

		// Assert
		bits.Length.AssertEqual(8);
		bits.AssertNotNull();
	}

	[TestMethod]
	public void ToBits_Double_ConvertsToBits()
	{
		// Arrange
		double value = 5.0;

		// Act
		var bits = value.ToBits(8);

		// Assert
		bits.Length.AssertEqual(8);
		bits.AssertNotNull();
	}

	[TestMethod]
	public void ToBits_IntWithStartBit_ConvertsToBits()
	{
		// Arrange
		var value = 15; // 1111 in binary

		// Act
		var bits = value.ToBits(1, 3); // Get bits 1-3

		// Assert
		bits.Length.AssertEqual(3);
		bits[0].AssertTrue();  // bit 1
		bits[1].AssertTrue();  // bit 2
		bits[2].AssertTrue();  // bit 3
	}

	[TestMethod]
	public void FromBits_WithStartBit_ConvertsBitsToInt()
	{
		// Arrange
		var bits = new[] { true, false, true, false, true };

		// Act
		var value = bits.FromBits(1);

		// Assert
		value.AssertGreater(0); // Verify it works and returns a value
	}

	[TestMethod]
	public void FromBits2_WithStartBit_ConvertsBitsToLong()
	{
		// Arrange
		var bits = new[] { true, false, true, false, true };

		// Act
		var value = bits.FromBits2(1);

		// Assert
		value.AssertGreater(0L); // Verify it works and returns a value
	}

	[TestMethod]
	public void SafeAdd_WithDefaultActivator_CreatesNewValue()
	{
		// Arrange
		var dict = new Dictionary<int, List<int>>();

		// Act
		var value = dict.SafeAdd(1, out var isNew);

		// Assert
		value.AssertNotNull();
		isNew.AssertTrue();
		dict[1].AssertEqual(value);
	}

	[TestMethod]
	public void ToPairSet_WithComparer_CreatesPairSet()
	{
		// Arrange
		var pairs = new[] { new KeyValuePair<string, int>("one", 1), new KeyValuePair<string, int>("two", 2) };

		// Act
		var pairSet = pairs.ToPairSet(StringComparer.OrdinalIgnoreCase);

		// Assert
		pairSet.Count.AssertEqual(2);
		pairSet["ONE"].AssertEqual(1); // case-insensitive lookup
		pairSet["TWO"].AssertEqual(2);
	}

	[TestMethod]
	public void ToPairSet_WithIndexedSelectors_CreatesPairSet()
	{
		// Arrange
		var items = new[] { "a", "b", "c" };

		// Act
		var pairSet = items.ToPairSet((s, i) => i, (s, i) => s.ToUpper());

		// Assert
		pairSet.Count.AssertEqual(3);
		pairSet[0].AssertEqual("A");
		pairSet[1].AssertEqual("B");
		pairSet[2].AssertEqual("C");
	}

	[TestMethod]
	public void TypedAs_ConvertsNonGenericDictionary()
	{
		// Arrange
		System.Collections.IDictionary dict = new System.Collections.Hashtable
		{
			{ 1, "one" },
			{ 2, "two" }
		};

		// Act
		var typedDict = dict.TypedAs<int, string>();

		// Assert
		typedDict.AssertNotNull();
		typedDict[1].AssertEqual("one");
		typedDict[2].AssertEqual("two");
	}

	[TestMethod]
	public void OrderBy_WithoutComparer_SortsAscending()
	{
		// Arrange
		var collection = new[] { 5, 2, 8, 1, 9 };

		// Act
		var result = collection.OrderBy().ToArray();

		// Assert
		result.AssertEqual([1, 2, 5, 8, 9]);
	}

	[TestMethod]
	public void ToDictionary_WithComparer_CreatesDict()
	{
		// Arrange
		var pairs = new[] { new KeyValuePair<string, int>("one", 1), new KeyValuePair<string, int>("two", 2) };

		// Act
		var dict = CollectionHelper.ToDictionary(pairs, StringComparer.OrdinalIgnoreCase);

		// Assert
		dict.Count.AssertEqual(2);
		dict["ONE"].AssertEqual(1);
		dict["TWO"].AssertEqual(2);
	}

	#region DuckTyping Tests

	[TestMethod]
	public void DuckTypingCollection_Add_ConvertsAndAddsToSource()
	{
		// Arrange
		var numbers = new List<int> { 1, 2, 3 };
		var strings = numbers.AsDuckTypedCollection(
			n => n.ToString(),
			s => int.Parse(s)
		);

		// Act
		strings.Add("42");

		// Assert
		numbers.Count.AssertEqual(4);
		numbers[3].AssertEqual(42);
	}

	[TestMethod]
	public void DuckTypingCollection_Remove_ConvertsAndRemovesFromSource()
	{
		// Arrange
		var numbers = new List<int> { 1, 2, 3 };
		var strings = numbers.AsDuckTypedCollection(
			n => n.ToString(),
			s => int.Parse(s)
		);

		// Act
		var result = strings.Remove("2");

		// Assert
		result.AssertTrue();
		numbers.Count.AssertEqual(2);
		numbers.Contains(2).AssertFalse();
	}

	[TestMethod]
	public void DuckTypingCollection_Contains_ConvertsAndChecks()
	{
		// Arrange
		var numbers = new List<int> { 1, 2, 3 };
		var strings = numbers.AsDuckTypedCollection(
			n => n.ToString(),
			s => int.Parse(s)
		);

		// Act & Assert
		strings.Contains("2").AssertTrue();
		strings.Contains("42").AssertFalse();
	}

	[TestMethod]
	public void DuckTypingCollection_Clear_ClearsSource()
	{
		// Arrange
		var numbers = new List<int> { 1, 2, 3 };
		var strings = numbers.AsDuckTypedCollection(
			n => n.ToString(),
			s => int.Parse(s)
		);

		// Act
		strings.Clear();

		// Assert
		numbers.Count.AssertEqual(0);
		strings.Count.AssertEqual(0);
	}

	[TestMethod]
	public void DuckTypingCollection_Count_ReflectsSourceCount()
	{
		// Arrange
		var numbers = new List<int> { 1, 2, 3 };
		var strings = numbers.AsDuckTypedCollection(
			n => n.ToString(),
			s => int.Parse(s)
		);

		// Assert
		strings.Count.AssertEqual(3);
	}

	[TestMethod]
	public void DuckTypingCollection_IsReadOnly_ReflectsSourceReadOnly()
	{
		// Arrange
		var numbers = new List<int> { 1, 2, 3 };
		var strings = numbers.AsDuckTypedCollection(
			n => n.ToString(),
			s => int.Parse(s)
		);

		// Assert
		strings.IsReadOnly.AssertFalse();
	}

	[TestMethod]
	public void DuckTypingCollection_Enumeration_ConvertsElements()
	{
		// Arrange
		var numbers = new List<int> { 1, 2, 3 };
		var strings = numbers.AsDuckTypedCollection(
			n => n.ToString(),
			s => int.Parse(s)
		);

		// Act
		var result = strings.ToList();

		// Assert
		result.Count.AssertEqual(3);
		result[0].AssertEqual("1");
		result[1].AssertEqual("2");
		result[2].AssertEqual("3");
	}

	[TestMethod]
	public void DuckTypingCollection_CopyTo_ConvertsAndCopies()
	{
		// Arrange
		var numbers = new List<int> { 1, 2, 3 };
		var strings = numbers.AsDuckTypedCollection(
			n => n.ToString(),
			s => int.Parse(s)
		);
		var array = new string[5];

		// Act
		strings.CopyTo(array, 1);

		// Assert
		array[0].AssertNull();
		array[1].AssertEqual("1");
		array[2].AssertEqual("2");
		array[3].AssertEqual("3");
		array[4].AssertNull();
	}

	[TestMethod]
	public void DuckTypingCollection_ComplexTypeConversion_Works()
	{
		// Arrange
		var people = new List<(string Name, int Age)>
		{
			("Alice", 30),
			("Bob", 25),
			("Charlie", 35)
		};

		var nameAges = people.AsDuckTypedCollection(
			p => $"{p.Name}:{p.Age}",
			s =>
			{
				var parts = s.Split(':');
				return (parts[0], int.Parse(parts[1]));
			}
		);

		// Act
		nameAges.Add("David:40");
		var hasAlice = nameAges.Contains("Alice:30");

		// Assert
		people.Count.AssertEqual(4);
		people[3].Name.AssertEqual("David");
		people[3].Age.AssertEqual(40);
		hasAlice.AssertTrue();
	}

	[TestMethod]
	public void DuckTypingCollection_NullSource_ThrowsException()
	{
		// Act & Assert
		ThrowsExactly<ArgumentNullException>(() =>
		{
			CollectionHelper.AsDuckTypedCollection<int, string>(
				null,
				n => n.ToString(),
				s => int.Parse(s)
			);
		});
	}

	[TestMethod]
	public void DuckTypingCollection_NullSourceToTarget_ThrowsException()
	{
		// Arrange
		var numbers = new List<int>();

		// Act & Assert
		ThrowsExactly<ArgumentNullException>(() =>
		{
			numbers.AsDuckTypedCollection<int, string>(
				null,
				s => int.Parse(s)
			);
		});
	}

	[TestMethod]
	public void DuckTypingCollection_NullTargetToSource_ThrowsException()
	{
		// Arrange
		var numbers = new List<int>();

		// Act & Assert
		ThrowsExactly<ArgumentNullException>(() =>
		{
			numbers.AsDuckTypedCollection(
				n => n.ToString(),
				null
			);
		});
	}

	#endregion
}