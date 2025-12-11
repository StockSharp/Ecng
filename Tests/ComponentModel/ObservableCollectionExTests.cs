namespace Ecng.Tests.ComponentModel;

using System.Collections.Specialized;

using Ecng.ComponentModel;

[TestClass]
public class ObservableCollectionExTests : BaseTestClass
{
	[TestMethod]
	public void Add_RaisesCollectionChanged()
	{
		var collection = new ObservableCollectionEx<int>();
		var changedArgs = new List<NotifyCollectionChangedEventArgs>();
		collection.CollectionChanged += (_, args) => changedArgs.Add(args);

		collection.Add(1);

		changedArgs.Count.AssertEqual(1);
		changedArgs[0].Action.AssertEqual(NotifyCollectionChangedAction.Add);
		changedArgs[0].NewItems[0].AssertEqual(1);
		changedArgs[0].NewStartingIndex.AssertEqual(0);
	}

	[TestMethod]
	public void Add_RaisesPropertyChanged()
	{
		var collection = new ObservableCollectionEx<int>();
		var propertyNames = new List<string>();
		collection.PropertyChanged += (_, args) => propertyNames.Add(args.PropertyName);

		collection.Add(1);

		propertyNames.Contains("Count").AssertTrue();
		propertyNames.Contains("Item[]").AssertTrue();
	}

	[TestMethod]
	public void Add_RaisesAddedRange()
	{
		var collection = new ObservableCollectionEx<int>();
		var addedItems = new List<int>();
		collection.AddedRange += items => addedItems.AddRange(items);

		collection.Add(42);

		addedItems.Count.AssertEqual(1);
		addedItems[0].AssertEqual(42);
	}

	[TestMethod]
	public void AddRange_AddsMultipleItems()
	{
		var collection = new ObservableCollectionEx<int>();

		collection.AddRange([1, 2, 3]);

		collection.Count.AssertEqual(3);
		collection[0].AssertEqual(1);
		collection[1].AssertEqual(2);
		collection[2].AssertEqual(3);
	}

	[TestMethod]
	public void AddRange_EmptyCollection_NoEvent()
	{
		var collection = new ObservableCollectionEx<int>();
		var eventCount = 0;
		collection.CollectionChanged += (_, _) => eventCount++;

		collection.AddRange([]);

		eventCount.AssertEqual(0);
	}

	[TestMethod]
	public void Remove_ExistingItem_ReturnsTrue()
	{
		var collection = new ObservableCollectionEx<int>();
		collection.AddRange([1, 2, 3]);

		var result = collection.Remove(2);

		result.AssertTrue();
		collection.Count.AssertEqual(2);
		collection.Contains(2).AssertFalse();
	}

	[TestMethod]
	public void Remove_NonExistingItem_ReturnsFalse()
	{
		var collection = new ObservableCollectionEx<int>();
		collection.AddRange([1, 2, 3]);

		var result = collection.Remove(99);

		result.AssertFalse();
		collection.Count.AssertEqual(3);
	}

	[TestMethod]
	public void Remove_RaisesRemovedRange()
	{
		var collection = new ObservableCollectionEx<int>();
		collection.AddRange([1, 2, 3]);
		var removedItems = new List<int>();
		collection.RemovedRange += items => removedItems.AddRange(items);

		collection.Remove(2);

		removedItems.Count.AssertEqual(1);
		removedItems[0].AssertEqual(2);
	}

	[TestMethod]
	public void Clear_RemovesAllItems()
	{
		var collection = new ObservableCollectionEx<int>();
		collection.AddRange([1, 2, 3]);

		collection.Clear();

		collection.Count.AssertEqual(0);
	}

	[TestMethod]
	public void Clear_RaisesRemovedRange()
	{
		var collection = new ObservableCollectionEx<int>();
		collection.AddRange([1, 2, 3]);
		var removedItems = new List<int>();
		collection.RemovedRange += removedItems.AddRange;

		collection.Clear();

		removedItems.Count.AssertEqual(3);
		removedItems.Contains(1).AssertTrue();
		removedItems.Contains(2).AssertTrue();
		removedItems.Contains(3).AssertTrue();
	}

	[TestMethod]
	public void Clear_EmptyCollection_NoEvents()
	{
		var collection = new ObservableCollectionEx<int>();
		var eventCount = 0;
		collection.CollectionChanged += (_, _) => eventCount++;
		collection.RemovedRange += _ => eventCount++;

		collection.Clear();

		eventCount.AssertEqual(0);
	}

	[TestMethod]
	public void Clear_RaisesCollectionReset()
	{
		var collection = new ObservableCollectionEx<int>();
		collection.AddRange([1, 2, 3]);
		var changedArgs = new List<NotifyCollectionChangedEventArgs>();
		collection.CollectionChanged += (_, args) => changedArgs.Add(args);

		collection.Clear();

		changedArgs.Count.AssertEqual(1);
		changedArgs[0].Action.AssertEqual(NotifyCollectionChangedAction.Reset);
	}

	[TestMethod]
	public void RemoveRange_ByItems_RemovesCorrectItems()
	{
		var collection = new ObservableCollectionEx<int>();
		collection.AddRange([1, 2, 3, 4, 5]);

		collection.RemoveRange([2, 4]);

		collection.Count.AssertEqual(3);
		collection[0].AssertEqual(1);
		collection[1].AssertEqual(3);
		collection[2].AssertEqual(5);
	}

	[TestMethod]
	public void RemoveRange_ByItems_PreservesOrder()
	{
		var collection = new ObservableCollectionEx<int>();
		collection.AddRange([.. Enumerable.Range(1, 100)]);

		// Remove every other item (large enough to trigger optimization)
		var toRemove = Enumerable.Range(1, 50).Select(i => i * 2).ToArray();
		collection.RemoveRange(toRemove);

		// Verify remaining items are odd numbers in order
		var expected = Enumerable.Range(1, 50).Select(i => i * 2 - 1).ToArray();
		collection.Count.AssertEqual(50);
		for (var i = 0; i < 50; i++)
			collection[i].AssertEqual(expected[i]);
	}

	[TestMethod]
	public void RemoveRange_ByIndex_RemovesCorrectItems()
	{
		var collection = new ObservableCollectionEx<int>();
		collection.AddRange([1, 2, 3, 4, 5]);

		var removed = collection.RemoveRange(1, 3);

		removed.AssertEqual(3);
		collection.Count.AssertEqual(2);
		collection[0].AssertEqual(1);
		collection[1].AssertEqual(5);
	}

	[TestMethod]
	public void RemoveRange_ByIndex_EmptyRange_ReturnsZero()
	{
		var collection = new ObservableCollectionEx<int>();
		collection.AddRange([1, 2, 3]);

		var removed = collection.RemoveRange(1, 0);

		removed.AssertEqual(0);
		collection.Count.AssertEqual(3);
	}

	[TestMethod]
	public void Insert_InsertsAtCorrectPosition()
	{
		var collection = new ObservableCollectionEx<int>();
		collection.AddRange([1, 3]);

		collection.Insert(1, 2);

		collection.Count.AssertEqual(3);
		collection[0].AssertEqual(1);
		collection[1].AssertEqual(2);
		collection[2].AssertEqual(3);
	}

	[TestMethod]
	public void Insert_RaisesCollectionChanged()
	{
		var collection = new ObservableCollectionEx<int>();
		collection.AddRange([1, 3]);
		var changedArgs = new List<NotifyCollectionChangedEventArgs>();
		collection.CollectionChanged += (_, args) => changedArgs.Add(args);

		collection.Insert(1, 2);

		changedArgs.Count.AssertEqual(1);
		changedArgs[0].Action.AssertEqual(NotifyCollectionChangedAction.Add);
		changedArgs[0].NewStartingIndex.AssertEqual(1);
	}

	[TestMethod]
	public void RemoveAt_RemovesCorrectItem()
	{
		var collection = new ObservableCollectionEx<int>();
		collection.AddRange([1, 2, 3]);

		collection.RemoveAt(1);

		collection.Count.AssertEqual(2);
		collection[0].AssertEqual(1);
		collection[1].AssertEqual(3);
	}

	[TestMethod]
	public void Indexer_Set_RaisesReplaceEvent()
	{
		var collection = new ObservableCollectionEx<int>();
		collection.AddRange([1, 2, 3]);
		var changedArgs = new List<NotifyCollectionChangedEventArgs>();
		collection.CollectionChanged += (_, args) => changedArgs.Add(args);

		collection[1] = 99;

		changedArgs.Count.AssertEqual(1);
		changedArgs[0].Action.AssertEqual(NotifyCollectionChangedAction.Replace);
		changedArgs[0].OldItems[0].AssertEqual(2);
		changedArgs[0].NewItems[0].AssertEqual(99);
	}

	[TestMethod]
	public void IndexOf_ReturnsCorrectIndex()
	{
		var collection = new ObservableCollectionEx<int>();
		collection.AddRange([10, 20, 30]);

		collection.IndexOf(20).AssertEqual(1);
		collection.IndexOf(99).AssertEqual(-1);
	}

	[TestMethod]
	public void Contains_ReturnsCorrectResult()
	{
		var collection = new ObservableCollectionEx<int>();
		collection.AddRange([1, 2, 3]);

		collection.Contains(2).AssertTrue();
		collection.Contains(99).AssertFalse();
	}

	[TestMethod]
	public void CopyTo_CopiesElements()
	{
		var collection = new ObservableCollectionEx<int>();
		collection.AddRange([1, 2, 3]);
		var array = new int[5];

		collection.CopyTo(array, 1);

		array[0].AssertEqual(0);
		array[1].AssertEqual(1);
		array[2].AssertEqual(2);
		array[3].AssertEqual(3);
		array[4].AssertEqual(0);
	}

	[TestMethod]
	public void ICollection_CopyTo_CopiesElements()
	{
		var collection = new ObservableCollectionEx<int>();
		collection.AddRange([1, 2, 3]);
		var array = new int[5];

		((System.Collections.ICollection)collection).CopyTo(array, 1);

		array[0].AssertEqual(0);
		array[1].AssertEqual(1);
		array[2].AssertEqual(2);
		array[3].AssertEqual(3);
		array[4].AssertEqual(0);
	}

	[TestMethod]
	public void ICollection_CopyTo_ObjectArray_CopiesElements()
	{
		// This test verifies the bug fix where CopyTo with object[] array
		// was creating a new array instead of copying to the provided one.
		var collection = new ObservableCollectionEx<int>();
		collection.AddRange([1, 2, 3]);
		var array = new object[5];

		((System.Collections.ICollection)collection).CopyTo(array, 1);

		// Before the fix, this would fail because the original array was not modified
		array[0].AssertNull();
		array[1].AssertEqual(1);
		array[2].AssertEqual(2);
		array[3].AssertEqual(3);
		array[4].AssertNull();
	}

	[TestMethod]
	public void ICollection_IsSynchronized_ReturnsFalse()
	{
		var collection = new ObservableCollectionEx<int>();

		((System.Collections.ICollection)collection).IsSynchronized.AssertFalse();
	}

	[TestMethod]
	public void IsReadOnly_ReturnsFalse()
	{
		var collection = new ObservableCollectionEx<int>();

		collection.IsReadOnly.AssertFalse();
	}

	[TestMethod]
	public void GetEnumerator_EnumeratesAllItems()
	{
		var collection = new ObservableCollectionEx<int>();
		collection.AddRange([1, 2, 3]);
		var enumerated = new List<int>();

		foreach (var item in collection)
			enumerated.Add(item);

		enumerated.Count.AssertEqual(3);
		enumerated[0].AssertEqual(1);
		enumerated[1].AssertEqual(2);
		enumerated[2].AssertEqual(3);
	}

	[TestMethod]
	public void IList_Add_AddsItem()
	{
		var collection = new ObservableCollectionEx<int>();

		var index = ((System.Collections.IList)collection).Add(42);

		index.AssertEqual(0);
		collection.Count.AssertEqual(1);
		collection[0].AssertEqual(42);
	}

	[TestMethod]
	public void IList_Contains_Works()
	{
		var collection = new ObservableCollectionEx<int>
        {
            42
        };

		((System.Collections.IList)collection).Contains(42).AssertTrue();
		((System.Collections.IList)collection).Contains(99).AssertFalse();
	}

	[TestMethod]
	public void IList_IndexOf_Works()
	{
		var collection = new ObservableCollectionEx<int>();
		collection.AddRange([10, 20, 30]);

		((System.Collections.IList)collection).IndexOf(20).AssertEqual(1);
	}

	[TestMethod]
	public void IList_Insert_Works()
	{
		var collection = new ObservableCollectionEx<int>();
		collection.AddRange([1, 3]);

		((System.Collections.IList)collection).Insert(1, 2);

		collection[1].AssertEqual(2);
	}

	[TestMethod]
	public void IList_Remove_Works()
	{
		var collection = new ObservableCollectionEx<int>();
		collection.AddRange([1, 2, 3]);

		((System.Collections.IList)collection).Remove(2);

		collection.Count.AssertEqual(2);
		collection.Contains(2).AssertFalse();
	}

	[TestMethod]
	public void IList_Indexer_Works()
	{
		var collection = new ObservableCollectionEx<int>();
		collection.AddRange([1, 2, 3]);

		var list = (System.Collections.IList)collection;
		list[1].AssertEqual(2);

		list[1] = 99;
		collection[1].AssertEqual(99);
	}

	[TestMethod]
	public void IList_IsFixedSize_ReturnsFalse()
	{
		var collection = new ObservableCollectionEx<int>();

		((System.Collections.IList)collection).IsFixedSize.AssertFalse();
	}

	[TestMethod]
	public void RemoveRange_LargeSet_RaisesRemovedRange()
	{
		var collection = new ObservableCollectionEx<int>();
		collection.AddRange([.. Enumerable.Range(1, 100)]);
		var removedItems = new List<int>();
		collection.RemovedRange += items => removedItems.AddRange(items);

		// Remove enough items to trigger the optimization path
		var toRemove = Enumerable.Range(1, 20).ToArray();
		collection.RemoveRange(toRemove);

		removedItems.Count.AssertEqual(20);
		foreach (var item in toRemove)
			removedItems.Contains(item).AssertTrue();
	}

	[TestMethod]
	public void RemoveRange_EmptyItems_NoEvent()
	{
		var collection = new ObservableCollectionEx<int>();
		collection.AddRange([1, 2, 3]);
		var eventCount = 0;
		collection.CollectionChanged += (_, _) => eventCount++;

		collection.RemoveRange([]);

		eventCount.AssertEqual(0);
	}
}
