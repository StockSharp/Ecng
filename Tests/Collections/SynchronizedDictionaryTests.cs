namespace Ecng.Tests.Collections;

[TestClass]
public class SynchronizedDictionaryTests
{
	[TestMethod]
	public void BasicOperations()
	{
		var dict = new SynchronizedDictionary<int, string>();
		dict.Add(1, "A");
		dict[2] = "B";
		dict.Count.AssertEqual(2);
		dict.ContainsKey(1).AssertTrue();
		dict[1].AssertEqual("A");
		dict.Remove(2).AssertTrue();
		dict.Count.AssertEqual(1);
	}

	[TestMethod]
	public void TryGetAndClear()
	{
		var dict = new SynchronizedDictionary<int, string>();
		dict.Add(1, "A");
		dict.TryGetValue(1, out var v).AssertTrue();
		v.AssertEqual("A");
		dict.Clear();
		dict.Count.AssertEqual(0);
	}

	[TestMethod]
	public void Enumeration()
	{
		var dict = new SynchronizedDictionary<int, string>();
		dict.Add(1, "A");
		dict.Add(2, "B");
		var items = dict.ToArray();
		items.Length.AssertEqual(2);
		items.Any(p => p.Key == 1 && p.Value == "A").AssertTrue();
		items.Any(p => p.Key == 2 && p.Value == "B").AssertTrue();
	}
}
