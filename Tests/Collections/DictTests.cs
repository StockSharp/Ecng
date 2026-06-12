namespace Ecng.Tests.Collections;

[TestClass]
public class DictTests : BaseTestClass
{
	private sealed class TestKeyedCollection : KeyedCollection<string, int>
	{
	}

	[TestMethod]
	public void Tuples()
	{
		var dict = new Dictionary<string, int>();
		(string name, int value) t = ("123", 123);
		dict.Add(t);

		foreach (var (name, value) in dict)
		{
			name.AssertEqual(t.name);
			value.AssertEqual(t.value);
		}
	}

	[TestMethod]
	public async Task SafeAddAsync()
	{
		var sync = new AsyncReaderWriterLock();
		var dict = new Dictionary<int, string> { { 1, "1" } };

		(await dict.SafeAddAsync(sync, 1, (k, t) => k.ToString().FromResult(), CancellationToken)).AssertEqual("1");
		(await dict.SafeAddAsync(sync, 2, (k, t) => k.ToString().FromResult(), CancellationToken)).AssertEqual("2");
		(await dict.SafeAddAsync(sync, 3, (k, t) => k.ToString().FromResult(), CancellationToken)).AssertEqual("3");
		(await dict.SafeAddAsync(sync, 2, (k, t) => k.ToString().FromResult(), CancellationToken)).AssertEqual("2");
	}

	[TestMethod]
	public async Task SafeAddAsync2()
	{
		var sync = new AsyncReaderWriterLock();
		var dict = new Dictionary<int, TaskCompletionSource<string>>();

		(await dict.SafeAddAsync(sync, 1, (k, t) => k.ToString().FromResult(), CancellationToken)).AssertEqual("1");
		(await dict.SafeAddAsync(sync, 2, (k, t) => k.ToString().FromResult(), CancellationToken)).AssertEqual("2");
		(await dict.SafeAddAsync(sync, 3, (k, t) => k.ToString().FromResult(), CancellationToken)).AssertEqual("3");
		(await dict.SafeAddAsync(sync, 2, (k, t) => k.ToString().FromResult(), CancellationToken)).AssertEqual("2");
	}

	[TestMethod]
	public async Task SafeAddAsync3()
	{
		var dict = new Dictionary<int, TaskCompletionSource<string>>();

		(await dict.SafeAddAsync(1, (k, t) => k.ToString().FromResult(), CancellationToken)).AssertEqual("1");
		(await dict.SafeAddAsync(2, (k, t) => k.ToString().FromResult(), CancellationToken)).AssertEqual("2");
		(await dict.SafeAddAsync(3, (k, t) => k.ToString().FromResult(), CancellationToken)).AssertEqual("3");
		(await dict.SafeAddAsync(2, (k, t) => k.ToString().FromResult(), CancellationToken)).AssertEqual("2");
	}

	[TestMethod]
	public void KeyedCollection_KeyValueContainsChecksValue()
	{
		var collection = new TestKeyedCollection { { "key", 1 } };

		((ICollection<KeyValuePair<string, int>>)collection)
			.Contains(new KeyValuePair<string, int>("key", 1))
			.AssertTrue();

		((ICollection<KeyValuePair<string, int>>)collection)
			.Contains(new KeyValuePair<string, int>("key", 2))
			.AssertFalse();

		((ICollection<KeyValuePair<string, int>>)collection)
			.Contains(new KeyValuePair<string, int>("missing", 1))
			.AssertFalse();
	}

	[TestMethod]
	public void KeyedCollection_KeyValueRemoveChecksValue()
	{
		var collection = new TestKeyedCollection { { "key", 1 } };

		((ICollection<KeyValuePair<string, int>>)collection)
			.Remove(new KeyValuePair<string, int>("key", 2))
			.AssertFalse();

		collection.ContainsKey("key").AssertTrue();

		((ICollection<KeyValuePair<string, int>>)collection)
			.Remove(new KeyValuePair<string, int>("key", 1))
			.AssertTrue();

		collection.ContainsKey("key").AssertFalse();
	}
}
