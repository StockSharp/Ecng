namespace Ecng.Tests.Logging;

using Ecng.Logging;

[TestClass]
public class LoggingTests : BaseTestClass
{
	/// <summary>
	/// Mock listener that tracks all method calls for testing.
	/// </summary>
	private class MockLogListener : LogListener
	{
		public List<LogMessage> SyncMessages { get; } = [];
		public List<LogMessage> AsyncMessages { get; } = [];
		public int OnWriteMessageCalls { get; private set; }
		public int OnWriteMessagesCalls { get; private set; }
		public int OnWriteMessagesAsyncCalls { get; private set; }
		public bool OverrideAsync { get; set; }

		protected override void OnWriteMessage(LogMessage message)
		{
			OnWriteMessageCalls++;
			SyncMessages.Add(message);
		}

		protected override void OnWriteMessages(IEnumerable<LogMessage> messages)
		{
			OnWriteMessagesCalls++;
			base.OnWriteMessages(messages); // calls OnWriteMessage for each
		}

		protected override ValueTask OnWriteMessagesAsync(IEnumerable<LogMessage> messages, CancellationToken cancellationToken = default)
		{
			OnWriteMessagesAsyncCalls++;

			if (OverrideAsync)
			{
				AsyncMessages.AddRange(messages);
				return default;
			}

			return base.OnWriteMessagesAsync(messages, cancellationToken); // fallback to sync
		}
	}

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
		ThrowsExactly<ArgumentException>(() => c.Parent = a);

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
		ThrowsExactly<ArgumentException>(() => c.Parent = a);

		var msg = new LogMessage(a, DateTime.UtcNow, LogLevels.Info, "x");

		((ILogReceiver)a).AddLog(msg);
	}

	private static LogMessage Msg(ILogSource src, LogLevels level, string text)
		=> new(src, DateTime.UtcNow, level, text);

	[TestMethod]
	public void Filter_EmptyCollection()
	{
		var src = new LogReceiver("S");
		IEnumerable<LogMessage> messages =
		[
			Msg(src, LogLevels.Info, "i"),
			Msg(src, LogLevels.Warning, "w"),
			Msg(src, LogLevels.Error, "e"),
		];

		var filters = new List<Func<LogMessage, bool>>();
		var result = messages.Filter(filters);

		// Should return the original enumerable instance when filters are empty
		result.AssertSame(messages);
		messages.Count().AssertEqual(result.Count());
	}

	[TestMethod]
	public void Filter_OnlyWarning()
	{
		var src = new LogReceiver("S");
		var messages = new[]
		{
			Msg(src, LogLevels.Info, "i1"),
			Msg(src, LogLevels.Warning, "w1"),
			Msg(src, LogLevels.Error, "e1"),
			Msg(src, LogLevels.Warning, "w2"),
		};

		var filters = new List<Func<LogMessage, bool>> { LoggingHelper.OnlyWarning };
		var result = messages.Filter(filters).ToArray();

		result.Length.AssertEqual(2);
		result.All(m => m.Level == LogLevels.Warning).AssertTrue();
	}

	[TestMethod]
	public void Filter_WarningOrError()
	{
		var src = new LogReceiver("S");
		var messages = new[]
		{
			Msg(src, LogLevels.Info, "i"),
			Msg(src, LogLevels.Warning, "w"),
			Msg(src, LogLevels.Error, "e"),
			Msg(src, LogLevels.Debug, "d"),
		};

		var filters = new List<Func<LogMessage, bool>> { LoggingHelper.OnlyWarning, LoggingHelper.OnlyError };
		var resultLevels = messages.Filter(filters).Select(m => m.Level).OrderBy(l => l).ToArray();
		var expectedLevels = new[] { LogLevels.Error, LogLevels.Warning }.OrderBy(l => l).ToArray();

		resultLevels.AssertEqual(expectedLevels);
	}

	[TestMethod]
	public void Filter_CustomPredicate()
	{
		var a = new LogReceiver("A");
		var b = new LogReceiver("B");
		var messages = new[]
		{
			Msg(a, LogLevels.Info, "a1"),
			Msg(b, LogLevels.Info, "b1"),
			Msg(a, LogLevels.Error, "a2"),
		};

		var filters = new List<Func<LogMessage, bool>>
		{
			m => m.Source.Name == "A"
		};

		var result = messages.Filter(filters).ToArray();

		result.Length.AssertEqual(2);
		result.All(m => m.Source.Name == "A").AssertTrue();
	}

	[TestMethod]
	public void Listener_SyncPath_CallsOnWriteMessage()
	{
		var listener = new MockLogListener();
		var src = new LogReceiver("S");
		var messages = new[] { Msg(src, LogLevels.Info, "m1"), Msg(src, LogLevels.Warning, "m2") };

		listener.WriteMessages(messages);

		listener.OnWriteMessagesCalls.AssertEqual(1);
		listener.OnWriteMessageCalls.AssertEqual(2);
		listener.SyncMessages.Count.AssertEqual(2);
	}

	[TestMethod]
	public async Task Listener_AsyncPath_FallbackToSync()
	{
		var listener = new MockLogListener { OverrideAsync = false };
		var src = new LogReceiver("S");
		var messages = new[] { Msg(src, LogLevels.Info, "m1"), Msg(src, LogLevels.Error, "m2") };

		await listener.WriteMessagesAsync(messages, CancellationToken);

		listener.OnWriteMessagesAsyncCalls.AssertEqual(1);
		listener.OnWriteMessagesCalls.AssertEqual(1); // fallback called sync
		listener.OnWriteMessageCalls.AssertEqual(2);
		listener.SyncMessages.Count.AssertEqual(2);
		listener.AsyncMessages.Count.AssertEqual(0);
	}

	[TestMethod]
	public async Task Listener_AsyncPath_OverrideAsync()
	{
		var listener = new MockLogListener { OverrideAsync = true };
		var src = new LogReceiver("S");
		var messages = new[] { Msg(src, LogLevels.Info, "m1") };

		await listener.WriteMessagesAsync(messages, CancellationToken);

		listener.OnWriteMessagesAsyncCalls.AssertEqual(1);
		listener.OnWriteMessagesCalls.AssertEqual(0); // no fallback
		listener.OnWriteMessageCalls.AssertEqual(0);
		listener.SyncMessages.Count.AssertEqual(0);
		listener.AsyncMessages.Count.AssertEqual(1);
	}
}
