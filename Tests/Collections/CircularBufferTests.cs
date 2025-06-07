namespace Ecng.Tests.Collections;

[TestClass]
public class CircularBufferTests
{
	[TestMethod]
	public void AppendOverCapacity()
	{
		var buf = new CircularBuffer<int>(3);
		buf.PushBack(1);
		buf.PushBack(2);
		buf.PushBack(3);
		buf.IsFull.AssertTrue();
		buf.PushBack(4);
		buf.ToArray().SequenceEqual([2, 3, 4]).AssertTrue();
	}

	[TestMethod]
	public void FrontBackPushPop()
	{
		var buf = new CircularBuffer<int>(2);
		buf.PushBack(1);
		buf.PushFront(0);
		buf.Back().AssertEqual(1);
		buf.Front().AssertEqual(0);
		buf.PopBack();
		buf.PopFront();
		buf.IsEmpty.AssertTrue();
	}

	[TestMethod]
	public void SegmentsEnumeration()
	{
		var buf = new CircularBuffer<int>(5);
		buf.PushBack(1);
		buf.PushBack(2);
		buf.PushBack(3);
		buf.PopFront();
		buf.PushBack(4);
		buf.PushBack(5);
		buf.ToArray().SequenceEqual([2, 3, 4, 5]).AssertTrue();
		var total = buf.ToArraySegments().Sum(s => s.Count);
		total.AssertEqual(4);
	}

	[TestMethod]
	public void ClearAndExceptions()
	{
		var buf = new CircularBuffer<int>(3);
		buf.PushBack(1);
		buf.PushBack(2);
		buf.Clear();
		buf.IsEmpty.AssertTrue();
		Assert.ThrowsExactly<InvalidOperationException>(() => buf.PopBack());
	}
}
