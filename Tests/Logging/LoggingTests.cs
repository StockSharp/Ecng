namespace Ecng.Tests.Logging;

using Ecng.Logging;
using Ecng.Serialization;

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

	private sealed class CollectingTraceListener : TraceListener
	{
		public List<string> Entries { get; } = [];

		public override void Write(string message)
		{
		}

		public override void WriteLine(string message)
		{
			Entries.Add(message);
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

		// Act: closing the cycle C -> A must be blocked by cycle detection and throw.
		ThrowsExactly<ArgumentException>(() => c.Parent = a);

		// Assert: the cycle was rejected; the original chain is intact and C has no parent.
		a.Parent.AssertSame(b);
		b.Parent.AssertSame(c);
		c.Parent.AssertSame(null);
	}

	[TestMethod]
	public void OverflowsRecursion()
	{
		// Guarded receivers cap recursion so a regression (cycle accepted) would not crash the runner with a real StackOverflow.
		var a = new GuardedReceiver("A");
		var b = new GuardedReceiver("B");
		var c = new GuardedReceiver("C");

		a.Parent = b;
		b.Parent = c;

		// closing the cycle is rejected, so propagation stays bounded
		ThrowsExactly<ArgumentException>(() => c.Parent = a);

		var msg = new LogMessage(a, DateTime.UtcNow, LogLevels.Info, "x");

		((ILogReceiver)a).AddLog(msg);
	}

	[TestMethod]
	[DataRow(LogLevels.Warning, LogLevels.Debug, false)]
	[DataRow(LogLevels.Warning, LogLevels.Warning, true)]
	[DataRow(LogLevels.Error, LogLevels.Warning, false)]
	[DataRow(LogLevels.Error, LogLevels.Error, true)]
	public void InheritedLogLevel_FiltersMessagesThroughParent(LogLevels parentLevel, LogLevels messageLevel, bool expectedRaised)
	{
		var parent = new LogReceiver("Parent") { LogLevel = parentLevel };
		var child = new LogReceiver("Child") { Parent = parent };
		var raised = false;

		parent.Log += _ => raised = true;

		((ILogReceiver)child).AddLog(new LogMessage(child, DateTime.UtcNow, messageLevel, "message"));

		raised.AssertEqual(expectedRaised);
	}

	[TestMethod]
	public void InheritedLogLevel_UsesAncestorLevel()
	{
		var root = new LogReceiver("Root") { LogLevel = LogLevels.Error };
		var middle = new LogReceiver("Middle") { Parent = root };
		var child = new LogReceiver("Child") { Parent = middle };
		var raised = false;

		root.Log += _ => raised = true;

		((ILogReceiver)child).AddLog(new LogMessage(child, DateTime.UtcNow, LogLevels.Warning, "warning"));

		raised.AssertFalse();
	}

	[TestMethod]
	public async Task ObserveError_DoesNotTreatCanceledTaskAsSuccess()
	{
		var token = new CancellationToken(true);
		var completed = false;
		var observedError = false;

		await Task.FromCanceled(token).ObserveError(_ => observedError = true, _ => completed = true);

		observedError.AssertFalse();
		completed.AssertFalse();
	}

	[TestMethod]
	public async Task ObserveErrorGeneric_DoesNotTreatCanceledTaskAsSuccess()
	{
		var token = new CancellationToken(true);
		var completed = false;
		var observedError = false;

		await Task.FromCanceled<int>(token).ObserveError(_ => observedError = true, _ => completed = true);

		observedError.AssertFalse();
		completed.AssertFalse();
	}

	[TestMethod]
	[DoNotParallelize]
	public void DebugLogListener_FirstNonInfoMessage_DoesNotEmitEmptyTraceEntry()
	{
		var trace = new CollectingTraceListener();
		Trace.Listeners.Add(trace);

		try
		{
			var listener = new DebugLogListener();
			var src = new LogReceiver("S");

			listener.WriteMessages([Msg(src, LogLevels.Error, "boom")]);
		}
		finally
		{
			Trace.Listeners.Remove(trace);
		}

		trace.Entries.Any(e => e.IsEmpty()).AssertFalse();
		trace.Entries.Count.AssertEqual(1);
		trace.Entries[0].AssertContains("boom");
	}

	[TestMethod]
	[DoNotParallelize]
	public void LogManager_LoadWithoutFlushInterval_KeepsDefault()
	{
		using var manager = new LogManager();
		var before = manager.FlushInterval;
		var storage = new SettingsStorage()
			.Set(nameof(LogManager.Listeners), Array.Empty<SettingsStorage>());

		manager.Load(storage);

		manager.FlushInterval.AssertEqual(before);
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

	/// <summary>
	/// Regression test for <see cref="LogManager"/> disposal: ensures the <see cref="UnhandledExceptionSource"/>
	/// the manager creates in its ctor is itself disposed when the manager is disposed, releasing its static
	/// AppDomain/TaskScheduler event subscriptions.
	/// (Was: DisposeManaged only cleared Sources and left the owned source subscribed forever -
	/// Logging\LogManager.cs:306 now calls _unhandledExceptionSource.Dispose().)
	/// </summary>
	[TestMethod]
	[DoNotParallelize]
	public void Manager_Dispose_DisposesOwnedUnhandledExceptionSource()
	{
		// sync mode avoids the flush-timer dispose path entirely
		var manager = new LogManager(false);

		var unhandled = manager.Sources.OfType<UnhandledExceptionSource>().FirstOrDefault();
		unhandled.AssertNotNull("LogManager should own an UnhandledExceptionSource.");
		unhandled.IsDisposed.AssertFalse();

		manager.Dispose();

		unhandled.IsDisposed.AssertTrue("UnhandledExceptionSource owned by the manager must be disposed on manager dispose.");
	}

	/// <summary>
	/// Regression test for the TraceSource/DebugLogListener pipeline: a single seed Trace write is raised on the
	/// source exactly once, and the listener's own Trace output does not feed back into TraceSource (no infinite loop).
	/// (Was: DebugLogListener wrote flushed messages back into Trace, re-feeding TraceSource without any reentrancy
	/// guard - now Logging\DebugLogListener.cs:48 wraps the write in TraceSource.Suppress and Logging\TraceSource.cs:16
	/// short-circuits re-raises while suppressed.)
	/// </summary>
	[TestMethod]
	[DoNotParallelize]
	public void TraceSource_WithTraceWritingListener_DoesNotFeedbackLoop()
	{
		using var traceSource = new TraceSource();
		var debug = new DebugLogListener();

		var raised = 0;
		const int guard = 50;

		void Handler(LogMessage message)
		{
			raised++;

			// Bounded recursion guard so a regression (feedback loop) would not crash the runner with a StackOverflow.
			if (raised >= guard)
				return;

			debug.WriteMessages([message]);
		}

		traceSource.Log += Handler;

		try
		{
			// Seed a single Trace event; correct behavior raises exactly once on the source.
			Trace.TraceInformation("seed");
		}
		finally
		{
			traceSource.Log -= Handler;
		}

		raised.AssertEqual(1, "A single Trace write must not re-feed into TraceSource (no infinite loop).");
	}
}
