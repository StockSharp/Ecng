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
		Assert.ThrowsExactly<InvalidOperationException>(() => st.Pop());
		Assert.ThrowsExactly<InvalidOperationException>(() => st.Peek());
	}

	[TestMethod]
	public void UnsupportedMethods()
	{
		var st = new SynchronizedStack<int>();
		var list = (IList<int>)st;
		Assert.ThrowsExactly<NotSupportedException>(() => _ = list[0]);
		Assert.ThrowsExactly<NotSupportedException>(() => list.Insert(0, 1));
		Assert.ThrowsExactly<NotSupportedException>(() => list.RemoveAt(0));
	}
}