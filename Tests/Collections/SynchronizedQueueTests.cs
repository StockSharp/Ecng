namespace Ecng.Tests.Collections;

[TestClass]
public class SynchronizedQueueTests
{
	[TestMethod]
	public void BasicOperations()
	{
		var q = new SynchronizedQueue<int>();
		q.Enqueue(1);
		q.Enqueue(2);
		q.Peek().AssertEqual(1);
		q.Dequeue().AssertEqual(1);
		q.Dequeue().AssertEqual(2);
		q.Count.AssertEqual(0);
	}

	[TestMethod]
	public void EmptyOperations()
	{
		var q = new SynchronizedQueue<int>();
		Assert.ThrowsException<InvalidOperationException>(() => q.Dequeue());
		Assert.ThrowsException<InvalidOperationException>(() => q.Peek());
	}

	[TestMethod]
	public void UnsupportedMethods()
	{
		var q = new SynchronizedQueue<int>();
		var list = (IList)q;
		Assert.ThrowsException<NotSupportedException>(() => _ = list[0]);
		Assert.ThrowsException<NotSupportedException>(() => list.Insert(0, 1));
		Assert.ThrowsException<NotSupportedException>(() => list.RemoveAt(0));
	}
}
