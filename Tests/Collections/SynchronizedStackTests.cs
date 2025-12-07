namespace Ecng.Tests.Collections;

[TestClass]
public class SynchronizedStackTests : BaseTestClass
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
		ThrowsExactly<InvalidOperationException>(() => st.Pop());
		ThrowsExactly<InvalidOperationException>(() => st.Peek());
	}

	[TestMethod]
	public void UnsupportedMethods()
	{
		var st = new SynchronizedStack<int>
		{
			1
		};
		var list = (IList<int>)st;
		ThrowsExactly<NotSupportedException>(() => _ = list[0]);
		ThrowsExactly<NotSupportedException>(() => list.Insert(0, 1));
		ThrowsExactly<NotSupportedException>(() => list.RemoveAt(0));
	}
}