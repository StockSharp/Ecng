namespace Ecng.Tests.Collections;

[TestClass]
public class SynchronizedSetTests : BaseTestClass
{
	[TestMethod]
	public void IndexingAndDuplicates()
	{
		var set = new SynchronizedSet<int>(true);
		set.AddRange([1, 2, 3]);
		set[0].AssertEqual(1);
		set.IndexOf(3).AssertEqual(2);
		set.Remove(2).AssertTrue();
		set.TryAdd(3).AssertFalse();
		set.ThrowIfDuplicate = true;
		ThrowsExactly<InvalidOperationException>(() => set.Add(1));
		ThrowsExactly<InvalidOperationException>(() => set.TryAdd(1));
	}

	[TestMethod]
	public void IndexingDisabled()
	{
		var set = new SynchronizedSet<int>
		{
			1
		};
		ThrowsExactly<InvalidOperationException>(() => _ = set[0]);
		ThrowsExactly<InvalidOperationException>(() => set.IndexOf(1));
		ThrowsExactly<InvalidOperationException>(() => set.RemoveAt(0));
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
		added.AssertEqual([1, 2, 3]);
		set.RemoveRange([1, 3]);
		removed.AssertEqual([1, 3]);
	}

	[TestMethod]
	public void UnionIntersectExceptSymmetric()
	{
		var set = new SynchronizedSet<int>(true);
		set.AddRange([1, 2, 3]);
		set.UnionWith([3, 4]);
		set.OrderBy(t => t).AssertEqual(new int[] { 1, 2, 3, 4 });
		set.IntersectWith([2, 4]);
		set.OrderBy(t => t).AssertEqual(new int[] { 2, 4 });
		set.ExceptWith([4]);
		set.AssertEqual([2]);
		set.SymmetricExceptWith([2, 3]);
		set.OrderBy(t => t).AssertEqual(new int[] { 3 });
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
		set.AssertEqual([1, 4]);
	}
}