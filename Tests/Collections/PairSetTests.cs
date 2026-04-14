namespace Ecng.Tests.Collections;

[TestClass]
public class PairSetTests : BaseTestClass
{
	[TestMethod]
	public void BasicAddAndLookup()
	{
		var set = new PairSet<string, int>
		{
			{ "one", 1 },
			{ "two", 2 }
		};

		set["one"].AssertEqual(1);
		set[1].AssertEqual("one");
		set["two"].AssertEqual(2);
		set[2].AssertEqual("two");
	}

	[TestMethod]
	public void SetValue_UpdatesReverseLookup()
	{
		// This test verifies that when setting a new value for an existing key,
		// the old value→key mapping is removed from the reverse lookup.
		// Without the fix, the old value would still map to the key.
		var set = new PairSet<string, int>
		{
			{ "key", 100 }
		};

		// Verify initial state
		set["key"].AssertEqual(100);
		set[100].AssertEqual("key");

		// Update the value for existing key
		set["key"] = 200;

		// New value should map correctly
		set["key"].AssertEqual(200);
		set[200].AssertEqual("key");

		// Old value should no longer be in reverse lookup
		// With the bug, this would return "key" instead of throwing
		ThrowsExactly<KeyNotFoundException>(() => _ = set[100]);
	}

	[TestMethod]
	public void SetValue_MultipleUpdates()
	{
		var set = new PairSet<string, int>
		{
			{ "key", 1 }
		};

		set["key"] = 2;
		set[2].AssertEqual("key");
		ThrowsExactly<KeyNotFoundException>(() => _ = set[1]);

		set["key"] = 3;
		set[3].AssertEqual("key");
		ThrowsExactly<KeyNotFoundException>(() => _ = set[2]);
	}

	[TestMethod]
	public void TryGetKey_Works()
	{
		var set = new PairSet<string, int>
		{
			{ "one", 1 }
		};

		set.TryGetKey(1, out var key).AssertTrue();
		key.AssertEqual("one");

		set.TryGetKey(999, out _).AssertFalse();
	}

	[TestMethod]
	public void TryGetValue_Works()
	{
		var set = new PairSet<string, int>
		{
			{ "one", 1 }
		};

		set.TryGetValue("one", out var value).AssertTrue();
		value.AssertEqual(1);

		set.TryGetValue("missing", out _).AssertFalse();
	}

	[TestMethod]
	public void ContainsKey_ContainsValue()
	{
		var set = new PairSet<string, int>
		{
			{ "one", 1 }
		};

		set.ContainsKey("one").AssertTrue();
		set.ContainsKey("two").AssertFalse();
		set.ContainsValue(1).AssertTrue();
		set.ContainsValue(2).AssertFalse();
	}

	[TestMethod]
	public void Remove_RemovesBothMappings()
	{
		var set = new PairSet<string, int>
		{
			{ "one", 1 }
		};

		set.Remove("one").AssertTrue();

		set.ContainsKey("one").AssertFalse();
		set.ContainsValue(1).AssertFalse();
	}

	[TestMethod]
	public void Clear_ClearsBothMappings()
	{
		var set = new PairSet<string, int>
		{
			{ "one", 1 },
			{ "two", 2 }
		};

		set.Clear();

		set.Count.AssertEqual(0);
		set.ContainsValue(1).AssertFalse();
		set.ContainsValue(2).AssertFalse();
	}

	#region Bijective Property Tests

	/// <summary>
	/// Verifies that bijective property is maintained when reassigning values.
	/// For any key k: set[set[k]] == k
	/// </summary>
	[TestMethod]
	public void SetValue_ShouldMaintainBijectiveProperty()
	{
		var set = new PairSet<string, int>
		{
			{ "one", 1 },
			{ "two", 2 }
		};

		set["one"] = 2;

		var valueOfTwo = set["two"];
		var keyForValueOfTwo = set[valueOfTwo];
		keyForValueOfTwo.AssertEqual("two", "Bijective property violated: set[set[\"two\"]] != \"two\"");
	}

	/// <summary>
	/// Verifies that old value is removed from reverse lookup after update.
	/// </summary>
	[TestMethod]
	public void SynchronizedPairSet_SetValue_ShouldRemoveOldValueFromReverseLookup()
	{
		var set = new SynchronizedPairSet<string, int>
		{
			{ "key", 100 }
		};

		set["key"] = 200;

		set["key"].AssertEqual(200);
		set[200].AssertEqual("key");

		ThrowsExactly<KeyNotFoundException>(() => _ = set[100],
			"Old value 100 should be removed from reverse lookup after update");
	}

	/// <summary>
	/// Verifies that SynchronizedPairSet maintains bijective property when reassigning values.
	/// </summary>
	[TestMethod]
	public void SynchronizedPairSet_SetValue_ShouldMaintainBijectiveProperty()
	{
		var set = new SynchronizedPairSet<string, int>
		{
			{ "one", 1 },
			{ "two", 2 }
		};

		set["one"] = 2;

		var valueOfTwo = set["two"];
		var keyForValueOfTwo = set[valueOfTwo];
		keyForValueOfTwo.AssertEqual("two", "Bijective property violated: set[set[\"two\"]] != \"two\"");
	}

	#endregion

	#region SynchronizedPairSet.SetKey Tests

	/// <summary>
	/// Verifies that SynchronizedPairSet.SetKey moves a value to a new key,
	/// removing the old key association.
	/// </summary>
	[TestMethod]
	public void SynchronizedPairSet_SetKey_ShouldMoveValueToNewKey()
	{
		var set = new SynchronizedPairSet<int, string>();
		set.Add(1, "a");
		set.Add(2, "b");

		// Move value "a" from key 1 to key 3
		set.SetKey("a", 3);

		set[3].AssertEqual("a", "New key 3 should map to value 'a'");
		set["a"].AssertEqual(3, "Value 'a' should reverse-map to key 3");
		set.ContainsKey(1).AssertFalse("Old key 1 should be removed");
		set[2].AssertEqual("b", "Other mappings should be unaffected");
	}

	/// <summary>
	/// Verifies that SetKey does not throw when the value is already mapped to another key.
	/// </summary>
	[TestMethod]
	public void SynchronizedPairSet_SetKey_ShouldNotThrowWhenValueExists()
	{
		var set = new SynchronizedPairSet<int, string>();
		set.Add(1, "a");

		// Moving value "a" to a new key should not throw
		set.SetKey("a", 2);

		set[2].AssertEqual("a");
		set["a"].AssertEqual(2);
	}

	#endregion
}
