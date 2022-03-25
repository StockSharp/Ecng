namespace Ecng.Tests.Collections
{
	using System.Threading;
	using System.Threading.Tasks;
	using System.Collections.Generic;

	using Ecng.Collections;
	using Ecng.UnitTesting;

	using Nito.AsyncEx;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class DictTests
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

			(await dict.SafeAddAsync(sync, 1, (k, t) => Task.FromResult(k.ToString()), default)).AssertEqual("1");
			(await dict.SafeAddAsync(sync, 2, (k, t) => Task.FromResult(k.ToString()), default)).AssertEqual("2");
			(await dict.SafeAddAsync(sync, 3, (k, t) => Task.FromResult(k.ToString()), default)).AssertEqual("3");
			(await dict.SafeAddAsync(sync, 2, (k, t) => Task.FromResult(k.ToString()), default)).AssertEqual("2");
		}

		[TestMethod]
		public async Task SafeAddAsync2()
		{
			var sync = new AsyncReaderWriterLock();
			var dict = new Dictionary<int, (TaskCompletionSource<string>, string)>();

			(await dict.SafeAddAsync(sync, 1, (k, t) => Task.FromResult(k.ToString()), default)).AssertEqual("1");
			(await dict.SafeAddAsync(sync, 2, (k, t) => Task.FromResult(k.ToString()), default)).AssertEqual("2");
			(await dict.SafeAddAsync(sync, 3, (k, t) => Task.FromResult(k.ToString()), default)).AssertEqual("3");
			(await dict.SafeAddAsync(sync, 2, (k, t) => Task.FromResult(k.ToString()), default)).AssertEqual("2");
		}
	}
}