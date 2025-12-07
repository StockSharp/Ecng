namespace Ecng.Tests.Collections;

[TestClass]
public class SynchronizedQueueTests : BaseTestClass
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
		ThrowsExactly<InvalidOperationException>(() => q.Dequeue());
		ThrowsExactly<InvalidOperationException>(() => q.Peek());
	}

	[TestMethod]
	public void UnsupportedMethods()
	{
		var q = new SynchronizedQueue<int>();
		q.Enqueue(1);
		var list = (IList<int>)q;
		ThrowsExactly<NotSupportedException>(() => _ = list[0]);
		ThrowsExactly<NotSupportedException>(() => list.Insert(0, 1));
		ThrowsExactly<NotSupportedException>(() => list.RemoveAt(0));
	}
}
