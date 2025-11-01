namespace Ecng.Tests.Logging;

using Ecng.Logging;

[TestClass]
public class LoggingTests
{
	private class GuardedReceiver : BaseLogReceiver
	{
		[ThreadStatic]
		private static int _depth;
		private readonly int _limit;

		public GuardedReceiver(string name, int limit =2000)
		{
			Name = name;
			_limit = limit;
		}

		protected override void RaiseLog(LogMessage message)
		{
			_depth++;
			try
			{
				if (_depth > _limit)
					throw new InvalidOperationException($"Recursion limit reached: {_limit}");

				base.RaiseLog(message);
			}
			finally
			{
				_depth--;
			}
		}
	}

	[TestMethod]
	public void IndirectCycle()
	{
		var a = new LogReceiver("A");
		var b = new LogReceiver("B");
		var c = new LogReceiver("C");

		// Arrange: chain A -> B -> C
		a.Parent = b;
		b.Parent = c;

		// Act: close the cycle C -> A (should be blocked by proper cycle detection)
		// Current implementation allows this and does not throw.
		Assert.ThrowsExactly<ArgumentException>(() => c.Parent = a);

		// Assert: cycle is created (reproduces the bug)
		a.Parent.AssertSame(b);
		b.Parent.AssertSame(c);
		c.Parent.AssertSame(null);
	}

	[TestMethod]
	public void OverflowsRecursion()
	{
		// Use guarded receivers to avoid real StackOverflow and still show unbounded propagation.
		var a = new GuardedReceiver("A");
		var b = new GuardedReceiver("B");
		var c = new GuardedReceiver("C");

		a.Parent = b;
		b.Parent = c;

		// create cycle
		Assert.ThrowsExactly<ArgumentException>(() => c.Parent = a);

		var msg = new LogMessage(a, DateTime.UtcNow, LogLevels.Info, "x");

		((ILogReceiver)a).AddLog(msg);
	}
}
