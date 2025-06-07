namespace Ecng.Tests.Collections;

[TestClass]
public class SynchronizedSetTests
{
	[TestMethod]
	public void IndexingAndDuplicates()
	{
		var set = new SynchronizedSet<int>(true);
		set.AddRange([1, 2, 3]);
		set[0].AssertEqual(1);
		set.IndexOf(3).AssertEqual(2);
		set.Remove(2).AssertTrue();
		set.ThrowIfDuplicate = true;
		Assert.ThrowsExactly<InvalidOperationException>(() => set.Add(1));
		set.TryAdd(3).AssertFalse();
	}

	[TestMethod]
	public void IndexingDisabled()
	{
		var set = new SynchronizedSet<int>
		{
			1
		};
		Assert.ThrowsExactly<InvalidOperationException>(() => _ = set[0]);
		Assert.ThrowsExactly<InvalidOperationException>(() => set.IndexOf(1));
		Assert.ThrowsExactly<InvalidOperationException>(() => set.RemoveAt(0));
	}

	[TestMethod]
	public void RangeEvents()
	{
		var set = new SynchronizedSet<int>();
		var added = new List<int>();
		var removed = new List<int>();
		set.AddedRange += items => added.AddRange(items);
		set.RemovedRange += items => removed.AddRange(items);
		set.AddRange([1, 2, 3]);
		added.SequenceEqual([1, 2, 3]).AssertTrue();
		set.RemoveRange([1, 3]);
		removed.SequenceEqual([1, 3]).AssertTrue();
	}

	[TestMethod]
	public void UnionIntersectExceptSymmetric()
	{
		var set = new SynchronizedSet<int>();
		set.AddRange([1, 2, 3]);
		set.UnionWith([3, 4]);
		set.OrderBy(t => t).SequenceEqual([1, 2, 3, 4]).AssertTrue();
		set.IntersectWith([2, 4]);
		set.OrderBy(t => t).SequenceEqual([2, 4]).AssertTrue();
		set.ExceptWith([4]);
		set.SequenceEqual([2]).AssertTrue();
		set.SymmetricExceptWith([2, 3]);
		set.OrderBy(t => t).SequenceEqual([3]).AssertTrue();
	}

	[TestMethod]
	public void SetComparisons()
	{
		var set = new SynchronizedSet<int>();
		set.AddRange([1, 2, 3]);
		set.IsSubsetOf([0, 1, 2, 3, 4]).AssertTrue();
		set.IsSupersetOf([1, 2]).AssertTrue();
		set.IsProperSupersetOf([1, 2]).AssertTrue();
		set.IsProperSubsetOf([1, 2, 3, 4]).AssertTrue();
		set.Overlaps([3, 4, 5]).AssertTrue();
		set.SetEquals([3, 2, 1]).AssertTrue();
	}

	[TestMethod]
	public void IndexedRemoveRange()
	{
		var set = new SynchronizedSet<int>(true);
		set.AddRange([1, 2, 3, 4]);
		var removed = set.RemoveRange(1, 2);
		removed.AssertEqual(2);
		set.SequenceEqual([1, 4]).AssertTrue();
	}
}