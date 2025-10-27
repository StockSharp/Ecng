namespace Ecng.Tests.Collections;

using Nito.AsyncEx;

[TestClass]
public class DictTests : BaseTestClass
{
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
}