namespace Ecng.Tests.Collections;

[TestClass]
public class SynchronizedStackTests
{
	[TestMethod]
	public void BasicOperations()
	{
		var st = new SynchronizedStack<int>();
		st.Push(1);
		st.Push(2);
		st.Peek().AssertEqual(2);
		st.Pop().AssertEqual(2);
		st.Pop().AssertEqual(1);
		st.Count.AssertEqual(0);
	}

	[TestMethod]
	public void EmptyOperations()
	{
		var st = new SynchronizedStack<int>();
		Assert.ThrowsException<InvalidOperationException>(() => st.Pop());
		Assert.ThrowsException<InvalidOperationException>(() => st.Peek());
	}

	[TestMethod]
	public void UnsupportedMethods()
	{
		var st = new SynchronizedStack<int>();
		var list = (IList)st;
		Assert.ThrowsException<NotSupportedException>(() => _ = list[0]);
		Assert.ThrowsException<NotSupportedException>(() => list.Insert(0, 1));
		Assert.ThrowsException<NotSupportedException>(() => list.RemoveAt(0));
	}
}
